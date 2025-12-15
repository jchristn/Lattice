namespace Lattice.Core.Repositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    internal class SchemaElementMethods : ISchemaElementMethods
    {
        private readonly PostgresqlRepository _Repo;

        internal SchemaElementMethods(PostgresqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<SchemaElement> Create(SchemaElement element, CancellationToken token = default)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            token.ThrowIfCancellationRequested();

            string query = $@"
                INSERT INTO schemaelements (id, schemaid, position, key, datatype, nullable, createdutc, lastupdateutc)
                VALUES ('{Sanitizer.Sanitize(element.Id)}',
                        '{Sanitizer.Sanitize(element.SchemaId)}',
                        {element.Position},
                        '{Sanitizer.Sanitize(element.Key)}',
                        '{Sanitizer.Sanitize(element.DataType)}',
                        {element.Nullable},
                        '{Converters.ToTimestamp(element.CreatedUtc)}',
                        '{Converters.ToTimestamp(element.LastUpdateUtc)}')
                RETURNING *;
            ";

            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);
            if (result.Rows.Count > 0)
                return Converters.SchemaElementFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<List<SchemaElement>> CreateMany(List<SchemaElement> elements, CancellationToken token = default)
        {
            if (elements == null || elements.Count == 0) return new List<SchemaElement>();
            token.ThrowIfCancellationRequested();

            List<string> statements = new List<string>();
            foreach (SchemaElement element in elements)
            {
                statements.Add($@"
                    INSERT INTO schemaelements (id, schemaid, position, key, datatype, nullable, createdutc, lastupdateutc)
                    VALUES ('{Sanitizer.Sanitize(element.Id)}',
                            '{Sanitizer.Sanitize(element.SchemaId)}',
                            {element.Position},
                            '{Sanitizer.Sanitize(element.Key)}',
                            '{Sanitizer.Sanitize(element.DataType)}',
                            {element.Nullable},
                            '{Converters.ToTimestamp(element.CreatedUtc)}',
                            '{Converters.ToTimestamp(element.LastUpdateUtc)}');
                ");
            }

            await _Repo.ExecuteTransactionAsync(statements.ToArray(), token);

            return elements;
        }

        public async Task<SchemaElement> ReadById(string id, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM schemaelements WHERE id = '{Sanitizer.Sanitize(id)}';";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.SchemaElementFromDataRow(result.Rows[0]);

            return null;
        }

        public async IAsyncEnumerable<SchemaElement> ReadBySchemaId(string schemaId, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentNullException(nameof(schemaId));

            string query = $"SELECT * FROM schemaelements WHERE schemaid = '{Sanitizer.Sanitize(schemaId)}' ORDER BY position;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.SchemaElementFromDataRow(row);
            }
        }

        public async Task<SchemaElement> ReadBySchemaIdAndKey(string schemaId, string key, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentNullException(nameof(schemaId));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            token.ThrowIfCancellationRequested();

            string query = $@"
                SELECT * FROM schemaelements
                WHERE schemaid = '{Sanitizer.Sanitize(schemaId)}'
                AND key = '{Sanitizer.Sanitize(key)}';
            ";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.SchemaElementFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task DeleteBySchemaId(string schemaId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentNullException(nameof(schemaId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM schemaelements WHERE schemaid = '{Sanitizer.Sanitize(schemaId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }
    }
}
