namespace Lattice.Server.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Request body to create or update a role: a name and the list of grants the role confers.
    /// </summary>
    public class CreateRoleRequest
    {
        /// <summary>The role name (unique within the tenant).</summary>
        public string Name { get; set; } = null;

        /// <summary>The grants the role confers.</summary>
        public List<RolePermissionSpec> Permissions { get; set; } = new List<RolePermissionSpec>();
    }
}
