namespace Lattice.Core.Repositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQLite implementation of document tag methods.
    /// </summary>
    internal class TagMethods : ITagMethods
    {
        private readonly SqliteRepository _Repo;

        internal TagMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Tag> Create(Tag tag, CancellationToken token = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO tags (id, collectionid, documentid, key, value, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(tag.Id)}',
                        {(tag.CollectionId != null ? $"'{Sanitizer.Sanitize(tag.CollectionId)}'" : "NULL")},
                        {(tag.DocumentId != null ? $"'{Sanitizer.Sanitize(tag.DocumentId)}'" : "NULL")},
                        '{Sanitizer.Sanitize(tag.Key)}',
                        {(tag.Value != null ? $"'{Sanitizer.Sanitize(tag.Value)}'" : "NULL")},
                        '{Converters.ToTimestamp(tag.CreatedUtc)}',
                        '{Converters.ToTimestamp(tag.LastUpdateUtc)}');
                SELECT * FROM tags WHERE id = '{Sanitizer.Sanitize(tag.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.TagFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<Tag>> CreateMany(List<Tag> tags, CancellationToken token = default)
        {
            if (tags == null || tags.Count == 0) return new List<Tag>();
            token.ThrowIfCancellationRequested();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION;");

            foreach (Tag tag in tags)
            {
                sb.AppendLine($@"
                    INSERT INTO tags (id, collectionid, documentid, key, value, createdutc, lastupdateutc)
                    VALUES ('{Sanitizer.Sanitize(tag.Id)}',
                            {(tag.CollectionId != null ? $"'{Sanitizer.Sanitize(tag.CollectionId)}'" : "NULL")},
                            {(tag.DocumentId != null ? $"'{Sanitizer.Sanitize(tag.DocumentId)}'" : "NULL")},
                            '{Sanitizer.Sanitize(tag.Key)}',
                            {(tag.Value != null ? $"'{Sanitizer.Sanitize(tag.Value)}'" : "NULL")},
                            '{Converters.ToTimestamp(tag.CreatedUtc)}',
                            '{Converters.ToTimestamp(tag.LastUpdateUtc)}');
                ");
            }

            sb.AppendLine("COMMIT;");
            await _Repo.ExecuteNonQueryAsync(sb.ToString(), token);

            return tags;
        }

        public async IAsyncEnumerable<Tag> ReadByDocumentId(string documentId, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentNullException(nameof(documentId));

            string query = $"SELECT * FROM tags WHERE documentid = '{Sanitizer.Sanitize(documentId)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.TagFromDataRow(row);
            }
        }

        public async Task<Dictionary<string, Dictionary<string, string>>> ReadByDocumentIds(List<string> documentIds, CancellationToken token = default)
        {
            Dictionary<string, Dictionary<string, string>> result = new Dictionary<string, Dictionary<string, string>>();
            if (documentIds == null || documentIds.Count == 0) return result;
            token.ThrowIfCancellationRequested();

            // Initialize empty dictionaries for all requested IDs
            foreach (string docId in documentIds)
            {
                result[docId] = new Dictionary<string, string>();
            }

            // Build IN clause with sanitized IDs
            string inClause = string.Join(",", documentIds.Select(id => $"'{Sanitizer.Sanitize(id)}'"));
            string query = $"SELECT documentid, key, value FROM tags WHERE documentid IN ({inClause});";
            DataTable dataTable = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in dataTable.Rows)
            {
                string docId = row["documentid"]?.ToString();
                string key = row["key"]?.ToString();
                string value = row["value"]?.ToString();
                if (!string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(key) && result.ContainsKey(docId))
                {
                    result[docId][key] = value;
                }
            }

            return result;
        }

        public async Task DeleteByDocumentId(string documentId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentNullException(nameof(documentId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM tags WHERE documentid = '{Sanitizer.Sanitize(documentId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async IAsyncEnumerable<string> FindDocumentIdsByTag(string key, string value, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            string whereClause = value != null
                ? $"key = '{Sanitizer.Sanitize(key)}' AND value = '{Sanitizer.Sanitize(value)}'"
                : $"key = '{Sanitizer.Sanitize(key)}'";

            string query = $"SELECT DISTINCT documentid FROM tags WHERE {whereClause};";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return row["documentid"]?.ToString();
            }
        }

        public async Task<HashSet<string>> FindDocumentIdsByTags(Dictionary<string, string> tags, CancellationToken token = default)
        {
            HashSet<string> result = new HashSet<string>();
            if (tags == null || tags.Count == 0) return result;
            token.ThrowIfCancellationRequested();

            List<KeyValuePair<string, string>> tagList = tags.ToList();

            // Use JOIN query to find documents that have ALL specified tags
            // For N tags, we self-join the tags table N times
            if (tagList.Count == 1)
            {
                // Single tag: simple query
                KeyValuePair<string, string> tag = tagList[0];
                string whereClause = tag.Value != null
                    ? $"key = '{Sanitizer.Sanitize(tag.Key)}' AND value = '{Sanitizer.Sanitize(tag.Value)}'"
                    : $"key = '{Sanitizer.Sanitize(tag.Key)}'";

                string query = $"SELECT DISTINCT documentid FROM tags WHERE {whereClause};";
                DataTable dataTable = await _Repo.ExecuteQueryAsync(query, false, token);
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(row["documentid"]?.ToString());
                }
            }
            else
            {
                // Multiple tags: use self-joins
                // SELECT t0.documentid FROM tags t0
                // INNER JOIN tags t1 ON t0.documentid = t1.documentid
                // INNER JOIN tags t2 ON t0.documentid = t2.documentid
                // WHERE t0.key = 'key0' AND t0.value = 'value0'
                //   AND t1.key = 'key1' AND t1.value = 'value1'
                //   AND t2.key = 'key2' AND t2.value = 'value2'
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT DISTINCT t0.documentid FROM tags t0 ");

                // Add JOINs
                for (int i = 1; i < tagList.Count; i++)
                {
                    sb.Append($"INNER JOIN tags t{i} ON t0.documentid = t{i}.documentid ");
                }

                // Add WHERE clause
                sb.Append("WHERE ");
                for (int i = 0; i < tagList.Count; i++)
                {
                    if (i > 0) sb.Append(" AND ");
                    KeyValuePair<string, string> tag = tagList[i];
                    if (tag.Value != null)
                    {
                        sb.Append($"(t{i}.key = '{Sanitizer.Sanitize(tag.Key)}' AND t{i}.value = '{Sanitizer.Sanitize(tag.Value)}')");
                    }
                    else
                    {
                        sb.Append($"t{i}.key = '{Sanitizer.Sanitize(tag.Key)}'");
                    }
                }
                sb.Append(";");

                DataTable dataTable = await _Repo.ExecuteQueryAsync(sb.ToString(), false, token);
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(row["documentid"]?.ToString());
                }
            }

            return result;
        }

        public async IAsyncEnumerable<Tag> ReadByCollectionId(string collectionId, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));

            string query = $"SELECT * FROM tags WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.TagFromDataRow(row);
            }
        }

        public async Task DeleteByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM tags WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }
    }
}
