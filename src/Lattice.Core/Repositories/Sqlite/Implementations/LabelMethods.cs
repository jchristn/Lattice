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
    /// SQLite implementation of document label methods.
    /// </summary>
    internal class LabelMethods : ILabelMethods
    {
        private readonly SqliteRepository _Repo;

        internal LabelMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Label> Create(Label label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO labels (id, documentid, labelvalue, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(label.Id)}',
                        '{Sanitizer.Sanitize(label.DocumentId)}',
                        '{Sanitizer.Sanitize(label.LabelValue)}',
                        '{Converters.ToTimestamp(label.CreatedUtc)}',
                        '{Converters.ToTimestamp(label.LastUpdateUtc)}');
                SELECT * FROM labels WHERE id = '{Sanitizer.Sanitize(label.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.LabelFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<Label>> CreateMany(List<Label> labels, CancellationToken token = default)
        {
            if (labels == null || labels.Count == 0) return new List<Label>();
            token.ThrowIfCancellationRequested();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION;");

            foreach (Label label in labels)
            {
                sb.AppendLine($@"
                    INSERT INTO labels (id, documentid, labelvalue, createdutc, lastupdateutc)
                    VALUES ('{Sanitizer.Sanitize(label.Id)}',
                            '{Sanitizer.Sanitize(label.DocumentId)}',
                            '{Sanitizer.Sanitize(label.LabelValue)}',
                            '{Converters.ToTimestamp(label.CreatedUtc)}',
                            '{Converters.ToTimestamp(label.LastUpdateUtc)}');
                ");
            }

            sb.AppendLine("COMMIT;");
            await _Repo.ExecuteNonQueryAsync(sb.ToString(), token);

            return labels;
        }

        public async IAsyncEnumerable<Label> ReadByDocumentId(string documentId, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentNullException(nameof(documentId));

            string query = $"SELECT * FROM labels WHERE documentid = '{Sanitizer.Sanitize(documentId)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.LabelFromDataRow(row);
            }
        }

        public async Task<Dictionary<string, List<string>>> ReadByDocumentIds(List<string> documentIds, CancellationToken token = default)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            if (documentIds == null || documentIds.Count == 0) return result;
            token.ThrowIfCancellationRequested();

            // Initialize empty lists for all requested IDs
            foreach (string docId in documentIds)
            {
                result[docId] = new List<string>();
            }

            // Build IN clause with sanitized IDs
            string inClause = string.Join(",", documentIds.Select(id => $"'{Sanitizer.Sanitize(id)}'"));
            string query = $"SELECT documentid, labelvalue FROM labels WHERE documentid IN ({inClause});";
            DataTable dataTable = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in dataTable.Rows)
            {
                string docId = row["documentid"]?.ToString();
                string labelValue = row["labelvalue"]?.ToString();
                if (!string.IsNullOrEmpty(docId) && result.ContainsKey(docId))
                {
                    result[docId].Add(labelValue);
                }
            }

            return result;
        }

        public async Task DeleteByDocumentId(string documentId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentNullException(nameof(documentId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM labels WHERE documentid = '{Sanitizer.Sanitize(documentId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async IAsyncEnumerable<string> FindDocumentIdsByLabel(string labelValue, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(labelValue)) throw new ArgumentNullException(nameof(labelValue));

            string query = $"SELECT DISTINCT documentid FROM labels WHERE labelvalue = '{Sanitizer.Sanitize(labelValue)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return row["documentid"]?.ToString();
            }
        }

        public async Task<HashSet<string>> FindDocumentIdsByLabels(List<string> labels, CancellationToken token = default)
        {
            HashSet<string> result = new HashSet<string>();
            if (labels == null || labels.Count == 0) return result;
            token.ThrowIfCancellationRequested();

            // Use JOIN query to find documents that have ALL specified labels
            // For N labels, we self-join the labels table N times and require each join to match a different label
            if (labels.Count == 1)
            {
                // Single label: simple query
                string query = $"SELECT DISTINCT documentid FROM labels WHERE labelvalue = '{Sanitizer.Sanitize(labels[0])}';";
                DataTable dataTable = await _Repo.ExecuteQueryAsync(query, false, token);
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(row["documentid"]?.ToString());
                }
            }
            else
            {
                // Multiple labels: use self-joins
                // SELECT l0.documentid FROM labels l0
                // INNER JOIN labels l1 ON l0.documentid = l1.documentid
                // INNER JOIN labels l2 ON l0.documentid = l2.documentid
                // WHERE l0.labelvalue = 'label0' AND l1.labelvalue = 'label1' AND l2.labelvalue = 'label2'
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT DISTINCT l0.documentid FROM labels l0 ");

                // Add JOINs
                for (int i = 1; i < labels.Count; i++)
                {
                    sb.Append($"INNER JOIN labels l{i} ON l0.documentid = l{i}.documentid ");
                }

                // Add WHERE clause
                sb.Append("WHERE ");
                for (int i = 0; i < labels.Count; i++)
                {
                    if (i > 0) sb.Append(" AND ");
                    sb.Append($"l{i}.labelvalue = '{Sanitizer.Sanitize(labels[i])}'");
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
    }
}
