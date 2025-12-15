namespace Lattice.Core.Repositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;
    using Lattice.Core.Search;

    internal class DocumentMethods : IDocumentMethods
    {
        private readonly PostgresqlRepository _Repo;

        internal DocumentMethods(PostgresqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Document> Create(Document document, CancellationToken token = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO documents (id, collectionid, schemaid, name, contentlength, sha256hash, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(document.Id)}',
                        '{Sanitizer.Sanitize(document.CollectionId)}',
                        '{Sanitizer.Sanitize(document.SchemaId)}',
                        {(document.Name != null ? $"'{Sanitizer.Sanitize(document.Name)}'" : "NULL")},
                        {document.ContentLength},
                        {(document.Sha256Hash != null ? $"'{Sanitizer.Sanitize(document.Sha256Hash)}'" : "NULL")},
                        '{Converters.ToTimestamp(document.CreatedUtc)}',
                        '{Converters.ToTimestamp(document.LastUpdateUtc)}')
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.DocumentFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Document> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM documents WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.DocumentFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<Dictionary<string, Document>> ReadByIds(List<string> ids, CancellationToken token = default)
        {
            Dictionary<string, Document> documents = new Dictionary<string, Document>();
            if (ids == null || ids.Count == 0) return documents;
            token.ThrowIfCancellationRequested();

            string inClause = string.Join(",", ids.Select(id => $"'{Sanitizer.Sanitize(id)}'"));
            string query = $"SELECT * FROM documents WHERE id IN ({inClause});";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                Document doc = Converters.DocumentFromDataRow(row);
                documents[doc.Id] = doc;
            }

            return documents;
        }

        public async Task<Document> ReadByIdWithLabelsAndTags(string id, bool includeLabels = true, bool includeTags = true, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string sanitizedId = Sanitizer.Sanitize(id);

            string query = $@"
                SELECT
                    d.id, d.collectionid, d.schemaid, d.name, d.contentlength, d.sha256hash, d.createdutc, d.lastupdateutc,
                    l.labelvalue,
                    t.key as tagkey, t.value as tagvalue
                FROM documents d
                LEFT JOIN labels l ON d.id = l.documentid
                LEFT JOIN tags t ON d.id = t.documentid
                WHERE d.id = '{sanitizedId}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count == 0)
                return null;

            Document doc = Converters.DocumentFromDataRow(result.Rows[0]);

            HashSet<string> labels = new HashSet<string>();
            Dictionary<string, string> tags = new Dictionary<string, string>();

            foreach (DataRow row in result.Rows)
            {
                if (includeLabels && row["labelvalue"] != DBNull.Value)
                {
                    string labelValue = row["labelvalue"]?.ToString();
                    if (!string.IsNullOrEmpty(labelValue))
                        labels.Add(labelValue);
                }

                if (includeTags && row["tagkey"] != DBNull.Value)
                {
                    string tagKey = row["tagkey"]?.ToString();
                    string tagValue = row["tagvalue"]?.ToString();
                    if (!string.IsNullOrEmpty(tagKey) && !tags.ContainsKey(tagKey))
                        tags[tagKey] = tagValue;
                }
            }

            foreach (string label in labels)
                doc.Labels.Add(label);
            foreach (var tag in tags)
                doc.Tags[tag.Key] = tag.Value;

            return doc;
        }

        public async Task<Dictionary<string, Document>> ReadByIdsWithLabelsAndTags(List<string> ids, bool includeLabels = true, bool includeTags = true, CancellationToken token = default)
        {
            Dictionary<string, Document> documents = new Dictionary<string, Document>();
            if (ids == null || ids.Count == 0) return documents;
            token.ThrowIfCancellationRequested();

            string inClause = string.Join(",", ids.Select(id => $"'{Sanitizer.Sanitize(id)}'"));

            string query = $@"
                SELECT
                    d.id, d.collectionid, d.schemaid, d.name, d.contentlength, d.sha256hash, d.createdutc, d.lastupdateutc,
                    l.labelvalue,
                    t.key as tagkey, t.value as tagvalue
                FROM documents d
                LEFT JOIN labels l ON d.id = l.documentid
                LEFT JOIN tags t ON d.id = t.documentid
                WHERE d.id IN ({inClause})
                ORDER BY d.id;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                string docId = row["id"]?.ToString();
                if (!string.IsNullOrEmpty(docId) && !documents.ContainsKey(docId))
                {
                    documents[docId] = Converters.DocumentFromDataRow(row);
                }
            }

            Dictionary<string, HashSet<string>> labelsByDoc = new Dictionary<string, HashSet<string>>();
            Dictionary<string, Dictionary<string, string>> tagsByDoc = new Dictionary<string, Dictionary<string, string>>();

            foreach (string docId in documents.Keys)
            {
                labelsByDoc[docId] = new HashSet<string>();
                tagsByDoc[docId] = new Dictionary<string, string>();
            }

            foreach (DataRow row in result.Rows)
            {
                string docId = row["id"]?.ToString();
                if (string.IsNullOrEmpty(docId) || !documents.ContainsKey(docId))
                    continue;

                if (includeLabels && row["labelvalue"] != DBNull.Value)
                {
                    string labelValue = row["labelvalue"]?.ToString();
                    if (!string.IsNullOrEmpty(labelValue))
                        labelsByDoc[docId].Add(labelValue);
                }

                if (includeTags && row["tagkey"] != DBNull.Value)
                {
                    string tagKey = row["tagkey"]?.ToString();
                    string tagValue = row["tagvalue"]?.ToString();
                    if (!string.IsNullOrEmpty(tagKey) && !tagsByDoc[docId].ContainsKey(tagKey))
                        tagsByDoc[docId][tagKey] = tagValue;
                }
            }

            foreach (var doc in documents.Values)
            {
                foreach (string label in labelsByDoc[doc.Id])
                    doc.Labels.Add(label);
                foreach (var tag in tagsByDoc[doc.Id])
                    doc.Tags[tag.Key] = tag.Value;
            }

            return documents;
        }

        public async IAsyncEnumerable<Document> ReadAllInCollection(
            string collectionId,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));

            string orderBy = GetOrderByClause(order);
            string query = $@"
                SELECT * FROM documents
                WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}'
                ORDER BY {orderBy}
                OFFSET {skip};
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.DocumentFromDataRow(row);
            }
        }

        public async Task<Document> Update(Document document, CancellationToken token = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            token.ThrowIfCancellationRequested();

            document.LastUpdateUtc = DateTime.UtcNow;

            string query = $@"
                UPDATE documents SET
                    name = {(document.Name != null ? $"'{Sanitizer.Sanitize(document.Name)}'" : "NULL")},
                    schemaid = '{Sanitizer.Sanitize(document.SchemaId)}',
                    lastupdateutc = '{Converters.ToTimestamp(document.LastUpdateUtc)}'
                WHERE id = '{Sanitizer.Sanitize(document.Id)}'
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.DocumentFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task Delete(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM documents WHERE id = '{Sanitizer.Sanitize(id)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task DeleteAllInCollection(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM documents WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<bool> Exists(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM documents WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]) > 0;

            return false;
        }

        public async Task<long> CountInCollection(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT COUNT(*) as cnt FROM documents WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["cnt"]);

            return 0;
        }

        public async Task<EnumerationResult<Document>> Enumerate(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            EnumerationResult<Document> result = new EnumerationResult<Document>
            {
                MaxResults = query.MaxResults,
                Skip = query.Skip,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            string countQuery = string.IsNullOrWhiteSpace(query.CollectionId)
                ? "SELECT COUNT(*) as cnt FROM documents;"
                : $"SELECT COUNT(*) as cnt FROM documents WHERE collectionid = '{Sanitizer.Sanitize(query.CollectionId)}';";

            DataTable countResult = await _Repo.ExecuteQueryAsync(countQuery, false, token);
            result.TotalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string orderBy = GetOrderByClause(query.Ordering);
            string whereClause = string.IsNullOrWhiteSpace(query.CollectionId)
                ? ""
                : $"WHERE collectionid = '{Sanitizer.Sanitize(query.CollectionId)}'";

            string dataQuery = $@"
                SELECT * FROM documents
                {whereClause}
                ORDER BY {orderBy}
                LIMIT {query.MaxResults} OFFSET {query.Skip};
            ";

            DataTable dataResult = await _Repo.ExecuteQueryAsync(dataQuery, false, token);

            foreach (DataRow row in dataResult.Rows)
            {
                result.Objects.Add(Converters.DocumentFromDataRow(row));
            }

            result.RecordsRemaining = Math.Max(0, result.TotalRecords - query.Skip - result.Objects.Count);
            result.EndOfResults = result.RecordsRemaining == 0;
            result.Timestamp.End = DateTime.UtcNow;

            return result;
        }

        private string GetOrderByClause(EnumerationOrderEnum order)
        {
            return order switch
            {
                EnumerationOrderEnum.CreatedAscending => "createdutc ASC",
                EnumerationOrderEnum.CreatedDescending => "createdutc DESC",
                EnumerationOrderEnum.LastUpdateAscending => "lastupdateutc ASC",
                EnumerationOrderEnum.LastUpdateDescending => "lastupdateutc DESC",
                EnumerationOrderEnum.NameAscending => "name ASC",
                EnumerationOrderEnum.NameDescending => "name DESC",
                _ => "createdutc DESC"
            };
        }
    }
}
