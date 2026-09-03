namespace Lattice.Server.Classes
{
    /// <summary>
    /// Request body for creating a credential. The access key is generated server-side and returned once.
    /// </summary>
    public class CreateCredentialRequest
    {
        /// <summary>Human-readable credential name.</summary>
        public string Name { get; set; } = null;

        /// <summary>Owning user id. Defaults to the calling user when omitted.</summary>
        public string UserId { get; set; } = null;

        /// <summary>Target tenant id; honored only for system administrators acting cross-tenant.</summary>
        public string TenantId { get; set; } = null;
    }
}
