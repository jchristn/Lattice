namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents a document entry for batch ingestion.
    /// </summary>
    public class BatchIngestDocument
    {
        /// <summary>
        /// The document content (will be serialized to JSON).
        /// </summary>
        public object Content { get; set; } = null!;

        /// <summary>
        /// Optional document name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Optional document labels.
        /// </summary>
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Optional document tags.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BatchIngestDocument() { }

        /// <summary>
        /// Instantiate with content.
        /// </summary>
        public BatchIngestDocument(object content, string? name = null, List<string>? labels = null, Dictionary<string, string>? tags = null)
        {
            Content = content;
            Name = name;
            Labels = labels;
            Tags = tags;
        }
    }
}
