namespace Lattice.Core.Search
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Search condition operators.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SearchConditionEnum
    {
        /// <summary>
        /// Left and right terms are equal.
        /// </summary>
        [EnumMember(Value = "Equals")]
        Equals,

        /// <summary>
        /// Left and right terms are not equal.
        /// </summary>
        [EnumMember(Value = "NotEquals")]
        NotEquals,

        /// <summary>
        /// Left term is greater than right term.
        /// </summary>
        [EnumMember(Value = "GreaterThan")]
        GreaterThan,

        /// <summary>
        /// Left term is greater than or equal to right term.
        /// </summary>
        [EnumMember(Value = "GreaterThanOrEqualTo")]
        GreaterThanOrEqualTo,

        /// <summary>
        /// Left term is less than right term.
        /// </summary>
        [EnumMember(Value = "LessThan")]
        LessThan,

        /// <summary>
        /// Left term is less than or equal to right term.
        /// </summary>
        [EnumMember(Value = "LessThanOrEqualTo")]
        LessThanOrEqualTo,

        /// <summary>
        /// Left term is null.
        /// </summary>
        [EnumMember(Value = "IsNull")]
        IsNull,

        /// <summary>
        /// Left term is not null.
        /// </summary>
        [EnumMember(Value = "IsNotNull")]
        IsNotNull,

        /// <summary>
        /// Left term contains right term.
        /// </summary>
        [EnumMember(Value = "Contains")]
        Contains,

        /// <summary>
        /// Left term starts with right term.
        /// </summary>
        [EnumMember(Value = "StartsWith")]
        StartsWith,

        /// <summary>
        /// Left term ends with right term.
        /// </summary>
        [EnumMember(Value = "EndsWith")]
        EndsWith,

        /// <summary>
        /// Left term matches a pattern (SQL LIKE).
        /// </summary>
        [EnumMember(Value = "Like")]
        Like
    }
}
