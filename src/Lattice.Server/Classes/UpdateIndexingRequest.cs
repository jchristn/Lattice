namespace Lattice.Server.Classes
{
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// Request model for updating collection indexing configuration.
    /// </summary>
    public class UpdateIndexingRequest
    {
        /// <summary>
        /// New indexing mode.
        /// </summary>
        public IndexingMode IndexingMode { get; set; }

        /// <summary>
        /// Fields to index (when mode is Selective).
        /// </summary>
        public List<string>? IndexedFields { get; set; }

        /// <summary>
        /// Whether to automatically rebuild indexes after updating.
        /// </summary>
        public bool RebuildIndexes { get; set; } = false;
    }
}
