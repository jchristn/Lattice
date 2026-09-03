namespace Lattice.Core.Models
{
    /// <summary>
    /// The type of resource an authorization decision applies to.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>All resource types (wildcard).</summary>
        All,
        /// <summary>Tenant records.</summary>
        Tenant,
        /// <summary>User records.</summary>
        User,
        /// <summary>Credential (access key) records.</summary>
        Credential,
        /// <summary>Authentication session records.</summary>
        Session,
        /// <summary>Role records.</summary>
        Role,
        /// <summary>Permission records.</summary>
        Permission,
        /// <summary>Role/scope assignment records.</summary>
        Assignment,
        /// <summary>Audit records.</summary>
        Audit,
        /// <summary>Document collections.</summary>
        Collection,
        /// <summary>Documents.</summary>
        Document,
        /// <summary>Schemas.</summary>
        Schema,
        /// <summary>Index tables and index entries.</summary>
        Index,
        /// <summary>Request history records.</summary>
        RequestHistory
    }
}
