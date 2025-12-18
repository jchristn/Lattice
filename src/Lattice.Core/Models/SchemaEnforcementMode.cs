namespace Lattice.Core.Models
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines how schema constraints are enforced for documents in a collection.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SchemaEnforcementMode
    {
        /// <summary>
        /// No schema enforcement (current default behavior). Documents are accepted without validation.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// Document must have exactly the defined fields, no more, no less.
        /// Extra fields will cause validation failure.
        /// </summary>
        [EnumMember(Value = "Strict")]
        Strict = 1,

        /// <summary>
        /// Document must have required fields but may have additional fields.
        /// Extra fields are allowed and stored but not validated.
        /// </summary>
        [EnumMember(Value = "Flexible")]
        Flexible = 2,

        /// <summary>
        /// Only validate specified fields, ignore others entirely.
        /// Non-specified fields are allowed without validation.
        /// </summary>
        [EnumMember(Value = "Partial")]
        Partial = 3
    }
}
