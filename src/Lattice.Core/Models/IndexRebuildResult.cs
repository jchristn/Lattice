namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result of an index rebuild operation.
    /// </summary>
    public class IndexRebuildResult
    {
        /// <summary>
        /// Collection ID that was rebuilt.
        /// </summary>
        public string CollectionId { get; set; }

        /// <summary>
        /// Number of documents processed.
        /// </summary>
        public int DocumentsProcessed { get; set; }

        /// <summary>
        /// Number of new index tables created.
        /// </summary>
        public int IndexesCreated { get; set; }

        /// <summary>
        /// Number of index tables dropped (when dropUnusedIndexes is true).
        /// </summary>
        public int IndexesDropped { get; set; }

        /// <summary>
        /// Number of index values inserted.
        /// </summary>
        public int ValuesInserted { get; set; }

        /// <summary>
        /// Duration of the rebuild operation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Duration in milliseconds.
        /// </summary>
        public long DurationMs => (long)Duration.TotalMilliseconds;

        /// <summary>
        /// List of errors encountered during rebuild.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Whether the rebuild completed successfully (no errors).
        /// </summary>
        public bool Success => Errors.Count == 0;
    }
}
