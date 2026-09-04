namespace Lattice.Core.Repositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// PostgreSQL implementation of credential methods.
    /// </summary>
    internal class CredentialMethods : ICredentialMethods
    {
        private readonly PostgresqlRepository _Repo;

        internal CredentialMethods(PostgresqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Credential> Create(Credential credential, CancellationToken token = default)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO credentials (id, tenantid, userid, name, accesskey, accesskeysha256, accesskeylast4, expiresutc, lastusedutc, active, isprotected, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(credential.Id)}',
                        '{Sanitizer.Sanitize(credential.TenantId)}',
                        '{Sanitizer.Sanitize(credential.UserId)}',
                        {(credential.Name != null ? $"'{Sanitizer.Sanitize(credential.Name)}'" : "NULL")},
                        {(credential.AccessKey != null ? $"'{Sanitizer.Sanitize(credential.AccessKey)}'" : "NULL")},
                        '{Sanitizer.Sanitize(credential.AccessKeySha256)}',
                        {(credential.AccessKeyLast4 != null ? $"'{Sanitizer.Sanitize(credential.AccessKeyLast4)}'" : "NULL")},
                        {(credential.ExpiresUtc != null ? $"'{Converters.ToTimestamp(credential.ExpiresUtc.Value)}'" : "NULL")},
                        {(credential.LastUsedUtc != null ? $"'{Converters.ToTimestamp(credential.LastUsedUtc.Value)}'" : "NULL")},
                        {credential.Active},
                        {credential.IsProtected},
                        '{Converters.ToTimestamp(credential.CreatedUtc)}',
                        '{Converters.ToTimestamp(credential.LastUpdateUtc)}')
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.CredentialFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Credential> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM credentials WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.CredentialFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Credential> ReadByAccessKeyHash(string accessKeySha256, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(accessKeySha256)) throw new ArgumentNullException(nameof(accessKeySha256));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM credentials WHERE accesskeysha256 = '{Sanitizer.Sanitize(accessKeySha256)}' LIMIT 1;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.CredentialFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<Credential>> ReadByTenant(string tenantId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM credentials WHERE tenantid = '{Sanitizer.Sanitize(tenantId)}' ORDER BY createdutc DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<Credential> credentials = new List<Credential>();
            foreach (DataRow row in result.Rows)
                credentials.Add(Converters.CredentialFromDataRow(row));

            return credentials;
        }

        public async Task<List<Credential>> ReadByUser(string userId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM credentials WHERE userid = '{Sanitizer.Sanitize(userId)}' ORDER BY createdutc DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<Credential> credentials = new List<Credential>();
            foreach (DataRow row in result.Rows)
                credentials.Add(Converters.CredentialFromDataRow(row));

            return credentials;
        }

        public async Task<Credential> Update(Credential credential, CancellationToken token = default)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            token.ThrowIfCancellationRequested();

            credential.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE credentials SET
                    name = {(credential.Name != null ? $"'{Sanitizer.Sanitize(credential.Name)}'" : "NULL")},
                    accesskeysha256 = '{Sanitizer.Sanitize(credential.AccessKeySha256)}',
                    accesskeylast4 = {(credential.AccessKeyLast4 != null ? $"'{Sanitizer.Sanitize(credential.AccessKeyLast4)}'" : "NULL")},
                    expiresutc = {(credential.ExpiresUtc != null ? $"'{Converters.ToTimestamp(credential.ExpiresUtc.Value)}'" : "NULL")},
                    lastusedutc = {(credential.LastUsedUtc != null ? $"'{Converters.ToTimestamp(credential.LastUsedUtc.Value)}'" : "NULL")},
                    active = {credential.Active},
                    isprotected = {credential.IsProtected},
                    lastupdateutc = '{Converters.ToTimestamp(credential.LastUpdateUtc)}'
                WHERE id = '{Sanitizer.Sanitize(credential.Id)}'
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.CredentialFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM credentials WHERE id = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        public async Task<bool> Exists(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM credentials WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]) > 0;

            return false;
        }
    }
}
