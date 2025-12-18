using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Schema enforcement mode for collections.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SchemaEnforcementMode
    {
        /// <summary>
        /// No schema enforcement.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,
        /// <summary>
        /// Strict schema enforcement - all documents must match exactly.
        /// </summary>
        [EnumMember(Value = "Strict")]
        Strict = 1,
        /// <summary>
        /// Flexible schema enforcement - allows additional fields.
        /// </summary>
        [EnumMember(Value = "Flexible")]
        Flexible = 2,
        /// <summary>
        /// Partial schema enforcement - validates specified fields only.
        /// </summary>
        [EnumMember(Value = "Partial")]
        Partial = 3
    }

    /// <summary>
    /// Indexing mode for collections.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IndexingMode
    {
        /// <summary>
        /// Index all fields automatically.
        /// </summary>
        [EnumMember(Value = "All")]
        All = 0,
        /// <summary>
        /// Index only selected fields.
        /// </summary>
        [EnumMember(Value = "Selective")]
        Selective = 1,
        /// <summary>
        /// Do not index any fields.
        /// </summary>
        [EnumMember(Value = "None")]
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
        [EnumMember(Value = "Equals")]
        Equals,
        /// <summary>
        /// Field value does not equal the specified value.
        /// </summary>
        [EnumMember(Value = "NotEquals")]
        NotEquals,
        /// <summary>
        /// Field value is greater than the specified value.
        /// </summary>
        [EnumMember(Value = "GreaterThan")]
        GreaterThan,
        /// <summary>
        /// Field value is greater than or equal to the specified value.
        /// </summary>
        [EnumMember(Value = "GreaterThanOrEqualTo")]
        GreaterThanOrEqualTo,
        /// <summary>
        /// Field value is less than the specified value.
        /// </summary>
        [EnumMember(Value = "LessThan")]
        LessThan,
        /// <summary>
        /// Field value is less than or equal to the specified value.
        /// </summary>
        [EnumMember(Value = "LessThanOrEqualTo")]
        LessThanOrEqualTo,
        /// <summary>
        /// Field value is null.
        /// </summary>
        [EnumMember(Value = "IsNull")]
        IsNull,
        /// <summary>
        /// Field value is not null.
        /// </summary>
        [EnumMember(Value = "IsNotNull")]
        IsNotNull,
        /// <summary>
        /// Field value contains the specified substring.
        /// </summary>
        [EnumMember(Value = "Contains")]
        Contains,
        /// <summary>
        /// Field value starts with the specified prefix.
        /// </summary>
        [EnumMember(Value = "StartsWith")]
        StartsWith,
        /// <summary>
        /// Field value ends with the specified suffix.
        /// </summary>
        [EnumMember(Value = "EndsWith")]
        EndsWith,
        /// <summary>
        /// Field value matches the specified pattern (SQL LIKE).
        /// </summary>
        [EnumMember(Value = "Like")]
        Like
    }

    /// <summary>
    /// Enumeration ordering options.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnumerationOrder
    {
        /// <summary>
        /// Order by creation date, oldest first.
        /// </summary>
        [EnumMember(Value = "CreatedAscending")]
        CreatedAscending,
        /// <summary>
        /// Order by creation date, newest first.
        /// </summary>
        [EnumMember(Value = "CreatedDescending")]
        CreatedDescending,
        /// <summary>
        /// Order by last update date, oldest first.
        /// </summary>
        [EnumMember(Value = "LastUpdateAscending")]
        LastUpdateAscending,
        /// <summary>
        /// Order by last update date, newest first.
        /// </summary>
        [EnumMember(Value = "LastUpdateDescending")]
        LastUpdateDescending,
        /// <summary>
        /// Order by name, A to Z.
        /// </summary>
        [EnumMember(Value = "NameAscending")]
        NameAscending,
        /// <summary>
        /// Order by name, Z to A.
        /// </summary>
        [EnumMember(Value = "NameDescending")]
        NameDescending
    }

    /// <summary>
    /// Data types for field constraints and schema elements.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DataType
    {
        /// <summary>
        /// String data type.
        /// </summary>
        [EnumMember(Value = "string")]
        String,
        /// <summary>
        /// Integer data type.
        /// </summary>
        [EnumMember(Value = "integer")]
        Integer,
        /// <summary>
        /// Numeric (decimal) data type.
        /// </summary>
        [EnumMember(Value = "number")]
        Number,
        /// <summary>
        /// Boolean data type.
        /// </summary>
        [EnumMember(Value = "boolean")]
        Boolean,
        /// <summary>
        /// Array data type.
        /// </summary>
        [EnumMember(Value = "array")]
        Array,
        /// <summary>
        /// Object (nested JSON) data type.
        /// </summary>
        [EnumMember(Value = "object")]
        Object,
        /// <summary>
        /// Null data type.
        /// </summary>
        [EnumMember(Value = "null")]
        Null
    }
}
