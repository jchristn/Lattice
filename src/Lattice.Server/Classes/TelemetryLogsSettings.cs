namespace Lattice.Server.Classes
{
    /// <summary>
    /// Logs pillar settings for telemetry export.
    /// </summary>
    public class TelemetryLogsSettings
    {
        /// <summary>
        /// Whether the logs pillar is enabled. Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Minimum severity to export, 0..7 (0 = Trace, 2 = Information, 7 = None). Default is 2
        /// (Information and above). Clamped when applied to the telemetry host.
        /// </summary>
        public int MinimumSeverity { get; set; } = 2;
    }
}
