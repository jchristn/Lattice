namespace Lattice.Core.Models
{
    /// <summary>
    /// The breadth of a role or scope assignment.
    /// </summary>
    public enum ResourceScope
    {
        /// <summary>Applies to all resources of the listed types within the tenant.</summary>
        Tenant,
        /// <summary>Applies only to the specific resource identified by the assignment.</summary>
        Resource
    }
}
