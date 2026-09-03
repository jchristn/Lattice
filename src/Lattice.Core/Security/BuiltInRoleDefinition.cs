namespace Lattice.Core.Security
{
    using System.Collections.Generic;

    /// <summary>
    /// The definition of a built-in role: its name and the permissions it grants. Built-in roles are
    /// seeded globally (with a null tenant) at first boot and refreshed on each startup.
    /// </summary>
    public class BuiltInRoleDefinition
    {
        /// <summary>
        /// The role name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The permissions the role grants.
        /// </summary>
        public List<BuiltInRolePermission> Permissions { get; set; } = new List<BuiltInRolePermission>();

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public BuiltInRoleDefinition()
        {
        }
    }
}
