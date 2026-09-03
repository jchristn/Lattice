namespace Lattice.Server.Security
{
    using System;
    using Lattice.Core.Models;

    /// <summary>
    /// The single, centralized mapping from an HTTP request (method and path) to the permission it
    /// requires. Both the REST pipeline and the MCP invoker resolve permissions here so the two cannot
    /// drift. For a payload whose intent depends on its content (search), the caller classifies it
    /// before resolving; this map treats the search route as a read.
    /// </summary>
    public static class RequestPermissionMap
    {
        /// <summary>
        /// Resolve the permission a request requires.
        /// </summary>
        /// <param name="method">HTTP method (GET, PUT, POST, DELETE, HEAD).</param>
        /// <param name="path">Request path without query string.</param>
        /// <returns>The required permission.</returns>
        public static RequiredPermission Resolve(string method, string path)
        {
            string p = (path ?? String.Empty).ToLowerInvariant();
            string m = (method ?? "GET").ToUpperInvariant();

            if (p == "/" || p.StartsWith("/v1.0/health", StringComparison.Ordinal)) return RequiredPermission.ForPublic();
            if (p.StartsWith("/openapi", StringComparison.Ordinal) || p.StartsWith("/swagger", StringComparison.Ordinal)) return RequiredPermission.ForPublic();
            // The Prometheus scrape endpoint is unauthenticated. It is normally served on a separate
            // telemetry listener, but if it is ever routed through this server it must stay public.
            if (p == "/metrics" || p.StartsWith("/metrics/", StringComparison.Ordinal)) return RequiredPermission.ForPublic();
            if (p.StartsWith("/v1.0/token", StringComparison.Ordinal))
            {
                // Creating or logging in is public; validating/refreshing/revoking a token needs the token itself.
                if (m == "POST") return RequiredPermission.ForPublic();
                return RequiredPermission.ForAnyAuthenticated();
            }
            if (p.StartsWith("/v1.0/whoami", StringComparison.Ordinal)) return RequiredPermission.ForAnyAuthenticated();

            // Management surfaces.
            if (p.StartsWith("/v1.0/tenants", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Tenant, OperationFor(m));
            if (p.StartsWith("/v1.0/users", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.User, OperationFor(m));
            if (p.StartsWith("/v1.0/credentials", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Credential, OperationFor(m));
            if (p.StartsWith("/v1.0/roles", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Role, OperationFor(m));
            if (p.StartsWith("/v1.0/permissions", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Permission, OperationFor(m));
            if (p.StartsWith("/v1.0/assignments", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Assignment, OperationFor(m));
            if (p.StartsWith("/v1.0/audit", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Audit, m == "DELETE" ? OperationType.Delete : OperationType.Read);

            // Data plane.
            if (p.StartsWith("/v1.0/requesthistory", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.RequestHistory, m == "DELETE" ? OperationType.Delete : OperationType.Read);
            if (p.StartsWith("/v1.0/schemas", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Schema, OperationFor(m));
            if (p.StartsWith("/v1.0/tables", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Index, OperationFor(m));

            if (p.Contains("/documents/search")) return new RequiredPermission(ResourceType.Document, OperationType.Read);
            if (p.Contains("/indexes/rebuild")) return new RequiredPermission(ResourceType.Index, OperationType.Execute);
            if (p.Contains("/documents")) return new RequiredPermission(ResourceType.Document, OperationFor(m));
            if (p.Contains("/constraints") || p.Contains("/indexing")) return new RequiredPermission(ResourceType.Collection, OperationFor(m));
            if (p.StartsWith("/v1.0/collections", StringComparison.Ordinal)) return new RequiredPermission(ResourceType.Collection, OperationFor(m));

            // Default: require admin on the platform for anything unclassified.
            return new RequiredPermission(ResourceType.All, OperationType.Admin);
        }

        private static OperationType OperationFor(string method)
        {
            switch (method)
            {
                case "GET":
                case "HEAD":
                    return OperationType.Read;
                case "DELETE":
                    return OperationType.Delete;
                case "PUT":
                case "POST":
                case "PATCH":
                    return OperationType.Write;
                default:
                    return OperationType.Read;
            }
        }
    }
}
