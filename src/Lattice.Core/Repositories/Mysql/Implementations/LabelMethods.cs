namespace Lattice.Core.Repositories.Mysql.Implementations
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
    /// MySQL implementation of label methods (unified for collections and documents).
    /// </summary>
    internal class LabelMethods : ILabelMethods
    {
        private readonly MysqlRepository _Repo;

        internal LabelMethods(MysqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #region Common

        public async Task<Label> Create(Label label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO `labels` (`id`, `collectionid`, `documentid`, `labelvalue`, `createdutc`, `lastupdateutc`)
                VALUES ('{Sanitizer.Sanitize(label.Id)}',
                        {(label.CollectionId != null ? $"'{Sanitizer.Sanitize(label.CollectionId)}'" : "NULL")},
                        {(label.DocumentId != null ? $"'{Sanitizer.Sanitize(label.DocumentId)}'" : "NULL")},
                        '{Sanitizer.Sanitize(label.LabelValue)}',
                        '{Converters.ToTimestamp(label.CreatedUtc)}',
                        '{Converters.ToTimestamp(label.LastUpdateUtc)}');
            ";

            await _Repo.ExecuteNonQueryAsync(query, token);

            string selectQuery = $"SELECT * FROM `labels` WHERE `id` = '{Sanitizer.Sanitize(label.Id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(selectQuery, false, token);
            if (result.Rows.Count > 0)
                return Converters.LabelFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<Label>> CreateMany(List<Label> labels, CancellationToken token = default)
        {
            if (labels == null || labels.Count == 0) return new List<Label>();
            token.ThrowIfCancellationRequested();

            List<string> statements = new List<string>();
            foreach (Label label in labels)
            {
                statements.Add($@"
                    INSERT INTO `labels` (`id`, `collectionid`, `documentid`, `labelvalue`, `createdutc`, `lastupdateutc`)
                    VALUES ('{Sanitizer.Sanitize(label.Id)}',
                            {(label.CollectionId != null ? $"'{Sanitizer.Sanitize(label.CollectionId)}'" : "NULL")},
                            {(label.DocumentId != null ? $"'{Sanitizer.Sanitize(label.DocumentId)}'" : "NULL")},
                            '{Sanitizer.Sanitize(label.LabelValue)}',
                            '{Converters.ToTimestamp(label.CreatedUtc)}',
                            '{Converters.ToTimestamp(label.LastUpdateUtc)}');
                ");
            }

            await _Repo.ExecuteTransactionAsync(statements.ToArray(), token);

            return labels;
        }

        #endregion

        #region Document-Labels

        public async IAsyncEnumerable<Label> ReadByDocumentId(string documentId, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentNullException(nameof(documentId));

            string query = $"SELECT * FROM `labels` WHERE `documentid` = '{Sanitizer.Sanitize(documentId)}';";
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

            foreach (string docId in documentIds)
            {
                result[docId] = new List<string>();
            }

            string inClause = string.Join(",", documentIds.Select(id => $"'{Sanitizer.Sanitize(id)}'"));
            string query = $"SELECT `documentid`, `labelvalue` FROM `labels` WHERE `documentid` IN ({inClause});";
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

            string query = $"DELETE FROM `labels` WHERE `documentid` = '{Sanitizer.Sanitize(documentId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async IAsyncEnumerable<string> FindDocumentIdsByLabel(string labelValue, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(labelValue)) throw new ArgumentNullException(nameof(labelValue));

            string query = $"SELECT DISTINCT `documentid` FROM `labels` WHERE `labelvalue` = '{Sanitizer.Sanitize(labelValue)}' AND `documentid` IS NOT NULL;";
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

            if (labels.Count == 1)
            {
                string query = $"SELECT DISTINCT `documentid` FROM `labels` WHERE `labelvalue` = '{Sanitizer.Sanitize(labels[0])}' AND `documentid` IS NOT NULL;";
                DataTable dataTable = await _Repo.ExecuteQueryAsync(query, false, token);
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(row["documentid"]?.ToString());
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT DISTINCT l0.`documentid` FROM `labels` l0 ");

                for (int i = 1; i < labels.Count; i++)
                {
                    sb.Append($"INNER JOIN `labels` l{i} ON l0.`documentid` = l{i}.`documentid` ");
                }

                sb.Append("WHERE l0.`documentid` IS NOT NULL ");
                for (int i = 0; i < labels.Count; i++)
                {
                    sb.Append($" AND l{i}.`labelvalue` = '{Sanitizer.Sanitize(labels[i])}'");
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

        #endregion

        #region Collection-Labels

        public async IAsyncEnumerable<Label> ReadByCollectionId(string collectionId, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));

            string query = $"SELECT * FROM `labels` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}' AND `documentid` IS NULL;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.LabelFromDataRow(row);
            }
        }

        public async Task DeleteByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM `labels` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}' AND `documentid` IS NULL;";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        #endregion
    }
}
