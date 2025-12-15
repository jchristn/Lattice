namespace Lattice.Core.Models
{
    /// <summary>
    /// Defines how schema constraints are enforced for documents in a collection.
    /// </summary>
    public enum SchemaEnforcementMode
    {
        /// <summary>
        /// No schema enforcement (current default behavior). Documents are accepted without validation.
        /// </summary>
        None = 0,

        /// <summary>
        /// Document must have exactly the defined fields, no more, no less.
        /// Extra fields will cause validation failure.
        /// </summary>
        Strict = 1,

        /// <summary>
        /// Document must have required fields but may have additional fields.
        /// Extra fields are allowed and stored but not validated.
        /// </summary>
        Flexible = 2,

        /// <summary>
        /// Only validate specified fields, ignore others entirely.
        /// Non-specified fields are allowed without validation.
        /// </summary>
        Partial = 3
    }
}
