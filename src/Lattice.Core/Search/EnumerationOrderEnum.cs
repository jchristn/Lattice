namespace Lattice.Core.Search
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Enumeration ordering options.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnumerationOrderEnum
    {
        /// <summary>
        /// Sort by creation time, oldest first.
        /// </summary>
        [EnumMember(Value = "CreatedAscending")]
        CreatedAscending,

        /// <summary>
        /// Sort by creation time, newest first.
        /// </summary>
        [EnumMember(Value = "CreatedDescending")]
        CreatedDescending,

        /// <summary>
        /// Sort by last update time, oldest first.
        /// </summary>
        [EnumMember(Value = "LastUpdateAscending")]
        LastUpdateAscending,

        /// <summary>
        /// Sort by last update time, newest first.
        /// </summary>
        [EnumMember(Value = "LastUpdateDescending")]
        LastUpdateDescending,

        /// <summary>
        /// Sort by name, A to Z.
        /// </summary>
        [EnumMember(Value = "NameAscending")]
        NameAscending,

        /// <summary>
        /// Sort by name, Z to A.
        /// </summary>
        [EnumMember(Value = "NameDescending")]
        NameDescending
    }
}
