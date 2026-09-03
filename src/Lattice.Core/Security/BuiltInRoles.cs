namespace Lattice.Core.Security
{
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// The catalog of built-in roles seeded for every deployment. These are global (null tenant),
    /// protected, and refreshed on startup while preserving their identifiers.
    /// </summary>
    public static class BuiltInRoles
    {
        /// <summary>Role name: full access within a tenant.</summary>
        public const string TenantAdmin = "TenantAdmin";

        /// <summary>Role name: manage users, credentials, roles, and audit.</summary>
        public const string SecurityAdmin = "SecurityAdmin";

        /// <summary>Role name: read-only access to security surfaces.</summary>
        public const string Auditor = "Auditor";

        /// <summary>Role name: full access to the document-store data plane.</summary>
        public const string CollectionAdmin = "CollectionAdmin";

        /// <summary>Role name: read and write documents, collections, schemas, and indexes.</summary>
        public const string Editor = "Editor";

        /// <summary>Role name: read documents, collections, schemas, and indexes.</summary>
        public const string Viewer = "Viewer";

        /// <summary>Role name: baseline read access to core data resources.</summary>
        public const string TenantMember = "TenantMember";

        /// <summary>
        /// Get the built-in role definitions to seed.
        /// </summary>
        /// <returns>The definitions.</returns>
        public static List<BuiltInRoleDefinition> All()
        {
            List<BuiltInRoleDefinition> roles = new List<BuiltInRoleDefinition>();

            roles.Add(new BuiltInRoleDefinition
            {
                Name = TenantAdmin,
                Permissions = new List<BuiltInRolePermission>
                {
                    Permit(new List<ResourceType> { ResourceType.All }, new List<OperationType> { OperationType.All })
                }
            });

            roles.Add(new BuiltInRoleDefinition
            {
                Name = SecurityAdmin,
                Permissions = new List<BuiltInRolePermission>
                {
                    Permit(
                        new List<ResourceType> { ResourceType.User, ResourceType.Credential, ResourceType.Role, ResourceType.Permission, ResourceType.Assignment, ResourceType.Session, ResourceType.Audit },
                        new List<OperationType> { OperationType.Read, OperationType.Write, OperationType.Admin })
                }
            });

            roles.Add(new BuiltInRoleDefinition
            {
                Name = Auditor,
                Permissions = new List<BuiltInRolePermission>
                {
                    Permit(
                        new List<ResourceType> { ResourceType.Audit, ResourceType.User, ResourceType.Credential, ResourceType.Role, ResourceType.Permission, ResourceType.Assignment, ResourceType.Session, ResourceType.RequestHistory },
                        new List<OperationType> { OperationType.Read })
                }
            });

            roles.Add(new BuiltInRoleDefinition
            {
                Name = CollectionAdmin,
                Permissions = new List<BuiltInRolePermission>
                {
                    Permit(
                        new List<ResourceType> { ResourceType.Collection, ResourceType.Document, ResourceType.Schema, ResourceType.Index },
                        new List<OperationType> { OperationType.All })
                }
            });

            roles.Add(new BuiltInRoleDefinition
            {
                Name = Editor,
                Permissions = new List<BuiltInRolePermission>
                {
                    Permit(
                        new List<ResourceType> { ResourceType.Collection, ResourceType.Document, ResourceType.Schema, ResourceType.Index },
                        new List<OperationType> { OperationType.Read, OperationType.Write })
                }
            });

            roles.Add(new BuiltInRoleDefinition
            {
                Name = Viewer,
                Permissions = new List<BuiltInRolePermission>
                {
                    Permit(
                        new List<ResourceType> { ResourceType.Collection, ResourceType.Document, ResourceType.Schema, ResourceType.Index, ResourceType.RequestHistory },
                        new List<OperationType> { OperationType.Read })
                }
            });

            roles.Add(new BuiltInRoleDefinition
            {
                Name = TenantMember,
                Permissions = new List<BuiltInRolePermission>
                {
                    Permit(
                        new List<ResourceType> { ResourceType.Collection, ResourceType.Document, ResourceType.Schema },
                        new List<OperationType> { OperationType.Read })
                }
            });

            return roles;
        }

        private static BuiltInRolePermission Permit(List<ResourceType> resourceTypes, List<OperationType> operationTypes)
        {
            return new BuiltInRolePermission
            {
                PermissionType = PermissionType.Permit,
                ResourceTypes = resourceTypes,
                OperationTypes = operationTypes
            };
        }
    }
}
