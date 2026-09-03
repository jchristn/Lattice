namespace Lattice.Server.Telemetry
{
    using System;
    using Lattice.Server.Classes;
    using Radiant;

    /// <summary>
    /// Builds and starts a Radiant telemetry host from <see cref="TelemetrySettings"/>. This is the one
    /// place in the server that depends on Radiant: it describes the OpenTelemetry pipeline, subscribes
    /// to the meter and activity source names the instrumentation emits into (<c>Lattice.Server</c> and
    /// <c>Lattice.Core</c>), and returns a started host that owns the providers and exporters for the
    /// process lifetime. Emitting telemetry never requires this — it rides the .NET base class library.
    /// </summary>
    public static class TelemetryBootstrap
    {
        /// <summary>
        /// Start a telemetry host for the given settings, or return null when telemetry is disabled.
        /// </summary>
        /// <param name="settings">Telemetry settings.</param>
        /// <param name="diagnostic">Optional callback for internal Radiant diagnostics.</param>
        /// <returns>A started <see cref="RadiantHost"/>, or null when disabled.</returns>
        public static RadiantHost Start(TelemetrySettings settings, Action<string> diagnostic = null)
        {
            if (settings == null || !settings.Enable) return null;

            RadiantSettings radiant = new RadiantSettings(
                string.IsNullOrWhiteSpace(settings.ServiceName) ? "lattice-server" : settings.ServiceName);

            radiant.ServiceInstanceId = settings.ServiceInstanceId;
            radiant.DiagnosticCallback = diagnostic;

            // Subscribe to the names the server and core engine emit into.
            radiant.Sources.AddMeter(ServerTelemetry.Name);
            radiant.Sources.AddActivitySource(ServerTelemetry.Name);
            radiant.Sources.AddMeter(Lattice.Core.Telemetry.LatticeTelemetry.Name);
            radiant.Sources.AddActivitySource(Lattice.Core.Telemetry.LatticeTelemetry.Name);

            // OTLP push exporter.
            radiant.Otlp.Enable = settings.Otlp.Enable;
            if (!string.IsNullOrWhiteSpace(settings.Otlp.Endpoint)) radiant.Otlp.Endpoint = settings.Otlp.Endpoint;
            radiant.Otlp.Protocol = ParseProtocol(settings.Otlp.Protocol);
            radiant.Otlp.TimeoutMs = settings.Otlp.TimeoutMs;

            // Metrics pillar.
            radiant.Metrics.Enable = settings.Metrics.Enable;
            radiant.Metrics.ExportIntervalMs = settings.Metrics.ExportIntervalMs;
            radiant.Metrics.IncludeRuntime = settings.Metrics.IncludeRuntime;
            radiant.Metrics.IncludeProcess = settings.Metrics.IncludeProcess;

            // Traces pillar.
            radiant.Traces.Enable = settings.Traces.Enable;
            radiant.Traces.SamplingRatio = settings.Traces.SamplingRatio;

            // Logs pillar.
            radiant.Logs.Enable = settings.Logs.Enable;
            radiant.Logs.MinimumSeverity = settings.Logs.MinimumSeverity;

            // In-process Prometheus scrape endpoint.
            radiant.Prometheus.Enable = settings.Prometheus.Enable;
            if (!string.IsNullOrWhiteSpace(settings.Prometheus.Hostname)) radiant.Prometheus.Hostname = settings.Prometheus.Hostname;
            radiant.Prometheus.Port = settings.Prometheus.Port;
            if (!string.IsNullOrWhiteSpace(settings.Prometheus.Path)) radiant.Prometheus.Path = settings.Prometheus.Path;

            // Direct Loki export.
            radiant.Loki.Enable = settings.Loki.Enable;
            if (!string.IsNullOrWhiteSpace(settings.Loki.Endpoint)) radiant.Loki.Endpoint = settings.Loki.Endpoint;
            radiant.Loki.TenantId = settings.Loki.TenantId;

            return RadiantHost.Start(radiant);
        }

        private static OtlpProtocolEnum ParseProtocol(string protocol)
        {
            if (string.IsNullOrWhiteSpace(protocol)) return OtlpProtocolEnum.Grpc;
            switch (protocol.Trim().ToLowerInvariant())
            {
                case "httpprotobuf":
                case "http":
                case "http/protobuf":
                    return OtlpProtocolEnum.HttpProtobuf;
                default:
                    return OtlpProtocolEnum.Grpc;
            }
        }
    }
}
