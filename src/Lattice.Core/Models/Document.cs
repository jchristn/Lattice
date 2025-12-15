namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a JSON document stored in Lattice.
    /// </summary>
    public class Document
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the document (doc_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the collection this document belongs to.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Identifier of the schema associated with this document.
        /// </summary>
        public string SchemaId { get; set; } = null;

        /// <summary>
        /// Name of the document.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Labels associated with the document.
        /// </summary>
        public List<string> Labels
        {
            get => _Labels;
            set => _Labels = value ?? new List<string>();
        }

        /// <summary>
        /// Key-value tags associated with the document.
        /// </summary>
        public Dictionary<string, string> Tags
        {
            get => _Tags;
            set => _Tags = value ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Timestamp when the document was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the document was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The raw JSON content of the document (populated when retrieving full document).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Content { get; set; } = null;

        /// <summary>
        /// The length of the document content in bytes.
        /// </summary>
        public long ContentLength
        {
            get => _ContentLength;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(ContentLength), "Content length cannot be negative.");
                _ContentLength = value;
            }
        }

        /// <summary>
        /// SHA256 hash of the document content.
        /// </summary>
        public string Sha256Hash { get; set; } = null;

        #endregion

        #region Private-Members

        private List<string> _Labels = new List<string>();
        private Dictionary<string, string> _Tags = new Dictionary<string, string>();
        private long _ContentLength = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Document()
        {
        }

        #endregion
    }
}
