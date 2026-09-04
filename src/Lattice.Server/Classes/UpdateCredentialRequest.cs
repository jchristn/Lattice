namespace Lattice.Server.Classes
{
    /// <summary>Request body for updating a credential. Only supplied fields are changed.</summary>
    public class UpdateCredentialRequest
    {
        /// <summary>New credential name, or null to leave unchanged.</summary>
        public string Name { get; set; } = null;

        /// <summary>New active flag, or null to leave unchanged.</summary>
        public bool? Active { get; set; } = null;
    }
}
