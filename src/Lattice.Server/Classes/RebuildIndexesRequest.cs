namespace Lattice.Server.Classes
{
    /// <summary>
    /// Request model for rebuilding collection indexes.
    /// </summary>
    public class RebuildIndexesRequest
    {
        /// <summary>
        /// Whether to drop values from index tables not in the indexed fields list.
        /// Only applies when IndexingMode is Selective.
        /// </summary>
        public bool DropUnusedIndexes { get; set; } = true;
    }
}
