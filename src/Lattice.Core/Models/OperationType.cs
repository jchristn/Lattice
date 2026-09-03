namespace Lattice.Core.Models
{
    /// <summary>
    /// The operation an authorization decision applies to.
    /// </summary>
    public enum OperationType
    {
        /// <summary>All operations (wildcard).</summary>
        All,
        /// <summary>Create a resource.</summary>
        Create,
        /// <summary>Read a resource.</summary>
        Read,
        /// <summary>Shorthand that expands to Create, Update, and Delete.</summary>
        Write,
        /// <summary>Update a resource.</summary>
        Update,
        /// <summary>Delete a resource.</summary>
        Delete,
        /// <summary>Execute an action (for example an index rebuild).</summary>
        Execute,
        /// <summary>Administer a resource type (management operations).</summary>
        Admin
    }
}
