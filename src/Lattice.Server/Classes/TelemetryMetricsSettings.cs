namespace Lattice.Server.Classes
{
    /// <summary>
    /// Metrics pillar settings for telemetry export.
    /// </summary>
    public class TelemetryMetricsSettings
    {
        /// <summary>
        /// Whether the metrics pillar is enabled. Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Metric export interval in milliseconds for OTLP. Default is 15000. Minimum 1000, maximum
        /// 300000 (clamped when applied to the telemetry host).
        /// </summary>
        public int ExportIntervalMs { get; set; } = 15000;

        /// <summary>
        /// Whether to include .NET runtime instrumentation (GC, heap, threads, JIT). Default is true.
        /// </summary>
        public bool IncludeRuntime { get; set; } = true;

        /// <summary>
        /// Whether to include baseline process metrics (working set, uptime, threads). Default is true.
        /// </summary>
        public bool IncludeProcess { get; set; } = true;
    }
}
