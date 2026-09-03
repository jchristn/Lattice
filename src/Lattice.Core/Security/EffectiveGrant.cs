namespace Lattice.Core.Security
{
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// A flattened permission tuple contributed by a principal's role and scope assignments. The
    /// evaluator matches a request against the full set of a principal's grants.
    /// </summary>
    public class EffectiveGrant
    {
        /// <summary>
        /// Whether this grant permits or denies. Deny wins over Permit.
        /// </summary>
        public PermissionType PermissionType { get; set; } = PermissionType.Permit;

        /// <summary>
        /// The resource types the grant covers.
        /// </summary>
        public List<ResourceType> ResourceTypes { get; set; } = new List<ResourceType>();

        /// <summary>
        /// The operation types the grant covers.
        /// </summary>
        public List<OperationType> OperationTypes { get; set; } = new List<OperationType>();

        /// <summary>
        /// Whether the grant applies tenant-wide or to a specific resource.
        /// </summary>
        public ResourceScope ResourceScope { get; set; } = ResourceScope.Tenant;

        /// <summary>
        /// The specific resource id for a Resource-scoped grant, or null.
        /// </summary>
        public string ResourceId { get; set; } = null;

        /// <summary>
        /// Whether a Tenant-scoped grant also satisfies checks on child resources.
        /// </summary>
        public bool InheritsToChildren { get; set; } = true;

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public EffectiveGrant()
        {
        }
    }
}
