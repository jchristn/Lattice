namespace Lattice.Core.Models
{
    /// <summary>
    /// Progress information for an index rebuild operation.
    /// </summary>
    public class IndexRebuildProgress
    {
        /// <summary>
        /// Total number of documents to process.
        /// </summary>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Number of documents processed so far.
        /// </summary>
        public int ProcessedDocuments { get; set; }

        /// <summary>
        /// Current phase of the rebuild operation.
        /// </summary>
        public string CurrentPhase { get; set; }

        /// <summary>
        /// Percentage of completion (0-100).
        /// </summary>
        public double PercentComplete => TotalDocuments > 0
            ? (double)ProcessedDocuments / TotalDocuments * 100
            : 0;
    }
}
