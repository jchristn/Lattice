namespace Lattice.Core.Security
{
    /// <summary>
    /// The outcome of first-run seeding. When a default tenant was created, carries the one-time
    /// credentials to surface to the operator.
    /// </summary>
    public class SeedResult
    {
        /// <summary>
        /// Whether a default tenant and administrator were created on this run.
        /// </summary>
        public bool CreatedDefaults { get; set; } = false;

        /// <summary>
        /// Identifier of the default tenant.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Email of the default administrator.
        /// </summary>
        public string AdminEmail { get; set; } = null;

        /// <summary>
        /// The raw access key of the default credential, shown only on the run that created it.
        /// </summary>
        public string DefaultAccessKey { get; set; } = null;

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public SeedResult()
        {
        }
    }
}
