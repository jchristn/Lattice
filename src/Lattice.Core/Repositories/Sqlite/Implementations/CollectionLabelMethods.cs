namespace Lattice.Core.Repositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQLite implementation of collection label methods.
    /// </summary>
    internal class CollectionLabelMethods : ICollectionLabelMethods
    {
        private readonly SqliteRepository _Repo;

        internal CollectionLabelMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<CollectionLabel> Create(CollectionLabel label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO collectionlabels (id, collectionid, labelvalue, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(label.Id)}',
                        '{Sanitizer.Sanitize(label.CollectionId)}',
                        '{Sanitizer.Sanitize(label.LabelValue)}',
                        '{Converters.ToTimestamp(label.CreatedUtc)}',
                        '{Converters.ToTimestamp(label.LastUpdateUtc)}');
                SELECT * FROM collectionlabels WHERE id = '{Sanitizer.Sanitize(label.Id)}';
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.CollectionLabelFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<CollectionLabel>> CreateMany(List<CollectionLabel> labels, CancellationToken token = default)
        {
            if (labels == null || labels.Count == 0) return new List<CollectionLabel>();
            token.ThrowIfCancellationRequested();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION;");

            foreach (CollectionLabel label in labels)
            {
                sb.AppendLine($@"
                    INSERT INTO collectionlabels (id, collectionid, labelvalue, createdutc, lastupdateutc)
                    VALUES ('{Sanitizer.Sanitize(label.Id)}',
                            '{Sanitizer.Sanitize(label.CollectionId)}',
                            '{Sanitizer.Sanitize(label.LabelValue)}',
                            '{Converters.ToTimestamp(label.CreatedUtc)}',
                            '{Converters.ToTimestamp(label.LastUpdateUtc)}');
                ");
            }

            sb.AppendLine("COMMIT;");
            await _Repo.ExecuteNonQueryAsync(sb.ToString(), token);

            return labels;
        }

        public async IAsyncEnumerable<CollectionLabel> ReadByCollectionId(string collectionId, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));

            string query = $"SELECT * FROM collectionlabels WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.CollectionLabelFromDataRow(row);
            }
        }

        public async Task DeleteByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM collectionlabels WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }
    }
}
