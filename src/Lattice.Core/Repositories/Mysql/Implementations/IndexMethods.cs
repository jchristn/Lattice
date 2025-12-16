namespace Lattice.Core.Repositories.Mysql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MySqlConnector;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;
    using Lattice.Core.Repositories.Mysql.Queries;
    using Lattice.Core.Search;

    /// <summary>
    /// MySQL implementation of index methods.
    /// </summary>
    internal class IndexMethods : IIndexMethods
    {
        private readonly MysqlRepository _Repo;

        internal IndexMethods(MysqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<IndexTableMapping> CreateMapping(IndexTableMapping mapping, CancellationToken token = default)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO `indextablemappings` (`id`, `key`, `tablename`, `createdutc`)
                VALUES ('{Sanitizer.Sanitize(mapping.Id)}',
                        '{Sanitizer.Sanitize(mapping.Key)}',
                        '{Sanitizer.Sanitize(mapping.TableName)}',
                        '{Converters.ToTimestamp(mapping.CreatedUtc)}');
            ";

            await _Repo.ExecuteNonQueryAsync(query, token);

            string selectQuery = $"SELECT * FROM `indextablemappings` WHERE `id` = '{Sanitizer.Sanitize(mapping.Id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(selectQuery, false, token);
            if (result.Rows.Count > 0)
                return Converters.IndexTableMappingFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<IndexTableMapping> GetMappingByKey(string key, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM `indextablemappings` WHERE `key` = '{Sanitizer.Sanitize(key)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.IndexTableMappingFromDataRow(result.Rows[0]);

            return null;
        }

        public async IAsyncEnumerable<IndexTableMapping> GetAllMappings([EnumeratorCancellation] CancellationToken token = default)
        {
            string query = "SELECT * FROM `indextablemappings` ORDER BY `key`;";
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
            string[] statements = SetupQueries.CreateIndexTable(sanitizedTableName).Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string statement in statements)
            {
                string trimmed = statement.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    try
                    {
                        await _Repo.ExecuteNonQueryAsync(trimmed + ";", token);
                    }
                    catch (MySqlException ex) when (ex.Message.Contains("Duplicate") || ex.Message.Contains("already exists"))
                    {
                        // Index already exists - ignore
                    }
                }
            }
        }

        public async Task<bool> IndexTableExists(string tableName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $"SELECT COUNT(*) as cnt FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{sanitizedTableName}';";
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
                INSERT INTO `{sanitizedTableName}` (`id`, `documentid`, `position`, `value`, `createdutc`)
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
            List<string> statements = new List<string>();

            foreach (DocumentValue value in values)
            {
                statements.Add($@"
                    INSERT INTO `{sanitizedTableName}` (`id`, `documentid`, `position`, `value`, `createdutc`)
                    VALUES ('{Sanitizer.Sanitize(value.Id)}',
                            '{Sanitizer.Sanitize(value.DocumentId)}',
                            {(value.Position.HasValue ? value.Position.Value.ToString() : "NULL")},
                            {(value.Value != null ? $"'{Sanitizer.Sanitize(value.Value)}'" : "NULL")},
                            '{Converters.ToTimestamp(value.CreatedUtc)}');
                ");
            }

            await _Repo.ExecuteTransactionAsync(statements.ToArray(), token);
        }

        public async Task InsertValuesMultiTable(Dictionary<string, List<DocumentValue>> valuesByTable, CancellationToken token = default)
        {
            if (valuesByTable == null || valuesByTable.Count == 0) return;
            token.ThrowIfCancellationRequested();

            List<string> statements = new List<string>();

            foreach (KeyValuePair<string, List<DocumentValue>> kvp in valuesByTable)
            {
                if (kvp.Value == null || kvp.Value.Count == 0) continue;

                string sanitizedTableName = Sanitizer.SanitizeTableName(kvp.Key);

                foreach (DocumentValue value in kvp.Value)
                {
                    statements.Add($@"
                        INSERT INTO `{sanitizedTableName}` (`id`, `documentid`, `position`, `value`, `createdutc`)
                        VALUES ('{Sanitizer.Sanitize(value.Id)}',
                                '{Sanitizer.Sanitize(value.DocumentId)}',
                                {(value.Position.HasValue ? value.Position.Value.ToString() : "NULL")},
                                {(value.Value != null ? $"'{Sanitizer.Sanitize(value.Value)}'" : "NULL")},
                                '{Converters.ToTimestamp(value.CreatedUtc)}');
                    ");
                }
            }

            if (statements.Count > 0)
            {
                await _Repo.ExecuteTransactionAsync(statements.ToArray(), token);
            }
        }

        public async Task DeleteByDocumentId(string tableName, string documentId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentNullException(nameof(documentId));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $"DELETE FROM `{sanitizedTableName}` WHERE `documentid` = '{Sanitizer.Sanitize(documentId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async IAsyncEnumerable<string> Search(string tableName, SearchFilter filter, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string condition = filter.ToSqlCondition("@value");

            string query = $"SELECT DISTINCT `documentid` FROM `{sanitizedTableName}` WHERE {condition};";

            DataTable result;

            if (filter.Condition != SearchConditionEnum.IsNull && filter.Condition != SearchConditionEnum.IsNotNull)
            {
                MySqlParameter parameter = new MySqlParameter("@value", filter.Value ?? (object)DBNull.Value);
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

        public async Task DeleteMapping(string tableName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM `indextablemappings` WHERE `tablename` = '{Sanitizer.Sanitize(tableName)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<IndexTableMapping> GetMappingByTableName(string tableName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM `indextablemappings` WHERE `tablename` = '{Sanitizer.Sanitize(tableName)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.IndexTableMappingFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<string>> GetIndexTablesForCollection(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string docQuery = $"SELECT `id` FROM `documents` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}';";
            DataTable docResult = await _Repo.ExecuteQueryAsync(docQuery, false, token);

            if (docResult.Rows.Count == 0)
                return new List<string>();

            List<string> tableNames = new List<string>();
            string mappingQuery = "SELECT `tablename` FROM `indextablemappings`;";
            DataTable mappingResult = await _Repo.ExecuteQueryAsync(mappingQuery, false, token);

            foreach (DataRow row in mappingResult.Rows)
            {
                string tableName = row["tablename"]?.ToString();
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);

                    string checkQuery = $@"
                        SELECT COUNT(*) as cnt FROM `{sanitizedTableName}`
                        WHERE `documentid` IN (SELECT `id` FROM `documents` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}')
                        LIMIT 1;";

                    try
                    {
                        DataTable checkResult = await _Repo.ExecuteQueryAsync(checkQuery, false, token);
                        if (checkResult.Rows.Count > 0 && Convert.ToInt64(checkResult.Rows[0]["cnt"]) > 0)
                        {
                            tableNames.Add(tableName);
                        }
                    }
                    catch
                    {
                        // Table might not exist, skip
                    }
                }
            }

            return tableNames;
        }

        public async Task DeleteValuesByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            List<string> tableNames = await GetIndexTablesForCollection(collectionId, token);

            if (tableNames.Count == 0) return;

            List<string> statements = new List<string>();

            foreach (string tableName in tableNames)
            {
                string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
                statements.Add($@"
                    DELETE FROM `{sanitizedTableName}`
                    WHERE `documentid` IN (SELECT `id` FROM `documents` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}');
                ");
            }

            await _Repo.ExecuteTransactionAsync(statements.ToArray(), token);
        }

        public async Task DeleteValuesFromTable(string tableName, string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $@"
                DELETE FROM `{sanitizedTableName}`
                WHERE `documentid` IN (SELECT `id` FROM `documents` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}');
            ";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<List<IndexTableEntry>> GetTableEntries(string tableName, int skip = 0, int limit = 100, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be non-negative");
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be positive");
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $"SELECT * FROM `{sanitizedTableName}` ORDER BY `documentid`, `position` LIMIT {limit} OFFSET {skip};";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            List<IndexTableEntry> entries = new List<IndexTableEntry>();
            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                entries.Add(Converters.IndexTableEntryFromDataRow(row));
            }

            return entries;
        }

        public async Task<long> GetTableEntryCount(string tableName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            token.ThrowIfCancellationRequested();

            string sanitizedTableName = Sanitizer.SanitizeTableName(tableName);
            string query = $"SELECT COUNT(*) as cnt FROM `{sanitizedTableName}`;";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]);

            return 0;
        }
    }
}
