namespace Lattice.Core.Models
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines how document fields are indexed in a collection.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IndexingMode
    {
        /// <summary>
        /// Index all fields (current default behavior).
        /// Every field in every document is indexed for searchability.
        /// </summary>
        [EnumMember(Value = "All")]
        All = 0,

        /// <summary>
        /// Only index explicitly specified fields.
        /// Use IndexedFields configuration to specify which fields to index.
        /// </summary>
        [EnumMember(Value = "Selective")]
        Selective = 1,

        /// <summary>
        /// Do not create any indexes (document store only).
        /// Documents can only be retrieved by ID, not searched by field values.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 2
    }
}
