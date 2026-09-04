namespace Lattice.Server.Classes
{
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// A single grant within a role: whether it permits or denies a set of operations on a set of resource
    /// types. A role is defined by a list of these.
    /// </summary>
    public class RolePermissionSpec
    {
        /// <summary>Whether this grant permits or denies. Deny wins over permit during evaluation.</summary>
        public PermissionType PermissionType { get; set; } = PermissionType.Permit;

        /// <summary>The resource types the grant covers.</summary>
        public List<ResourceType> ResourceTypes { get; set; } = new List<ResourceType>();

        /// <summary>The operations the grant covers (Write expands to Create, Update, and Delete).</summary>
        public List<OperationType> OperationTypes { get; set; } = new List<OperationType>();
    }
}
