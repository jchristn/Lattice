namespace Lattice.Server.API.MCP
{
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Security;

    /// <summary>
    /// Maps a Lattice MCP tool name to the resource and operation it requires and authorizes it against the
    /// same <see cref="AuthorizationService"/> as the REST API, so the MCP transport and the REST API
    /// cannot drift in what a principal is allowed to do.
    /// </summary>
    public static class McpToolAuthorization
    {
        #region Public-Methods

        /// <summary>Authorize a tool call for the given caller.</summary>
        /// <param name="authz">Authorization service.</param>
        /// <param name="caller">The resolved principal.</param>
        /// <param name="toolName">The tool name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True when the caller may invoke the tool.</returns>
        public static async Task<bool> AuthorizeAsync(AuthorizationService authz, CallerContext caller, string toolName, CancellationToken token = default)
        {
            if (caller == null || !caller.IsAuthenticated) return false;

            switch (toolName)
            {
                case "lattice_capabilities":
                case "lattice_whoami":
                    return true;

                case "lattice_list_collections":
                case "lattice_get_collection":
                    return await PermittedAsync(authz, caller, ResourceType.Collection, OperationType.Read, token).ConfigureAwait(false);
                case "lattice_create_collection":
                    return await PermittedAsync(authz, caller, ResourceType.Collection, OperationType.Create, token).ConfigureAwait(false);
                case "lattice_delete_collection":
                    return await PermittedAsync(authz, caller, ResourceType.Collection, OperationType.Delete, token).ConfigureAwait(false);

                case "lattice_list_documents":
                case "lattice_get_document":
                case "lattice_search_documents":
                    return await PermittedAsync(authz, caller, ResourceType.Document, OperationType.Read, token).ConfigureAwait(false);
                case "lattice_create_document":
                    return await PermittedAsync(authz, caller, ResourceType.Document, OperationType.Create, token).ConfigureAwait(false);
                case "lattice_delete_document":
                    return await PermittedAsync(authz, caller, ResourceType.Document, OperationType.Delete, token).ConfigureAwait(false);

                case "lattice_list_schemas":
                case "lattice_get_schema":
                case "lattice_get_schema_elements":
                    return await PermittedAsync(authz, caller, ResourceType.Schema, OperationType.Read, token).ConfigureAwait(false);

                case "lattice_list_tables":
                case "lattice_get_table_entries":
                    return await PermittedAsync(authz, caller, ResourceType.Index, OperationType.Read, token).ConfigureAwait(false);

                case "lattice_list_tenants":
                    return await PermittedAsync(authz, caller, ResourceType.Tenant, OperationType.Read, token).ConfigureAwait(false);
                case "lattice_list_users":
                    return await PermittedAsync(authz, caller, ResourceType.User, OperationType.Read, token).ConfigureAwait(false);
                case "lattice_list_credentials":
                    return await PermittedAsync(authz, caller, ResourceType.Credential, OperationType.Read, token).ConfigureAwait(false);
                case "lattice_create_credential":
                    return await PermittedAsync(authz, caller, ResourceType.Credential, OperationType.Create, token).ConfigureAwait(false);
                case "lattice_list_roles":
                    return await PermittedAsync(authz, caller, ResourceType.Role, OperationType.Read, token).ConfigureAwait(false);
                case "lattice_list_audit":
                    return await PermittedAsync(authz, caller, ResourceType.Audit, OperationType.Read, token).ConfigureAwait(false);

                default:
                    return false;
            }
        }

        #endregion

        #region Private-Methods

        private static async Task<bool> PermittedAsync(AuthorizationService authz, CallerContext caller, ResourceType resourceType, OperationType operation, CancellationToken token)
        {
            AuthorizationVerdict verdict = await authz.AuthorizeAsync(caller, resourceType, operation, null, token).ConfigureAwait(false);
            return verdict == AuthorizationVerdict.Permitted;
        }

        #endregion
    }
}
