namespace Lattice.Core.Repositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    internal class FieldConstraintMethods : IFieldConstraintMethods
    {
        private readonly PostgresqlRepository _Repo;

        internal FieldConstraintMethods(PostgresqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<List<FieldConstraint>> CreateMany(List<FieldConstraint> constraints, CancellationToken token = default)
        {
            if (constraints == null || constraints.Count == 0) return new List<FieldConstraint>();
            token.ThrowIfCancellationRequested();

            List<string> statements = new List<string>();

            foreach (FieldConstraint constraint in constraints)
            {
                string allowedValuesJson = constraint.AllowedValues != null && constraint.AllowedValues.Count > 0
                    ? $"'{Sanitizer.Sanitize(System.Text.Json.JsonSerializer.Serialize(constraint.AllowedValues))}'"
                    : "NULL";

                statements.Add($@"
                    INSERT INTO fieldconstraints (id, collectionid, fieldpath, datatype, required, nullable,
                        regexpattern, minvalue, maxvalue, minlength, maxlength, allowedvalues, arrayelementtype,
                        createdutc, lastupdateutc)
                    VALUES ('{Sanitizer.Sanitize(constraint.Id)}',
                            '{Sanitizer.Sanitize(constraint.CollectionId)}',
                            '{Sanitizer.Sanitize(constraint.FieldPath)}',
                            {(constraint.DataType != null ? $"'{Sanitizer.Sanitize(constraint.DataType)}'" : "NULL")},
                            {constraint.Required},
                            {constraint.Nullable},
                            {(constraint.RegexPattern != null ? $"'{Sanitizer.Sanitize(constraint.RegexPattern)}'" : "NULL")},
                            {(constraint.MinValue.HasValue ? constraint.MinValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "NULL")},
                            {(constraint.MaxValue.HasValue ? constraint.MaxValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "NULL")},
                            {(constraint.MinLength.HasValue ? constraint.MinLength.Value.ToString() : "NULL")},
                            {(constraint.MaxLength.HasValue ? constraint.MaxLength.Value.ToString() : "NULL")},
                            {allowedValuesJson},
                            {(constraint.ArrayElementType != null ? $"'{Sanitizer.Sanitize(constraint.ArrayElementType)}'" : "NULL")},
                            '{Converters.ToTimestamp(constraint.CreatedUtc)}',
                            '{Converters.ToTimestamp(constraint.LastUpdateUtc)}');
                ");
            }

            await _Repo.ExecuteTransactionAsync(statements.ToArray(), token);

            return await ReadByCollectionId(constraints[0].CollectionId, token);
        }

        public async Task<List<FieldConstraint>> ReadByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"SELECT * FROM fieldconstraints WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}' ORDER BY fieldpath;";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            List<FieldConstraint> constraints = new List<FieldConstraint>();
            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                constraints.Add(Converters.FieldConstraintFromDataRow(row));
            }

            return constraints;
        }

        public async Task DeleteByCollectionId(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM fieldconstraints WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }
    }
}
