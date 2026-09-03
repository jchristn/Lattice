namespace Lattice.Server
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Lattice.Core.Models;
    using Lattice.Core.Telemetry;
    using Lattice.Server.API.REST;
    using Lattice.Server.Classes;
    using Lattice.Server.Telemetry;
    using Radiant;
    using SyslogLogging;

    /// <summary>
    /// Main program entry point.
    /// </summary>
    public class LatticeServer
    {
        private static Settings _Settings = null!;
        private static LoggingModule _Logging = null!;
        private static LatticeClient _Client = null!;
        private static RestServiceHandler _Rest = null!;
        private static RadiantHost _Telemetry = null;
        private static readonly string _Header = "[LatticeServer] ";
        private static readonly TaskCompletionSource<bool> _ExitTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A task that completes when the server shuts down.</returns>
        public static async Task Main(string[] args)
        {
            Welcome();

            // Load settings
            string settingsFile = "lattice.json";
            if (args.Length > 0)
            {
                settingsFile = args[0];
            }

            _Settings = Settings.FromFile(settingsFile);
            Console.WriteLine(_Header + "settings loaded from " + settingsFile);

            // Initialize logging
            _Logging = new LoggingModule();
            _Logging.Settings.EnableConsole = _Settings.Logging.ConsoleLogging;
            _Logging.Settings.MinimumSeverity = _Settings.Logging.MinimumSeverity;

            if (!String.IsNullOrEmpty(_Settings.Logging.LogFilename))
            {
                _Logging.Settings.FileLogging = FileLoggingMode.SingleLogFile;
                _Logging.Settings.LogFilename = _Settings.Logging.LogFilename;
            }

            _Logging.Info(_Header + "logging initialized");

            // Initialize telemetry (metrics, traces, logs). Instrumentation rides the .NET BCL and is
            // always present; this starts a host that collects and exports it when enabled.
            LatticeTelemetry.DatabaseSystem = ResolveDatabaseSystem(_Settings);
            try
            {
                _Telemetry = TelemetryBootstrap.Start(_Settings.Telemetry, message => _Logging?.Debug("[Radiant] " + message));
                if (_Telemetry != null && _Telemetry.IsEnabled)
                {
                    _Logging.Info(_Header + "telemetry initialized (service '" + _Settings.Telemetry.ServiceName
                        + "', instance " + _Telemetry.ServiceInstanceId + ")");
                    if (_Settings.Telemetry.Otlp.Enable)
                        _Logging.Info(_Header + "telemetry OTLP export to " + _Settings.Telemetry.Otlp.Endpoint);
                    if (_Settings.Telemetry.Prometheus.Enable)
                        _Logging.Info(_Header + "telemetry Prometheus scrape at http://" + _Settings.Telemetry.Prometheus.Hostname
                            + ":" + _Settings.Telemetry.Prometheus.Port + _Settings.Telemetry.Prometheus.Path);
                }
                else
                {
                    _Logging.Info(_Header + "telemetry disabled");
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "telemetry failed to start: " + e.Message + " (continuing without telemetry export)");
                _Telemetry = null;
            }

            // Initialize Lattice client
            _Client = new LatticeClient(_Settings.Lattice);
            _Logging.Info(_Header + "Lattice client initialized");

            // Ensure a default collection exists on first run
            await EnsureDefaultCollectionAsync().ConfigureAwait(false);

            // Initialize REST service
            _Rest = new RestServiceHandler(_Settings, _Client, _Logging);
            _Rest.Start();

            // Handle shutdown signals
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _Logging.Info(_Header + "shutdown requested");
                _ExitTcs.TrySetResult(true);
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                _Logging.Info(_Header + "process exit");
                _ExitTcs.TrySetResult(true);
            };

            Console.WriteLine(_Header + "server running, press CTRL+C to exit");

            // Wait for exit signal
            await _ExitTcs.Task.ConfigureAwait(false);

            // Cleanup
            await _Rest.StopAsync().ConfigureAwait(false);

            if (_Telemetry != null)
            {
                try
                {
                    _Telemetry.ForceFlush(5000);
                    await _Telemetry.DisposeAsync().ConfigureAwait(false);
                    _Logging.Info(_Header + "telemetry stopped");
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "telemetry shutdown error: " + e.Message);
                }
            }

            _Logging.Info(_Header + "server stopped");
        }

        private static async Task EnsureDefaultCollectionAsync(CancellationToken token = default)
        {
            try
            {
                List<Collection> existing = await _Client.Collection.ReadAll(token).ConfigureAwait(false);

                if (existing == null || existing.Count == 0)
                {
                    await _Client.Collection.Create("default", "Default collection created on first run", token: token)
                        .ConfigureAwait(false);
                    _Logging.Info(_Header + "created default collection");
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to ensure default collection: " + e.Message);
            }
        }

        private static string ResolveDatabaseSystem(Settings settings)
        {
            try
            {
                switch (settings.Lattice.Database.Type)
                {
                    case DatabaseTypeEnum.Sqlite: return "sqlite";
                    case DatabaseTypeEnum.Mysql: return "mysql";
                    case DatabaseTypeEnum.Postgres: return "postgresql";
                    case DatabaseTypeEnum.SqlServer: return "sqlserver";
                    default: return "unknown";
                }
            }
            catch
            {
                return "unknown";
            }
        }

        private static void Welcome()
        {
            Console.WriteLine(
                Constants.Logo +
                Environment.NewLine +
                Constants.Copyright +
                Environment.NewLine);
        }
    }
}
