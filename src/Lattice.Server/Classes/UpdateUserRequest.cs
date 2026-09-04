namespace Lattice.Server.Classes
{
    /// <summary>Request body for updating a user. Only supplied fields are changed.</summary>
    public class UpdateUserRequest
    {
        /// <summary>New given name, or null to leave unchanged.</summary>
        public string FirstName { get; set; } = null;

        /// <summary>New family name, or null to leave unchanged.</summary>
        public string LastName { get; set; } = null;

        /// <summary>New password (stored as SHA-256), or null to leave unchanged.</summary>
        public string Password { get; set; } = null;

        /// <summary>New tenant-administrator flag, or null to leave unchanged.</summary>
        public bool? IsTenantAdmin { get; set; } = null;

        /// <summary>New system-administrator flag (honored only for system admins), or null to leave unchanged.</summary>
        public bool? IsAdmin { get; set; } = null;

        /// <summary>New active flag, or null to leave unchanged.</summary>
        public bool? Active { get; set; } = null;
    }
}
