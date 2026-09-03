namespace Lattice.Core.Models
{
    /// <summary>
    /// Whether a permission grants or denies access. Deny always wins within a matched tenant and scope.
    /// </summary>
    public enum PermissionType
    {
        /// <summary>Grant the operation.</summary>
        Permit,
        /// <summary>Deny the operation (overrides any Permit).</summary>
        Deny
    }
}
