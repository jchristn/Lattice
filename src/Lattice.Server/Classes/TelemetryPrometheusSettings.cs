namespace Lattice.Server.Classes
{
    /// <summary>
    /// In-process Prometheus scrape endpoint settings for telemetry.
    /// </summary>
    public class TelemetryPrometheusSettings
    {
        /// <summary>
        /// Whether the in-process scrape endpoint is enabled. Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Hostname to bind. Default is <c>localhost</c>. Use <c>+</c> or <c>*</c> to bind all
        /// interfaces.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// TCP port to bind. Default is 9464. Minimum 1, maximum 65535 (clamped when applied to the
        /// telemetry host).
        /// </summary>
        public int Port { get; set; } = 9464;

        /// <summary>
        /// Scrape path. Default is <c>/metrics</c>.
        /// </summary>
        public string Path { get; set; } = "/metrics";
    }
}
