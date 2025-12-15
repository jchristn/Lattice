namespace Lattice.Core
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Database types supported by Lattice.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DatabaseTypeEnum
    {
        /// <summary>
        /// SQLite database.
        /// </summary>
        [EnumMember(Value = "Sqlite")]
        Sqlite,

        /// <summary>
        /// MySQL database.
        /// </summary>
        [EnumMember(Value = "Mysql")]
        Mysql,

        /// <summary>
        /// PostgreSQL database.
        /// </summary>
        [EnumMember(Value = "Postgres")]
        Postgres,

        /// <summary>
        /// SQL Server database.
        /// </summary>
        [EnumMember(Value = "SqlServer")]
        SqlServer
    }
}
