namespace Lattice.Core.Repositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQLite implementation of schema methods.
    /// </summary>
    internal class SchemaMethods : ISchemaMethods
    {
        private readonly SqliteRepository _Repo;

        internal SchemaMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Schema> Create(Schema schema, CancellationToken token = default)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO schemas (id, name, hash, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(schema.Id)}',
                        {(schema.Name != null ? $"'{Sanitizer.Sanitize(schema.Name)}'" : "NULL")},
                        '{Sanitizer.Sanitize(schema.Hash)}',
                        '{Converters.ToTimestamp(schema.CreatedUtc)}',
                        '{Converters.ToTimestamp(schema.LastUpdateUtc)}');
                SELECT * FROM schemas WHERE id = '{Sanitizer.Sanitize(schema.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.SchemaFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Schema> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM schemas WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.SchemaFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Schema> ReadByHash(string hash, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentNullException(nameof(hash));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM schemas WHERE hash = '{Sanitizer.Sanitize(hash)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.SchemaFromDataRow(result.Rows[0]);

            return null;
        }

        public async IAsyncEnumerable<Schema> ReadAll([EnumeratorCancellation] CancellationToken token = default)
        {
            string query = "SELECT * FROM schemas ORDER BY createdutc DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.SchemaFromDataRow(row);
            }
        }

        public async Task<Schema> Update(Schema schema, CancellationToken token = default)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            token.ThrowIfCancellationRequested();

            schema.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE schemas SET
                    name = {(schema.Name != null ? $"'{Sanitizer.Sanitize(schema.Name)}'" : "NULL")},
                    lastupdateutc = '{Converters.ToTimestamp(schema.LastUpdateUtc)}'
                WHERE id = '{Sanitizer.Sanitize(schema.Id)}';
                SELECT * FROM schemas WHERE id = '{Sanitizer.Sanitize(schema.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.SchemaFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM schemas WHERE id = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<bool> ExistsByHash(string hash, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentNullException(nameof(hash));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM schemas WHERE hash = '{Sanitizer.Sanitize(hash)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]) > 0;

            return false;
        }
    }
}
