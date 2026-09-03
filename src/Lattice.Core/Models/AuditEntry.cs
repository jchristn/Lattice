namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// An append-only security audit event: authentication outcomes, session lifecycle, credential use,
    /// RBAC mutations, authorization denials, and administrative bypasses. Denials are always persisted.
    /// </summary>
    public class AuditEntry
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the audit entry (aud_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the tenant the event pertains to, or null for system-level events.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// The kind of event (for example AuthSuccess, AuthFailure, AuthzDenied, SessionRevoked).
        /// </summary>
        public string EventType { get; set; } = null;

        /// <summary>
        /// Correlating request identifier.
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Correlation identifier spanning related requests, or null.
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Trace identifier for telemetry correlation, or null.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// The kind of principal, or null when unauthenticated.
        /// </summary>
        public PrincipalType? PrincipalType { get; set; } = null;

        /// <summary>
        /// Identifier of the principal (user or credential), or null.
        /// </summary>
        public string PrincipalId { get; set; } = null;

        /// <summary>
        /// Identifier of the user involved, or null.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Identifier of the credential involved, or null.
        /// </summary>
        public string CredentialId { get; set; } = null;

        /// <summary>
        /// The resource type the request targeted, or null.
        /// </summary>
        public ResourceType? ResourceType { get; set; } = null;

        /// <summary>
        /// Identifier of the resource the request targeted, or null.
        /// </summary>
        public string ResourceId { get; set; } = null;

        /// <summary>
        /// The classified request type.
        /// </summary>
        public string RequestType { get; set; } = null;

        /// <summary>
        /// HTTP method of the request.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Request path.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Source IP address.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// The authentication result (for example Success, NotFound, Inactive, Invalid), or null.
        /// </summary>
        public string AuthResult { get; set; } = null;

        /// <summary>
        /// The authorization result (for example Permitted, DeniedExplicit, DeniedImplicit), or null.
        /// </summary>
        public string AuthzResult { get; set; } = null;

        /// <summary>
        /// The reason authorization was denied, or null.
        /// </summary>
        public string DenialReason { get; set; } = null;

        /// <summary>
        /// The reason an administrative bypass was applied, or null.
        /// </summary>
        public string BypassReason { get; set; } = null;

        /// <summary>
        /// The permission the request required, or null.
        /// </summary>
        public string RequiredPermission { get; set; } = null;

        /// <summary>
        /// The HTTP response code returned.
        /// </summary>
        public int ResponseCode { get; set; } = 0;

        /// <summary>
        /// Timestamp when the event occurred (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public AuditEntry()
        {
        }

        #endregion
    }
}
