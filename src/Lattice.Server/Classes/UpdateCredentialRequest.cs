namespace Lattice.Server.Classes
{
    /// <summary>Request body for updating a credential. Only supplied fields are changed.</summary>
    public class UpdateCredentialRequest
    {
        /// <summary>New credential name, or null to leave unchanged.</summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// New raw access key, or null to leave unchanged. When supplied, the access key becomes this value and its
        /// SHA-256 hash and last-four are recomputed so the new key can be used as a bearer token immediately.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>New active flag, or null to leave unchanged.</summary>
        public bool? Active { get; set; } = null;
    }
}
