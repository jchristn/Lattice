namespace Lattice.Core.Models
{
    /// <summary>
    /// The kind of authenticated principal a request resolves to.
    /// </summary>
    public enum PrincipalType
    {
        /// <summary>An interactive user, authenticated by email/password and carrying a session token.</summary>
        User,
        /// <summary>A machine credential, authenticated by its access key presented as a bearer token.</summary>
        Credential
    }
}
