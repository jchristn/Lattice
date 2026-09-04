namespace Lattice.LoadGenerator
{
    /// <summary>
    /// A category of synthetic activity the load generator can produce. Selected via the
    /// <c>--operations</c> argument (all categories are enabled by default).
    /// </summary>
    public enum OperationKind
    {
        /// <summary>Create collections owned by the target tenant.</summary>
        Collections,

        /// <summary>Ingest documents (and, transitively, schemas and index entries) into the collections.</summary>
        Documents,

        /// <summary>Write backdated request-history entries spread across the time window.</summary>
        Requests,

        /// <summary>Write backdated security audit entries spread across the time window.</summary>
        Audit,

        /// <summary>Create synthetic users in the target tenant.</summary>
        Users,

        /// <summary>Create synthetic access-key credentials for the synthetic users.</summary>
        Credentials,

        /// <summary>Create custom roles and assign roles to the synthetic users.</summary>
        Roles
    }
}
