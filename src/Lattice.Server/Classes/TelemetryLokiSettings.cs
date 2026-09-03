namespace Lattice.Server.Classes
{
    /// <summary>
    /// Direct Loki log export settings (OTLP-HTTP to Loki). When disabled, logs still reach Loki via
    /// the OTLP collector if the logs pillar and OTLP export are enabled.
    /// </summary>
    public class TelemetryLokiSettings
    {
        /// <summary>
        /// Whether direct Loki export is enabled. Default is false.
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// Loki OTLP base endpoint. Default is <c>http://localhost:3100/otlp</c>.
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:3100/otlp";

        /// <summary>
        /// Loki tenant id sent as the <c>X-Scope-OrgID</c> header, or null for single-tenant Loki.
        /// </summary>
        public string TenantId { get; set; } = null;
    }
}
