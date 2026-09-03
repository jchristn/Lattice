namespace Lattice.Core.Repositories.Postgresql.Implementations
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// PostgreSQL implementation of authentication session methods.
    /// </summary>
    internal class AuthSessionMethods : IAuthSessionMethods
    {
        private readonly PostgresqlRepository _Repo;

        internal AuthSessionMethods(PostgresqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<AuthSession> Create(AuthSession session, CancellationToken token = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO authsessions (id, tenantid, principaltype, userid, tokenid, sourceip, useragent, expiresutc, lastusedutc, revokedutc, revocationreason, active, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(session.Id)}',
                        '{Sanitizer.Sanitize(session.TenantId)}',
                        {(int)session.PrincipalType},
                        {(session.UserId != null ? $"'{Sanitizer.Sanitize(session.UserId)}'" : "NULL")},
                        '{Sanitizer.Sanitize(session.TokenId)}',
                        {(session.SourceIp != null ? $"'{Sanitizer.Sanitize(session.SourceIp)}'" : "NULL")},
                        {(session.UserAgent != null ? $"'{Sanitizer.Sanitize(session.UserAgent)}'" : "NULL")},
                        '{Converters.ToTimestamp(session.ExpiresUtc)}',
                        {(session.LastUsedUtc != null ? $"'{Converters.ToTimestamp(session.LastUsedUtc.Value)}'" : "NULL")},
                        {(session.RevokedUtc != null ? $"'{Converters.ToTimestamp(session.RevokedUtc.Value)}'" : "NULL")},
                        {(session.RevocationReason != null ? $"'{Sanitizer.Sanitize(session.RevocationReason)}'" : "NULL")},
                        {session.Active},
                        '{Converters.ToTimestamp(session.CreatedUtc)}',
                        '{Converters.ToTimestamp(session.LastUpdateUtc)}')
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.AuthSessionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<AuthSession> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM authsessions WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.AuthSessionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<AuthSession> ReadByTokenId(string tokenId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentNullException(nameof(tokenId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM authsessions WHERE tokenid = '{Sanitizer.Sanitize(tokenId)}' LIMIT 1;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.AuthSessionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<AuthSession> Update(AuthSession session, CancellationToken token = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            token.ThrowIfCancellationRequested();

            session.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE authsessions SET
                    principaltype = {(int)session.PrincipalType},
                    userid = {(session.UserId != null ? $"'{Sanitizer.Sanitize(session.UserId)}'" : "NULL")},
                    tokenid = '{Sanitizer.Sanitize(session.TokenId)}',
                    sourceip = {(session.SourceIp != null ? $"'{Sanitizer.Sanitize(session.SourceIp)}'" : "NULL")},
                    useragent = {(session.UserAgent != null ? $"'{Sanitizer.Sanitize(session.UserAgent)}'" : "NULL")},
                    expiresutc = '{Converters.ToTimestamp(session.ExpiresUtc)}',
                    lastusedutc = {(session.LastUsedUtc != null ? $"'{Converters.ToTimestamp(session.LastUsedUtc.Value)}'" : "NULL")},
                    revokedutc = {(session.RevokedUtc != null ? $"'{Converters.ToTimestamp(session.RevokedUtc.Value)}'" : "NULL")},
                    revocationreason = {(session.RevocationReason != null ? $"'{Sanitizer.Sanitize(session.RevocationReason)}'" : "NULL")},
                    active = {session.Active},
                    lastupdateutc = '{Converters.ToTimestamp(session.LastUpdateUtc)}'
                WHERE id = '{Sanitizer.Sanitize(session.Id)}'
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.AuthSessionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM authsessions WHERE id = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        public async Task<long> DeleteExpired(DateTime cutoffUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string countQuery = $"SELECT COUNT(*) as cnt FROM authsessions WHERE expiresutc < '{Converters.ToTimestamp(cutoffUtc)}';";
            DataTable countResult = await _Repo.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);

            long affected = 0;
            if (countResult.Rows.Count > 0)
                affected = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string deleteQuery = $"DELETE FROM authsessions WHERE expiresutc < '{Converters.ToTimestamp(cutoffUtc)}';";
            await _Repo.ExecuteNonQueryAsync(deleteQuery, token).ConfigureAwait(false);

            return affected;
        }
    }
}
