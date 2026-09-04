namespace Lattice.LoadGenerator
{
    using System;
    using System.Text;

    /// <summary>
    /// Running tally of what the seeder created, rendered to the console at the end of a run.
    /// </summary>
    public class SeedSummary
    {
        #region Public-Members

        /// <summary>Number of collections created.</summary>
        public int Collections { get; set; } = 0;

        /// <summary>Number of documents ingested.</summary>
        public int Documents { get; set; } = 0;

        /// <summary>Number of request-history entries written.</summary>
        public int RequestHistory { get; set; } = 0;

        /// <summary>Number of audit entries written.</summary>
        public int AuditEntries { get; set; } = 0;

        /// <summary>Number of users created.</summary>
        public int Users { get; set; } = 0;

        /// <summary>Number of credentials created.</summary>
        public int Credentials { get; set; } = 0;

        /// <summary>Number of custom roles created.</summary>
        public int Roles { get; set; } = 0;

        /// <summary>Number of role assignments created.</summary>
        public int Assignments { get; set; } = 0;

        /// <summary>Number of live requests fired against a running server.</summary>
        public int LiveRequests { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>Instantiate.</summary>
        public SeedSummary()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>Render the summary as a multi-line string.</summary>
        /// <returns>Formatted summary.</returns>
        public string Render()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Seed complete:");
            sb.AppendLine("  Collections     : " + Collections);
            sb.AppendLine("  Documents       : " + Documents);
            sb.AppendLine("  Request history : " + RequestHistory);
            sb.AppendLine("  Audit entries   : " + AuditEntries);
            sb.AppendLine("  Users           : " + Users);
            sb.AppendLine("  Credentials     : " + Credentials);
            sb.AppendLine("  Roles           : " + Roles);
            sb.AppendLine("  Assignments     : " + Assignments);
            if (LiveRequests > 0) sb.AppendLine("  Live requests   : " + LiveRequests);
            return sb.ToString().TrimEnd();
        }

        #endregion
    }
}
