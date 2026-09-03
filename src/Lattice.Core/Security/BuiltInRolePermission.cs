namespace Lattice.Core.Security
{
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// A single permission that a built-in role grants or denies, used when seeding built-in roles.
    /// </summary>
    public class BuiltInRolePermission
    {
        /// <summary>
        /// Whether the permission permits or denies.
        /// </summary>
        public PermissionType PermissionType { get; set; } = PermissionType.Permit;

        /// <summary>
        /// The resource types the permission covers.
        /// </summary>
        public List<ResourceType> ResourceTypes { get; set; } = new List<ResourceType>();

        /// <summary>
        /// The operation types the permission covers.
        /// </summary>
        public List<OperationType> OperationTypes { get; set; } = new List<OperationType>();

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public BuiltInRolePermission()
        {
        }
    }
}
