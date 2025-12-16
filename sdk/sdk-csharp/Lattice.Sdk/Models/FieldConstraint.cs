using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents a field constraint for schema validation.
    /// </summary>
    public class FieldConstraint
    {
        /// <summary>
        /// Gets or sets the unique identifier of the constraint.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the collection this constraint belongs to.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string? CollectionId { get; set; }

        /// <summary>
        /// Gets or sets the JSON path of the field to constrain.
        /// </summary>
        [JsonPropertyName("fieldPath")]
        public string FieldPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected data type for the field.
        /// </summary>
        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }

        /// <summary>
        /// Gets or sets whether the field is required.
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets whether the field can be null.
        /// </summary>
        [JsonPropertyName("nullable")]
        public bool Nullable { get; set; } = true;

        /// <summary>
        /// Gets or sets the regex pattern that the field value must match.
        /// </summary>
        [JsonPropertyName("regexPattern")]
        public string? RegexPattern { get; set; }

        /// <summary>
        /// Gets or sets the minimum allowed value for numeric fields.
        /// </summary>
        [JsonPropertyName("minValue")]
        public double? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed value for numeric fields.
        /// </summary>
        [JsonPropertyName("maxValue")]
        public double? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the minimum allowed length for string fields.
        /// </summary>
        [JsonPropertyName("minLength")]
        public int? MinLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed length for string fields.
        /// </summary>
        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the list of allowed values for the field.
        /// </summary>
        [JsonPropertyName("allowedValues")]
        public List<string>? AllowedValues { get; set; }

        /// <summary>
        /// Gets or sets the expected element type for array fields.
        /// </summary>
        [JsonPropertyName("arrayElementType")]
        public string? ArrayElementType { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the constraint was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the constraint was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime? LastUpdateUtc { get; set; }
    }

    /// <summary>
    /// Represents the response from the constraints endpoint.
    /// </summary>
    public class ConstraintsResponse
    {
        /// <summary>
        /// Gets or sets the collection ID.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the schema enforcement mode for the collection.
        /// </summary>
        [JsonPropertyName("schemaEnforcementMode")]
        public SchemaEnforcementMode SchemaEnforcementMode { get; set; }

        /// <summary>
        /// Gets or sets the list of field constraints.
        /// </summary>
        [JsonPropertyName("fieldConstraints")]
        public List<FieldConstraint> FieldConstraints { get; set; } = new List<FieldConstraint>();
    }

    /// <summary>
    /// Represents the response from the indexing endpoint.
    /// </summary>
    public class IndexingConfiguration
    {
        /// <summary>
        /// Gets or sets the collection ID.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the indexing mode for the collection.
        /// </summary>
        [JsonPropertyName("indexingMode")]
        public IndexingMode IndexingMode { get; set; }

        /// <summary>
        /// Gets or sets the list of indexed fields.
        /// </summary>
        [JsonPropertyName("indexedFields")]
        public List<IndexedField> IndexedFields { get; set; } = new List<IndexedField>();
    }

    /// <summary>
    /// Represents an indexed field in a collection.
    /// </summary>
    public class IndexedField
    {
        /// <summary>
        /// Gets or sets the unique identifier of the indexed field.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the collection this indexed field belongs to.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON path of the indexed field.
        /// </summary>
        [JsonPropertyName("fieldPath")]
        public string FieldPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the index was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the index was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime? LastUpdateUtc { get; set; }
    }
}
