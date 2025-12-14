namespace Lattice.Server
{
    using System;
    using System.Threading;
    using SyslogLogging;
    using Lattice.Core;
    using Lattice.Server.API.REST;
    using Lattice.Server.Classes;

    /// <summary>
    /// Main program entry point.
    /// </summary>
    public class Program
    {
        private static Settings _Settings = null!;
        private static LoggingModule _Logging = null!;
        private static LatticeClient _Client = null!;
        private static RestServiceHandler _Rest = null!;
        private static readonly string _Header = "[Lattice.Server] ";
        private static readonly ManualResetEvent _ExitEvent = new ManualResetEvent(false);

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Lattice Server starting...");

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

            // Initialize Lattice client
            _Client = new LatticeClient(_Settings.Lattice);
            _Logging.Info(_Header + "Lattice client initialized");

            // Initialize REST service
            _Rest = new RestServiceHandler(_Settings, _Client, _Logging);
            _Rest.Start();

            // Handle shutdown signals
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _Logging.Info(_Header + "shutdown requested");
                _ExitEvent.Set();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                _Logging.Info(_Header + "process exit");
                _ExitEvent.Set();
            };

            Console.WriteLine(_Header + "server running, press CTRL+C to exit");

            // Wait for exit signal
            _ExitEvent.WaitOne();

            // Cleanup
            _Rest.Stop();
            _Logging.Info(_Header + "server stopped");
        }
    }
}
