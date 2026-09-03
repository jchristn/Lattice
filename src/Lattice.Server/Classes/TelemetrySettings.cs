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
        /// Master switch for telemetry export. Default true. When false, no telemetry host is started
        /// and instrumentation stays an inert no-op (near-zero overhead).
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// The logical service name stamped as the <c>service.name</c> resource attribute. Default
        /// <c>lattice-server</c>.
        /// </summary>
        public string ServiceName { get; set; } = "lattice-server";

        /// <summary>
        /// The service instance identifier stamped as <c>service.instance.id</c>. Null generates a
        /// stable GUID for the process lifetime.
        /// </summary>
        public string ServiceInstanceId { get; set; } = null;

        /// <summary>
        /// OTLP push exporter settings.
        /// </summary>
        public OtlpSettings Otlp { get; set; } = new OtlpSettings();

        /// <summary>
        /// Metrics pillar settings.
        /// </summary>
        public MetricsExportSettings Metrics { get; set; } = new MetricsExportSettings();

        /// <summary>
        /// Traces pillar settings.
        /// </summary>
        public TracesExportSettings Traces { get; set; } = new TracesExportSettings();

        /// <summary>
        /// Logs pillar settings.
        /// </summary>
        public LogsExportSettings Logs { get; set; } = new LogsExportSettings();

        /// <summary>
        /// In-process Prometheus scrape endpoint settings.
        /// </summary>
        public PrometheusScrapeExportSettings Prometheus { get; set; } = new PrometheusScrapeExportSettings();

        /// <summary>
        /// Direct Loki log export settings.
        /// </summary>
        public LokiExportSettings Loki { get; set; } = new LokiExportSettings();

        /// <summary>
        /// OTLP push exporter settings.
        /// </summary>
        public class OtlpSettings
        {
            /// <summary>Whether the OTLP push exporter is enabled. Default true.</summary>
            public bool Enable { get; set; } = true;

            /// <summary>Collector endpoint. Default <c>http://localhost:4317</c> (gRPC).</summary>
            public string Endpoint { get; set; } = "http://localhost:4317";

            /// <summary>Wire protocol: <c>Grpc</c> or <c>HttpProtobuf</c>. Default <c>Grpc</c>.</summary>
            public string Protocol { get; set; } = "Grpc";

            /// <summary>Per-export timeout in milliseconds. Default 10000.</summary>
            public int TimeoutMs { get; set; } = 10000;
        }

        /// <summary>
        /// Metrics pillar settings.
        /// </summary>
        public class MetricsExportSettings
        {
            /// <summary>Whether the metrics pillar is enabled. Default true.</summary>
            public bool Enable { get; set; } = true;

            /// <summary>Metric export interval in milliseconds for OTLP. Default 15000.</summary>
            public int ExportIntervalMs { get; set; } = 15000;

            /// <summary>Include .NET runtime instrumentation (GC, heap, threads, JIT). Default true.</summary>
            public bool IncludeRuntime { get; set; } = true;

            /// <summary>Include baseline process metrics (working set, uptime, threads). Default true.</summary>
            public bool IncludeProcess { get; set; } = true;
        }

        /// <summary>
        /// Traces pillar settings.
        /// </summary>
        public class TracesExportSettings
        {
            /// <summary>Whether the traces pillar is enabled. Default true.</summary>
            public bool Enable { get; set; } = true;

            /// <summary>Head-based sampling ratio, 0.0 to 1.0. Default 1.0.</summary>
            public double SamplingRatio { get; set; } = 1.0;
        }

        /// <summary>
        /// Logs pillar settings.
        /// </summary>
        public class LogsExportSettings
        {
            /// <summary>Whether the logs pillar is enabled. Default true.</summary>
            public bool Enable { get; set; } = true;

            /// <summary>Minimum severity 0..7 (0 Trace .. 7 None). Default 2 (Information and above).</summary>
            public int MinimumSeverity { get; set; } = 2;
        }

        /// <summary>
        /// In-process Prometheus scrape endpoint settings.
        /// </summary>
        public class PrometheusScrapeExportSettings
        {
            /// <summary>Whether the in-process scrape endpoint is enabled. Default true.</summary>
            public bool Enable { get; set; } = true;

            /// <summary>Hostname to bind. Default <c>localhost</c>.</summary>
            public string Hostname { get; set; } = "localhost";

            /// <summary>TCP port to bind. Default 9464.</summary>
            public int Port { get; set; } = 9464;

            /// <summary>Scrape path. Default <c>/metrics</c>.</summary>
            public string Path { get; set; } = "/metrics";
        }

        /// <summary>
        /// Direct Loki log export settings (OTLP-HTTP to Loki). When disabled logs still reach Loki via
        /// the OTLP collector if the logs pillar and OTLP export are enabled.
        /// </summary>
        public class LokiExportSettings
        {
            /// <summary>Whether direct Loki export is enabled. Default false.</summary>
            public bool Enable { get; set; } = false;

            /// <summary>Loki OTLP base endpoint. Default <c>http://localhost:3100/otlp</c>.</summary>
            public string Endpoint { get; set; } = "http://localhost:3100/otlp";

            /// <summary>Loki tenant id sent as <c>X-Scope-OrgID</c>, or null for single-tenant.</summary>
            public string TenantId { get; set; } = null;
        }
    }
}
