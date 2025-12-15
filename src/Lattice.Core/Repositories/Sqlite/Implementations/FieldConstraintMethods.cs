namespace Lattice.Core.Repositories.Sqlite.Implementations
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
    /// SQLite implementation of field constraint methods.
    /// </summary>
    internal class FieldConstraintMethods : IFieldConstraintMethods
    {
        private readonly SqliteRepository _Repo;

        internal FieldConstraintMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<List<FieldConstraint>> CreateMany(List<FieldConstraint> constraints, CancellationToken token = default)
        {
            if (constraints == null || constraints.Count == 0) return new List<FieldConstraint>();
            token.ThrowIfCancellationRequested();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION;");

            foreach (FieldConstraint constraint in constraints)
            {
                string allowedValuesJson = constraint.AllowedValues != null && constraint.AllowedValues.Count > 0
                    ? $"'{Sanitizer.Sanitize(System.Text.Json.JsonSerializer.Serialize(constraint.AllowedValues))}'"
                    : "NULL";

                sb.AppendLine($@"
                    INSERT INTO fieldconstraints (id, collectionid, fieldpath, datatype, required, nullable,
                        regexpattern, minvalue, maxvalue, minlength, maxlength, allowedvalues, arrayelementtype,
                        createdutc, lastupdateutc)
                    VALUES ('{Sanitizer.Sanitize(constraint.Id)}',
                            '{Sanitizer.Sanitize(constraint.CollectionId)}',
                            '{Sanitizer.Sanitize(constraint.FieldPath)}',
                            {(constraint.DataType != null ? $"'{Sanitizer.Sanitize(constraint.DataType)}'" : "NULL")},
                            {(constraint.Required ? 1 : 0)},
                            {(constraint.Nullable ? 1 : 0)},
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

            sb.AppendLine("COMMIT;");
            await _Repo.ExecuteNonQueryAsync(sb.ToString(), token);

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
