namespace Lattice.Server.Classes
{
    /// <summary>
    /// Traces pillar settings for telemetry export.
    /// </summary>
    public class TelemetryTracesSettings
    {
        /// <summary>
        /// Whether the traces pillar is enabled. Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Head-based sampling ratio, 0.0 to 1.0. Default is 1.0 (sample everything). A value of 0.1
        /// samples roughly 10% of root traces (clamped when applied to the telemetry host).
        /// </summary>
        public double SamplingRatio { get; set; } = 1.0;
    }
}
