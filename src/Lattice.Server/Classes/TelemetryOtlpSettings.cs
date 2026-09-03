namespace Lattice.Server.Classes
{
    /// <summary>
    /// OTLP push exporter settings for telemetry export to an OpenTelemetry Collector.
    /// </summary>
    public class TelemetryOtlpSettings
    {
        /// <summary>
        /// Whether the OTLP push exporter is enabled. Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Collector endpoint. Default is <c>http://localhost:4317</c> (gRPC). Use
        /// <c>http://localhost:4318</c> with the HttpProtobuf protocol.
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:4317";

        /// <summary>
        /// OTLP wire protocol: <c>Grpc</c> or <c>HttpProtobuf</c>. Default is <c>Grpc</c>.
        /// </summary>
        public string Protocol { get; set; } = "Grpc";

        /// <summary>
        /// Per-export timeout in milliseconds. Default is 10000. Minimum 1000, maximum 120000
        /// (clamped when applied to the telemetry host).
        /// </summary>
        public int TimeoutMs { get; set; } = 10000;
    }
}
