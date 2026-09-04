namespace Lattice.LoadGenerator
{
    /// <summary>
    /// Preset information density. Selects sensible default counts for every category; individual
    /// counts can still be overridden with their explicit arguments.
    /// </summary>
    public enum DensityLevel
    {
        /// <summary>A light dataset — quick to seed, enough to show structure.</summary>
        Low,

        /// <summary>A balanced dataset that looks like a modestly active system (default).</summary>
        Medium,

        /// <summary>A dense dataset that looks like a busy production system.</summary>
        High
    }
}
