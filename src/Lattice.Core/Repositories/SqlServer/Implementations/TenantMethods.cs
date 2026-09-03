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
    /// SQL Server implementation of tenant methods.
    /// </summary>
    internal class TenantMethods : ITenantMethods
    {
        private readonly SqlServerRepository _Repo;

        internal TenantMethods(SqlServerRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Tenant> Create(Tenant tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO [tenants] ([id], [name], [region], [active], [isprotected], [createdutc], [lastupdateutc])
                OUTPUT INSERTED.*
                VALUES ('{Sanitizer.Sanitize(tenant.Id)}',
                        N'{Sanitizer.Sanitize(tenant.Name)}',
                        {(tenant.Region != null ? $"N'{Sanitizer.Sanitize(tenant.Region)}'" : "NULL")},
                        {(tenant.Active ? 1 : 0)},
                        {(tenant.IsProtected ? 1 : 0)},
                        '{Converters.ToTimestamp(tenant.CreatedUtc)}',
                        '{Converters.ToTimestamp(tenant.LastUpdateUtc)}');
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.TenantFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Tenant> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM [tenants] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.TenantFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Tenant> ReadByName(string name, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT TOP 1 * FROM [tenants] WHERE [name] = N'{Sanitizer.Sanitize(name)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Converters.TenantFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<Tenant>> ReadAll(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string query = "SELECT * FROM [tenants] ORDER BY [createdutc] DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            List<Tenant> tenants = new List<Tenant>();
            foreach (DataRow row in result.Rows)
                tenants.Add(Converters.TenantFromDataRow(row));

            return tenants;
        }

        public async Task<Tenant> Update(Tenant tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            token.ThrowIfCancellationRequested();

            tenant.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE [tenants] SET
                    [name] = N'{Sanitizer.Sanitize(tenant.Name)}',
                    [region] = {(tenant.Region != null ? $"N'{Sanitizer.Sanitize(tenant.Region)}'" : "NULL")},
                    [active] = {(tenant.Active ? 1 : 0)},
                    [isprotected] = {(tenant.IsProtected ? 1 : 0)},
                    [lastupdateutc] = '{Converters.ToTimestamp(tenant.LastUpdateUtc)}'
                OUTPUT INSERTED.*
                WHERE [id] = '{Sanitizer.Sanitize(tenant.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result.Rows.Count > 0)
                return Converters.TenantFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM [tenants] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token).ConfigureAwait(false);
        }

        public async Task<bool> Exists(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM [tenants] WHERE [id] = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]) > 0;

            return false;
        }

        public async Task<long> Count(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string query = "SELECT COUNT(*) as cnt FROM [tenants];";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]);

            return 0;
        }
    }
}
