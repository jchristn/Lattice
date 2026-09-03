namespace Lattice.Core.Repositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// PostgreSQL implementation of audit methods.
    /// </summary>
    internal class AuditMethods : IAuditMethods
    {
        private readonly PostgresqlRepository _Repo;

        internal AuditMethods(PostgresqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<AuditEntry> Create(AuditEntry entry, CancellationToken token = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO audit (id, tenantid, eventtype, requestid, correlationid, traceid, principaltype, principalid, userid, credentialid, resourcetype, resourceid, requesttype, method, path, sourceip, authresult, authzresult, denialreason, bypassreason, requiredpermission, responsecode, createdutc)
                VALUES ('{Sanitizer.Sanitize(entry.Id)}',
                        {(entry.TenantId != null ? $"'{Sanitizer.Sanitize(entry.TenantId)}'" : "NULL")},
                        {(entry.EventType != null ? $"'{Sanitizer.Sanitize(entry.EventType)}'" : "NULL")},
                        {(entry.RequestId != null ? $"'{Sanitizer.Sanitize(entry.RequestId)}'" : "NULL")},
                        {(entry.CorrelationId != null ? $"'{Sanitizer.Sanitize(entry.CorrelationId)}'" : "NULL")},
                        {(entry.TraceId != null ? $"'{Sanitizer.Sanitize(entry.TraceId)}'" : "NULL")},
                        {(entry.PrincipalType != null ? ((int)entry.PrincipalType.Value).ToString() : "NULL")},
                        {(entry.PrincipalId != null ? $"'{Sanitizer.Sanitize(entry.PrincipalId)}'" : "NULL")},
                        {(entry.UserId != null ? $"'{Sanitizer.Sanitize(entry.UserId)}'" : "NULL")},
                        {(entry.CredentialId != null ? $"'{Sanitizer.Sanitize(entry.CredentialId)}'" : "NULL")},
                        {(entry.ResourceType != null ? ((int)entry.ResourceType.Value).ToString() : "NULL")},
                        {(entry.ResourceId != null ? $"'{Sanitizer.Sanitize(entry.ResourceId)}'" : "NULL")},
                        {(entry.RequestType != null ? $"'{Sanitizer.Sanitize(entry.RequestType)}'" : "NULL")},
                        {(entry.Method != null ? $"'{Sanitizer.Sanitize(entry.Method)}'" : "NULL")},
                        {(entry.Path != null ? $"'{Sanitizer.Sanitize(entry.Path)}'" : "NULL")},
                        {(entry.SourceIp != null ? $"'{Sanitizer.Sanitize(entry.SourceIp)}'" : "NULL")},
                        {(entry.AuthResult != null ? $"'{Sanitizer.Sanitize(entry.AuthResult)}'" : "NULL")},
                        {(entry.AuthzResult != null ? $"'{Sanitizer.Sanitize(entry.AuthzResult)}'" : "NULL")},
                        {(entry.DenialReason != null ? $"'{Sanitizer.Sanitize(entry.DenialReason)}'" : "NULL")},
                        {(entry.BypassReason != null ? $"'{Sanitizer.Sanitize(entry.BypassReason)}'" : "NULL")},
                        {(entry.RequiredPermission != null ? $"'{Sanitizer.Sanitize(entry.RequiredPermission)}'" : "NULL")},
                        {entry.ResponseCode},
                        '{Converters.ToTimestamp(entry.CreatedUtc)}')
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.AuditEntryFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<AuditEntry> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM audit WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.AuditEntryFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<AuditEntry>> Search(string tenantId, string eventType, DateTime? fromUtc, DateTime? toUtc, int skip, int maxResults, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (skip < 0) skip = 0;
            if (maxResults < 1) maxResults = 1;

            string whereClause = BuildWhereClause(tenantId, eventType, fromUtc, toUtc);

            string query = $"SELECT * FROM audit{whereClause} ORDER BY createdutc DESC LIMIT {maxResults} OFFSET {skip};";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<AuditEntry> entries = new List<AuditEntry>();
            foreach (DataRow row in result.Rows)
                entries.Add(Converters.AuditEntryFromDataRow(row));

            return entries;
        }

        public async Task<long> Count(string tenantId, string eventType, DateTime? fromUtc, DateTime? toUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string whereClause = BuildWhereClause(tenantId, eventType, fromUtc, toUtc);

            string query = $"SELECT COUNT(*) as cnt FROM audit{whereClause};";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]);

            return 0;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM audit WHERE id = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        public async Task<long> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string countQuery = $"SELECT COUNT(*) as cnt FROM audit WHERE createdutc < '{Converters.ToTimestamp(cutoffUtc)}';";
            DataTable countResult = await _Repo.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);

            long affected = 0;
            if (countResult.Rows.Count > 0)
                affected = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string deleteQuery = $"DELETE FROM audit WHERE createdutc < '{Converters.ToTimestamp(cutoffUtc)}';";
            await _Repo.ExecuteNonQueryAsync(deleteQuery, token).ConfigureAwait(false);

            return affected;
        }

        private static string BuildWhereClause(string tenantId, string eventType, DateTime? fromUtc, DateTime? toUtc)
        {
            List<string> conditions = new List<string>();

            if (tenantId != null)
                conditions.Add($"tenantid = '{Sanitizer.Sanitize(tenantId)}'");

            if (!string.IsNullOrWhiteSpace(eventType))
                conditions.Add($"eventtype = '{Sanitizer.Sanitize(eventType)}'");

            if (fromUtc != null)
                conditions.Add($"createdutc >= '{Converters.ToTimestamp(fromUtc.Value)}'");

            if (toUtc != null)
                conditions.Add($"createdutc <= '{Converters.ToTimestamp(toUtc.Value)}'");

            if (conditions.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder(" WHERE ");
            for (int i = 0; i < conditions.Count; i++)
            {
                if (i > 0) sb.Append(" AND ");
                sb.Append(conditions[i]);
            }

            return sb.ToString();
        }
    }
}
