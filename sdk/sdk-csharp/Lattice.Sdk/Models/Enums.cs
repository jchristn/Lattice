using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Schema enforcement mode for collections.
    /// </summary>
    public enum SchemaEnforcementMode
    {
        /// <summary>
        /// No schema enforcement.
        /// </summary>
        None = 0,
        /// <summary>
        /// Strict schema enforcement - all documents must match exactly.
        /// </summary>
        Strict = 1,
        /// <summary>
        /// Flexible schema enforcement - allows additional fields.
        /// </summary>
        Flexible = 2,
        /// <summary>
        /// Partial schema enforcement - validates specified fields only.
        /// </summary>
        Partial = 3
    }

    /// <summary>
    /// Indexing mode for collections.
    /// </summary>
    public enum IndexingMode
    {
        /// <summary>
        /// Index all fields automatically.
        /// </summary>
        All = 0,
        /// <summary>
        /// Index only selected fields.
        /// </summary>
        Selective = 1,
        /// <summary>
        /// Do not index any fields.
        /// </summary>
        None = 2
    }

    /// <summary>
    /// Search condition operators.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SearchCondition
    {
        /// <summary>
        /// Field value equals the specified value.
        /// </summary>
        Equals,
        /// <summary>
        /// Field value does not equal the specified value.
        /// </summary>
        NotEquals,
        /// <summary>
        /// Field value is greater than the specified value.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Field value is greater than or equal to the specified value.
        /// </summary>
        GreaterThanOrEqualTo,
        /// <summary>
        /// Field value is less than the specified value.
        /// </summary>
        LessThan,
        /// <summary>
        /// Field value is less than or equal to the specified value.
        /// </summary>
        LessThanOrEqualTo,
        /// <summary>
        /// Field value is null.
        /// </summary>
        IsNull,
        /// <summary>
        /// Field value is not null.
        /// </summary>
        IsNotNull,
        /// <summary>
        /// Field value contains the specified substring.
        /// </summary>
        Contains,
        /// <summary>
        /// Field value starts with the specified prefix.
        /// </summary>
        StartsWith,
        /// <summary>
        /// Field value ends with the specified suffix.
        /// </summary>
        EndsWith,
        /// <summary>
        /// Field value matches the specified pattern (SQL LIKE).
        /// </summary>
        Like
    }

    /// <summary>
    /// Enumeration ordering options.
    /// </summary>
    public enum EnumerationOrder
    {
        /// <summary>
        /// Order by creation date, oldest first.
        /// </summary>
        CreatedAscending,
        /// <summary>
        /// Order by creation date, newest first.
        /// </summary>
        CreatedDescending,
        /// <summary>
        /// Order by last update date, oldest first.
        /// </summary>
        LastUpdateAscending,
        /// <summary>
        /// Order by last update date, newest first.
        /// </summary>
        LastUpdateDescending,
        /// <summary>
        /// Order by name, A to Z.
        /// </summary>
        NameAscending,
        /// <summary>
        /// Order by name, Z to A.
        /// </summary>
        NameDescending
    }

    /// <summary>
    /// Data types for field constraints and schema elements.
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// String data type.
        /// </summary>
        String,
        /// <summary>
        /// Integer data type.
        /// </summary>
        Integer,
        /// <summary>
        /// Numeric (decimal) data type.
        /// </summary>
        Number,
        /// <summary>
        /// Boolean data type.
        /// </summary>
        Boolean,
        /// <summary>
        /// Array data type.
        /// </summary>
        Array,
        /// <summary>
        /// Object (nested JSON) data type.
        /// </summary>
        Object,
        /// <summary>
        /// Null data type.
        /// </summary>
        Null
    }
}
