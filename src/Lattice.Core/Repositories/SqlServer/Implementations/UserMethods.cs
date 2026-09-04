namespace Lattice.Core.Repositories.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQL Server implementation of user methods.
    /// </summary>
    internal class UserMethods : IUserMethods
    {
        private readonly SqlServerRepository _Repo;

        internal UserMethods(SqlServerRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<User> Create(User user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO [users] ([id], [tenantid], [firstname], [lastname], [email], [passwordsha256], [isadmin], [istenantadmin], [active], [isprotected], [createdutc], [lastupdateutc])
                OUTPUT INSERTED.*
                VALUES ('{Sanitizer.Sanitize(user.Id)}',
                        '{Sanitizer.Sanitize(user.TenantId)}',
                        {(user.FirstName != null ? $"N'{Sanitizer.Sanitize(user.FirstName)}'" : "NULL")},
                        {(user.LastName != null ? $"N'{Sanitizer.Sanitize(user.LastName)}'" : "NULL")},
                        N'{Sanitizer.Sanitize(user.Email)}',
                        {(user.PasswordSha256 != null ? $"'{Sanitizer.Sanitize(user.PasswordSha256)}'" : "NULL")},
                        {(user.IsAdmin ? 1 : 0)},
                        {(user.IsTenantAdmin ? 1 : 0)},
                        {(user.Active ? 1 : 0)},
                        {(user.IsProtected ? 1 : 0)},
                        '{Converters.ToTimestamp(user.CreatedUtc)}',
                        '{Converters.ToTimestamp(user.LastUpdateUtc)}');
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.UserFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<User> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [users] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.UserFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<User> ReadByEmail(string tenantId, string email, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT TOP 1 * FROM [users] WHERE [tenantid] = '{Sanitizer.Sanitize(tenantId)}' AND [email] = N'{Sanitizer.Sanitize(email)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.UserFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<User>> ReadByTenant(string tenantId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [users] WHERE [tenantid] = '{Sanitizer.Sanitize(tenantId)}' ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<User> users = new List<User>();
            foreach (DataRow row in result.Rows)
                users.Add(Converters.UserFromDataRow(row));

            return users;
        }

        public async Task<List<User>> ReadByEmailAcrossTenants(string email, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [users] WHERE [email] = N'{Sanitizer.Sanitize(email)}' ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<User> users = new List<User>();
            foreach (DataRow row in result.Rows)
                users.Add(Converters.UserFromDataRow(row));

            return users;
        }

        public async Task<User> Update(User user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            token.ThrowIfCancellationRequested();

            user.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE [users] SET
                    [firstname] = {(user.FirstName != null ? $"N'{Sanitizer.Sanitize(user.FirstName)}'" : "NULL")},
                    [lastname] = {(user.LastName != null ? $"N'{Sanitizer.Sanitize(user.LastName)}'" : "NULL")},
                    [email] = N'{Sanitizer.Sanitize(user.Email)}',
                    [passwordsha256] = {(user.PasswordSha256 != null ? $"'{Sanitizer.Sanitize(user.PasswordSha256)}'" : "NULL")},
                    [isadmin] = {(user.IsAdmin ? 1 : 0)},
                    [istenantadmin] = {(user.IsTenantAdmin ? 1 : 0)},
                    [active] = {(user.Active ? 1 : 0)},
                    [isprotected] = {(user.IsProtected ? 1 : 0)},
                    [lastupdateutc] = '{Converters.ToTimestamp(user.LastUpdateUtc)}'
                OUTPUT INSERTED.*
                WHERE [id] = '{Sanitizer.Sanitize(user.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.UserFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [users] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        public async Task<bool> Exists(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM [users] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]) > 0;

            return false;
        }

        public async Task<long> Count(string tenantId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM [users] WHERE [tenantid] = '{Sanitizer.Sanitize(tenantId)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]);

            return 0;
        }
    }
}
