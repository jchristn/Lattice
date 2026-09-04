namespace Lattice.Server.Classes
{
    /// <summary>Request body for updating a tenant. Only supplied fields are changed.</summary>
    public class UpdateTenantRequest
    {
        /// <summary>New tenant name, or null to leave unchanged.</summary>
        public string Name { get; set; } = null;

        /// <summary>New active flag, or null to leave unchanged.</summary>
        public bool? Active { get; set; } = null;
    }
}
