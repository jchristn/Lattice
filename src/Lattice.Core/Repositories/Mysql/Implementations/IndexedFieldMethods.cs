namespace Lattice.Core.Repositories.Mysql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// MySQL implementation of indexed field methods.
    /// </summary>
    internal class IndexedFieldMethods : IIndexedFieldMethods
    {
        private readonly MysqlRepository _Repo;

        internal IndexedFieldMethods(MysqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<List<IndexedField>> CreateMany(List<IndexedField> fields, CancellationToken token = default)
        {
            if (fields == null || fields.Count == 0) return new List<IndexedField>();
            token.ThrowIfCancellationRequested();

            List<string> statements = new List<string>();

            foreach (IndexedField field in fields)
            {
                statements.Add($@"
                    INSERT INTO `indexedfields` (`id`, `collectionid`, `fieldpath`, `createdutc`, `lastupdateutc`)
                    VALUES ('{Sanitizer.Sanitize(field.Id)}',
                            '{Sanitizer.Sanitize(field.CollectionId)}',
                            '{Sanitizer.Sanitize(field.FieldPath)}',
                            '{Converters.ToTimestamp(field.CreatedUtc)}',
                            '{Converters.ToTimestamp(field.LastUpdateUtc)}');
                ");
            }

            await _Repo.ExecuteTransactionAsync(statements.ToArray(), token);

            return await ReadByCollectionId(fields[0].CollectionId, token);
        }

        public async Task<List<IndexedField>> ReadByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM `indexedfields` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}' ORDER BY `fieldpath`;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            List<IndexedField> fields = new List<IndexedField>();
            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                fields.Add(Converters.IndexedFieldFromDataRow(row));
            }

            return fields;
        }

        public async Task DeleteByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM `indexedfields` WHERE `collectionid` = '{Sanitizer.Sanitize(collectionId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }
    }
}
