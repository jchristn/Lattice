namespace Lattice.Core.Repositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.Sqlite;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;
    using Lattice.Core.Repositories.Sqlite.Queries;
    using Lattice.Core.Search;

    /// <summary>
    /// SQLite implementation of index methods.
    /// </summary>
    internal class IndexMethods : IIndexMethods
    {
        private readonly SqliteRepository _Repo;

        internal IndexMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<IndexTableMapping> CreateMapping(IndexTableMapping mapping, CancellationToken token = default)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO indextablemappings (id, key, tablename, createdutc)
                VALUES ('{Sanitizer.Sanitize(mapping.Id)}',
                        '{Sanitizer.Sanitize(mapping.Key)}',
                        '{Sanitizer.Sanitize(mapping.TableName)}',
                        '{Converters.ToTimestamp(mapping.CreatedUtc)}');
                SELECT * FROM indextablemappings WHERE id = '{Sanitizer.Sanitize(mapping.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.IndexTableMappingFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<IndexTableMapping> GetMappingByKey(string key, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM indextablemappings WHERE key = '{Sanitizer.Sanitize(key)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.IndexTableMappingFromDataRow(result.Rows[0]);

            return null;
        }

        public async IAsyncEnumerable<IndexTableMapping> GetAllMappings([EnumeratorCancellation] CancellationToken token = default)
        {
            string query = "SELECT * FROM indextablemappings ORDER BY key;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.IndexTableMappingFromDataRow(row);
            }
        }

        public async Task CreateIndexTable(string tableName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = SetupQueries.CreateIndexTable(sanitizedTableName);
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<bool> IndexTableExists(string tableName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $"SELECT COUNT(*) as cnt FROM sqlite_master WHERE type='table' AND name='{sanitizedTableName}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]) > 0;

            return false;
        }

        public async Task InsertValue(string tableName, DocumentValue value, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (value == null) throw new ArgumentNullException(nameof(value));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $@"
                INSERT INTO {sanitizedTableName} (id, documentid, position, value, createdutc)
                VALUES ('{Sanitizer.Sanitize(value.Id)}',
                        '{Sanitizer.Sanitize(value.DocumentId)}',
                        {(value.Position.HasValue ? value.Position.Value.ToString() : "NULL")},
                        {(value.Value != null ? $"'{Sanitizer.Sanitize(value.Value)}'" : "NULL")},
                        '{Converters.ToTimestamp(value.CreatedUtc)}');
            ";

            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task InsertValues(string tableName, List<DocumentValue> values, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (values == null || values.Count == 0) return;
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION;");

            foreach (DocumentValue value in values)
            {
                sb.AppendLine($@"
                    INSERT INTO {sanitizedTableName} (id, documentid, position, value, createdutc)
                    VALUES ('{Sanitizer.Sanitize(value.Id)}',
                            '{Sanitizer.Sanitize(value.DocumentId)}',
                            {(value.Position.HasValue ? value.Position.Value.ToString() : "NULL")},
                            {(value.Value != null ? $"'{Sanitizer.Sanitize(value.Value)}'" : "NULL")},
                            '{Converters.ToTimestamp(value.CreatedUtc)}');
                ");
            }

            sb.AppendLine("COMMIT;");
            await _Repo.ExecuteNonQueryAsync(sb.ToString(), token);
        }

        public async Task InsertValuesMultiTable(Dictionary<string, List<DocumentValue>> valuesByTable, CancellationToken token = default)
        {
            if (valuesByTable == null || valuesByTable.Count == 0) return;
            token.ThrowIfCancellationRequested();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION;");

            foreach (KeyValuePair<string, List<DocumentValue>> kvp in valuesByTable)
            {
                if (kvp.Value == null || kvp.Value.Count == 0) continue;

                string sanitizedTableName = Sanitizer.SanitizeTableName(kvp.Key);

                foreach (DocumentValue value in kvp.Value)
                {
                    sb.AppendLine($@"
                        INSERT INTO {sanitizedTableName} (id, documentid, position, value, createdutc)
                        VALUES ('{Sanitizer.Sanitize(value.Id)}',
                                '{Sanitizer.Sanitize(value.DocumentId)}',
                                {(value.Position.HasValue ? value.Position.Value.ToString() : "NULL")},
                                {(value.Value != null ? $"'{Sanitizer.Sanitize(value.Value)}'" : "NULL")},
                                '{Converters.ToTimestamp(value.CreatedUtc)}');
                    ");
                }
            }

            sb.AppendLine("COMMIT;");
            await _Repo.ExecuteNonQueryAsync(sb.ToString(), token);
        }

        public async Task DeleteByDocumentId(string tableName, string documentId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentNullException(nameof(documentId));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $"DELETE FROM {sanitizedTableName} WHERE documentid = '{Sanitizer.Sanitize(documentId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async IAsyncEnumerable<string> Search(string tableName, SearchFilter filter, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string condition = filter.ToSqlCondition("@value");

            string query = $"SELECT DISTINCT documentid FROM {sanitizedTableName} WHERE {condition};";

            DataTable result;

            // Use parameterized query for value-based conditions
            if (filter.Condition != SearchConditionEnum.IsNull && filter.Condition != SearchConditionEnum.IsNotNull)
            {
                SqliteParameter parameter = new SqliteParameter("@value", filter.Value ?? (object)DBNull.Value);
                result = await _Repo.ExecuteParameterizedQueryAsync(query, token, parameter);
            }
            else
            {
                result = await _Repo.ExecuteQueryAsync(query, false, token);
            }

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return row["documentid"]?.ToString();
            }
        }

        public async Task DropIndexTable(string tableName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = SetupQueries.DropIndexTable(sanitizedTableName);
            await _Repo.ExecuteNonQueryAsync(query, token);
        }
    }
}
