namespace Lattice.Server.Classes
{
    /// <summary>
    /// Telemetry (metrics, traces, logs) settings. These map onto a Radiant host at startup, which
    /// owns the OpenTelemetry pipeline and exports over OTLP (to an OpenTelemetry Collector) and,
    /// optionally, an in-process Prometheus scrape endpoint and direct Loki log export.
    /// <para>
    /// Instrumentation itself rides the .NET base class library and is always present; these settings
    /// only control whether and where a host collects and exports it.
    /// </para>
    /// </summary>
    public class TelemetrySettings
    {
        /// <summary>
        /// Master switch for telemetry export. Default is true. When false, no telemetry host is
        /// started and instrumentation stays an inert no-op (near-zero overhead).
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// The logical service name stamped as the <c>service.name</c> resource attribute. Default is
        /// <c>lattice-server</c>.
        /// </summary>
        public string ServiceName { get; set; } = "lattice-server";

        /// <summary>
        /// The service instance identifier stamped as <c>service.instance.id</c>. Null generates a
        /// stable GUID for the process lifetime.
        /// </summary>
        public string ServiceInstanceId { get; set; } = null;

        /// <summary>
        /// OTLP push exporter settings. Never null.
        /// </summary>
        public TelemetryOtlpSettings Otlp
        {
            get => _Otlp;
            set => _Otlp = value ?? new TelemetryOtlpSettings();
        }

        /// <summary>
        /// Metrics pillar settings. Never null.
        /// </summary>
        public TelemetryMetricsSettings Metrics
        {
            get => _Metrics;
            set => _Metrics = value ?? new TelemetryMetricsSettings();
        }

        /// <summary>
        /// Traces pillar settings. Never null.
        /// </summary>
        public TelemetryTracesSettings Traces
        {
            get => _Traces;
            set => _Traces = value ?? new TelemetryTracesSettings();
        }

        /// <summary>
        /// Logs pillar settings. Never null.
        /// </summary>
        public TelemetryLogsSettings Logs
        {
            get => _Logs;
            set => _Logs = value ?? new TelemetryLogsSettings();
        }

        /// <summary>
        /// In-process Prometheus scrape endpoint settings. Never null.
        /// </summary>
        public TelemetryPrometheusSettings Prometheus
        {
            get => _Prometheus;
            set => _Prometheus = value ?? new TelemetryPrometheusSettings();
        }

        /// <summary>
        /// Direct Loki log export settings. Never null.
        /// </summary>
        public TelemetryLokiSettings Loki
        {
            get => _Loki;
            set => _Loki = value ?? new TelemetryLokiSettings();
        }

        private TelemetryOtlpSettings _Otlp = new TelemetryOtlpSettings();
        private TelemetryMetricsSettings _Metrics = new TelemetryMetricsSettings();
        private TelemetryTracesSettings _Traces = new TelemetryTracesSettings();
        private TelemetryLogsSettings _Logs = new TelemetryLogsSettings();
        private TelemetryPrometheusSettings _Prometheus = new TelemetryPrometheusSettings();
        private TelemetryLokiSettings _Loki = new TelemetryLokiSettings();
    }
}
