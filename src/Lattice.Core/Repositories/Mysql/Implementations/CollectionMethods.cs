namespace Lattice.Core.Repositories.Mysql.Implementations
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
    /// MySQL implementation of collection methods.
    /// </summary>
    internal class CollectionMethods : ICollectionMethods
    {
        private readonly MysqlRepository _Repo;

        internal CollectionMethods(MysqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Collection> Create(Collection collection, CancellationToken token = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO `collections` (`id`, `name`, `description`, `documentsdirectory`, `schemaenforcementmode`, `indexingmode`, `createdutc`, `lastupdateutc`)
                VALUES ('{Sanitizer.Sanitize(collection.Id)}',
                        '{Sanitizer.Sanitize(collection.Name)}',
                        {(collection.Description != null ? $"'{Sanitizer.Sanitize(collection.Description)}'" : "NULL")},
                        {(collection.DocumentsDirectory != null ? $"'{Sanitizer.Sanitize(collection.DocumentsDirectory)}'" : "NULL")},
                        {(int)collection.SchemaEnforcementMode},
                        {(int)collection.IndexingMode},
                        '{Converters.ToTimestamp(collection.CreatedUtc)}',
                        '{Converters.ToTimestamp(collection.LastUpdateUtc)}');
            ";

            await _Repo.ExecuteNonQueryAsync(query, token);

            string selectQuery = $"SELECT * FROM `collections` WHERE `id` = '{Sanitizer.Sanitize(collection.Id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(selectQuery, false, token);
            if (result.Rows.Count > 0)
                return Converters.CollectionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Collection> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM `collections` WHERE `id` = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.CollectionFromDataRow(result.Rows[0]);

            return null;
        }

        public async IAsyncEnumerable<Collection> ReadAll([EnumeratorCancellation] CancellationToken token = default)
        {
            string query = "SELECT * FROM `collections` ORDER BY `createdutc` DESC;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.CollectionFromDataRow(row);
            }
        }

        public async Task<Collection> Update(Collection collection, CancellationToken token = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            token.ThrowIfCancellationRequested();

            collection.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE `collections` SET
                    `name` = '{Sanitizer.Sanitize(collection.Name)}',
                    `description` = {(collection.Description != null ? $"'{Sanitizer.Sanitize(collection.Description)}'" : "NULL")},
                    `documentsdirectory` = {(collection.DocumentsDirectory != null ? $"'{Sanitizer.Sanitize(collection.DocumentsDirectory)}'" : "NULL")},
                    `schemaenforcementmode` = {(int)collection.SchemaEnforcementMode},
                    `indexingmode` = {(int)collection.IndexingMode},
                    `lastupdateutc` = '{Converters.ToTimestamp(collection.LastUpdateUtc)}'
                WHERE `id` = '{Sanitizer.Sanitize(collection.Id)}';
            ";

            await _Repo.ExecuteNonQueryAsync(query, token);

            string selectQuery = $"SELECT * FROM `collections` WHERE `id` = '{Sanitizer.Sanitize(collection.Id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(selectQuery, false, token);
            if (result.Rows.Count > 0)
                return Converters.CollectionFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM `collections` WHERE `id` = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<bool> Exists(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM `collections` WHERE `id` = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]) > 0;

            return false;
        }

        public async Task<long> Count(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            string query = "SELECT COUNT(*) as cnt FROM `collections`;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]);

            return 0;
        }
    }
}
