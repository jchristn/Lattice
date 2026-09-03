namespace Lattice.Server.API.MCP
{
    using System.Collections.Generic;

    /// <summary>
    /// Static catalog of MCP protocol metadata for the Lattice endpoint: the <c>initialize</c> result, the
    /// capabilities description, and the <c>tools/list</c> schema. Kept separate from request handling so
    /// the tool contract is defined in one place and the dispatcher stays small.
    /// </summary>
    public static class McpToolCatalog
    {
        #region Public-Methods

        /// <summary>Build the <c>initialize</c> result (protocol version, server info, capabilities).</summary>
        /// <param name="serverName">The server name advertised in the handshake.</param>
        /// <returns>The initialize result payload.</returns>
        public static object BuildInitializeResult(string serverName)
        {
            return new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new { name = string.IsNullOrEmpty(serverName) ? "lattice" : serverName, version = "0.3.0" },
                capabilities = new { tools = new { } }
            };
        }

        /// <summary>Build the <c>lattice_capabilities</c> payload describing the platform and paging protocol.</summary>
        /// <returns>The capabilities payload.</returns>
        public static object BuildCapabilities()
        {
            return new
            {
                platform = "Lattice",
                description = "JSON document store with schema validation, full-text indexing, and flexible search.",
                tools = new[]
                {
                    "lattice_capabilities", "lattice_whoami",
                    "lattice_list_collections", "lattice_get_collection", "lattice_create_collection", "lattice_delete_collection",
                    "lattice_list_documents", "lattice_get_document", "lattice_create_document", "lattice_delete_document",
                    "lattice_search_documents",
                    "lattice_list_schemas", "lattice_get_schema", "lattice_get_schema_elements",
                    "lattice_list_tables", "lattice_get_table_entries",
                    "lattice_list_tenants", "lattice_list_users", "lattice_list_credentials", "lattice_create_credential",
                    "lattice_list_roles", "lattice_list_audit"
                },
                enumeration = "List tools are paged. Call a lattice_list_* tool with skip=0; the first result's totalRecords is the exact count. Advance skip by maxResults and repeat until endOfResults is true (equivalently recordsRemaining reaches 0). Fetch a full object individually with the matching lattice_get_* tool."
            };
        }

        /// <summary>Build the <c>tools/list</c> tool descriptors with their JSON-Schema input contracts.</summary>
        /// <returns>The list of tool descriptors.</returns>
        public static List<object> BuildToolList()
        {
            return new List<object>
            {
                Tool("lattice_capabilities", "Describe the Lattice platform and how to enumerate objects. Returns the tool list and the paging protocol.", EmptySchema()),
                Tool("lattice_whoami", "Return the resolved principal for the current credentials (tenant, user, admin status).", EmptySchema()),

                Tool("lattice_list_collections", "Enumerate collections as summaries, paged. Start with skip=0; the first result's totalRecords is the exact total. Advance skip by maxResults until endOfResults is true.", PagingSchema()),
                Tool("lattice_get_collection", "Fetch a single collection by id.", IdSchema("collectionId", "Collection id.")),
                Tool("lattice_create_collection", "Create a collection. Only name is required.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["name"] = StringProp("Collection name (required)."),
                        ["description"] = StringProp("Optional description."),
                        ["labels"] = ArrayProp("Labels for categorization."),
                        ["tags"] = new Dictionary<string, object> { ["type"] = "object", ["description"] = "Key-value metadata tags." }
                    },
                    new[] { "name" })),
                Tool("lattice_delete_collection", "Delete a collection and all its documents. Irreversible.", IdSchema("collectionId", "Collection id.")),

                Tool("lattice_list_documents", "Enumerate documents in a collection as summaries, paged. Same paging protocol as lattice_list_collections.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["collectionId"] = StringProp("Collection id (required)."),
                        ["maxResults"] = IntProp("Page size (clamped 1..1000)."),
                        ["skip"] = IntProp("Number of records to skip.")
                    },
                    new[] { "collectionId" })),
                Tool("lattice_get_document", "Fetch a single document by id. Set includeContent=true to include the raw JSON content.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["collectionId"] = StringProp("Collection id (required)."),
                        ["documentId"] = StringProp("Document id (required)."),
                        ["includeContent"] = BoolProp("Whether to include the raw JSON content.")
                    },
                    new[] { "collectionId", "documentId" })),
                Tool("lattice_create_document", "Ingest a JSON document into a collection. 'content' is the document body as a JSON object.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["collectionId"] = StringProp("Collection id (required)."),
                        ["content"] = new Dictionary<string, object> { ["type"] = "object", ["description"] = "The JSON document content (required)." },
                        ["name"] = StringProp("Optional document name."),
                        ["labels"] = ArrayProp("Labels for categorization."),
                        ["tags"] = new Dictionary<string, object> { ["type"] = "object", ["description"] = "Key-value metadata tags." }
                    },
                    new[] { "collectionId", "content" })),
                Tool("lattice_delete_document", "Delete a document by id.", IdSchema("documentId", "Document id.")),

                Tool("lattice_search_documents", "Search a collection using a SQL-like expression. Returns matching documents with paging.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["collectionId"] = StringProp("Collection id (required)."),
                        ["sqlExpression"] = StringProp("SQL-like expression, e.g. \"Person.Last = 'Smith'\"."),
                        ["maxResults"] = IntProp("Page size (clamped 1..1000)."),
                        ["skip"] = IntProp("Number of records to skip."),
                        ["includeContent"] = BoolProp("Whether to include document content in results.")
                    },
                    new[] { "collectionId" })),

                Tool("lattice_list_schemas", "Enumerate discovered schemas as summaries, paged.", PagingSchema()),
                Tool("lattice_get_schema", "Fetch a single schema by id.", IdSchema("schemaId", "Schema id.")),
                Tool("lattice_get_schema_elements", "Fetch the elements (fields) defined in a schema.", IdSchema("schemaId", "Schema id.")),

                Tool("lattice_list_tables", "Enumerate index-table mappings as summaries, paged.", PagingSchema()),
                Tool("lattice_get_table_entries", "Fetch entries from a specific index table, paged.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["tableName"] = StringProp("Index table name (required)."),
                        ["skip"] = IntProp("Number of entries to skip."),
                        ["limit"] = IntProp("Maximum entries to return (1..1000).")
                    },
                    new[] { "tableName" })),

                Tool("lattice_list_tenants", "Enumerate tenants as summaries, paged (system administrator only).", PagingSchema()),
                Tool("lattice_list_users", "Enumerate users in the caller's tenant, paged. System administrators may pass tenantId to target another tenant.", TenantScopedPagingSchema()),
                Tool("lattice_list_credentials", "Enumerate access-key credentials in the caller's tenant, paged. System administrators may pass tenantId to target another tenant.", TenantScopedPagingSchema()),
                Tool("lattice_create_credential", "Create an access-key credential for a user. The raw access key is returned once and cannot be retrieved again.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["name"] = StringProp("Optional credential name."),
                        ["userId"] = StringProp("Owning user id. Defaults to the calling user."),
                        ["tenantId"] = StringProp("Tenant id (system administrators only).")
                    },
                    null)),
                Tool("lattice_list_roles", "Enumerate roles visible to the caller's tenant, paged.", PagingSchema()),
                Tool("lattice_list_audit", "Enumerate audit entries for the caller's tenant, paged. System administrators may pass tenantId. Optional eventType filter.", ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["tenantId"] = StringProp("Tenant id (system administrators only)."),
                        ["eventType"] = StringProp("Optional event-type filter."),
                        ["maxResults"] = IntProp("Page size (clamped 1..1000)."),
                        ["skip"] = IntProp("Number of records to skip.")
                    },
                    null))
            };
        }

        #endregion

        #region Private-Methods

        private static object Tool(string name, string description, object inputSchema)
        {
            return new { name, description, inputSchema };
        }

        private static object EmptySchema()
        {
            return new { type = "object", properties = new { } };
        }

        private static object IdSchema(string propertyName, string description)
        {
            return ObjectSchema(new Dictionary<string, object> { [propertyName] = StringProp(description) }, new[] { propertyName });
        }

        private static object PagingSchema()
        {
            return ObjectSchema(new Dictionary<string, object>
            {
                ["maxResults"] = IntProp("Page size (clamped 1..1000)."),
                ["skip"] = IntProp("Number of records to skip.")
            }, null);
        }

        private static object TenantScopedPagingSchema()
        {
            return ObjectSchema(new Dictionary<string, object>
            {
                ["tenantId"] = StringProp("Tenant id (system administrators only)."),
                ["maxResults"] = IntProp("Page size (clamped 1..1000)."),
                ["skip"] = IntProp("Number of records to skip.")
            }, null);
        }

        private static object ObjectSchema(Dictionary<string, object> properties, string[] required)
        {
            Dictionary<string, object> schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties
            };
            if (required != null && required.Length > 0) schema["required"] = required;
            return schema;
        }

        private static object StringProp(string description)
        {
            return new Dictionary<string, object> { ["type"] = "string", ["description"] = description };
        }

        private static object IntProp(string description)
        {
            return new Dictionary<string, object> { ["type"] = "integer", ["description"] = description };
        }

        private static object BoolProp(string description)
        {
            return new Dictionary<string, object> { ["type"] = "boolean", ["description"] = description };
        }

        private static object ArrayProp(string description)
        {
            return new Dictionary<string, object> { ["type"] = "array", ["items"] = new Dictionary<string, object> { ["type"] = "string" }, ["description"] = description };
        }

        #endregion
    }
}
