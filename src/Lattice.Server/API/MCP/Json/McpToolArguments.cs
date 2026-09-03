namespace Lattice.Server.API.MCP.Json
{
    using System.Collections.Generic;

    /// <summary>
    /// The flat set of arguments an MCP tool may receive. Every field is optional; each tool reads only the
    /// fields it needs. Modeling arguments as one named type keeps the code free of DOM traversal while
    /// still accepting the union of all tool inputs.
    /// </summary>
    public class McpToolArguments
    {
        /// <summary>Target collection identifier.</summary>
        public string CollectionId { get; set; } = null;

        /// <summary>Target document identifier.</summary>
        public string DocumentId { get; set; } = null;

        /// <summary>Target schema identifier.</summary>
        public string SchemaId { get; set; } = null;

        /// <summary>Target index table name.</summary>
        public string TableName { get; set; } = null;

        /// <summary>Tenant identifier (system administrators only, to target another tenant).</summary>
        public string TenantId { get; set; } = null;

        /// <summary>User identifier.</summary>
        public string UserId { get; set; } = null;

        /// <summary>Resource name (collection name, credential name, and so on).</summary>
        public string Name { get; set; } = null;

        /// <summary>Optional description.</summary>
        public string Description { get; set; } = null;

        /// <summary>Arbitrary JSON document content (re-serialized verbatim before ingestion).</summary>
        public object Content { get; set; } = null;

        /// <summary>Labels for categorization.</summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>Key-value metadata tags.</summary>
        public Dictionary<string, string> Tags { get; set; } = null;

        /// <summary>SQL-like search expression.</summary>
        public string SqlExpression { get; set; } = null;

        /// <summary>Maximum number of results to return.</summary>
        public int? MaxResults { get; set; } = null;

        /// <summary>Number of records to skip.</summary>
        public int? Skip { get; set; } = null;

        /// <summary>Maximum number of index-table entries to return.</summary>
        public int? Limit { get; set; } = null;

        /// <summary>Whether to include document content in the result.</summary>
        public bool? IncludeContent { get; set; } = null;

        /// <summary>Optional audit event-type filter.</summary>
        public string EventType { get; set; } = null;
    }
}
