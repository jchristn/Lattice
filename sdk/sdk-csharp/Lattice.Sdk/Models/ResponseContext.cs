using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents an index table mapping.
    /// </summary>
    public class IndexTableMapping
    {
        /// <summary>
        /// Gets or sets the key (field path) for this mapping.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database table name for this index.
        /// </summary>
        [JsonPropertyName("tableName")]
        public string TableName { get; set; } = string.Empty;
    }

}
