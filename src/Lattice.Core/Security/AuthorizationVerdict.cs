namespace Lattice.Core.Security
{
    /// <summary>
    /// The outcome of an authorization decision.
    /// </summary>
    public enum AuthorizationVerdict
    {
        /// <summary>A Permit grant matched and no Deny grant did.</summary>
        Permitted,
        /// <summary>A Deny grant matched.</summary>
        DeniedExplicit,
        /// <summary>No grant matched.</summary>
        DeniedImplicit
    }
}
