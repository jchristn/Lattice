namespace Lattice.Core.Repositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using Lattice.Core.Models;

    /// <summary>
    /// Converts DataRow objects to model objects for PostgreSQL.
    /// </summary>
    internal static class Converters
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Collection CollectionFromDataRow(DataRow row)
        {
            if (row == null) return null;

            var collection = new Collection
            {
                Id = row["id"]?.ToString(),
                Name = row["name"]?.ToString(),
                Description = row["description"]?.ToString(),
                DocumentsDirectory = row["documentsdirectory"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };

            if (row.Table.Columns.Contains("schemaenforcementmode") && row["schemaenforcementmode"] != DBNull.Value)
            {
                collection.SchemaEnforcementMode = (SchemaEnforcementMode)Convert.ToInt32(row["schemaenforcementmode"]);
            }

            if (row.Table.Columns.Contains("indexingmode") && row["indexingmode"] != DBNull.Value)
            {
                collection.IndexingMode = (IndexingMode)Convert.ToInt32(row["indexingmode"]);
            }

            return collection;
        }

        internal static Document DocumentFromDataRow(DataRow row)
        {
            if (row == null) return null;

            var doc = new Document
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                SchemaId = row["schemaid"]?.ToString(),
                Name = row["name"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };

            if (row.Table.Columns.Contains("contentlength") && row["contentlength"] != DBNull.Value)
            {
                doc.ContentLength = Convert.ToInt64(row["contentlength"]);
            }

            if (row.Table.Columns.Contains("sha256hash") && row["sha256hash"] != DBNull.Value)
            {
                doc.Sha256Hash = row["sha256hash"]?.ToString();
            }

            return doc;
        }

        internal static Schema SchemaFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Schema
            {
                Id = row["id"]?.ToString(),
                Name = row["name"]?.ToString(),
                Hash = row["hash"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static SchemaElement SchemaElementFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new SchemaElement
            {
                Id = row["id"]?.ToString(),
                SchemaId = row["schemaid"]?.ToString(),
                Position = Convert.ToInt32(row["position"]),
                Key = row["key"]?.ToString(),
                DataType = row["datatype"]?.ToString(),
                Nullable = Convert.ToBoolean(row["nullable"]),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static Label LabelFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Label
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"] != DBNull.Value ? row["collectionid"]?.ToString() : null,
                DocumentId = row["documentid"] != DBNull.Value ? row["documentid"]?.ToString() : null,
                LabelValue = row["labelvalue"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static Tag TagFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Tag
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"] != DBNull.Value ? row["collectionid"]?.ToString() : null,
                DocumentId = row["documentid"] != DBNull.Value ? row["documentid"]?.ToString() : null,
                Key = row["key"]?.ToString(),
                Value = row["value"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }

        internal static IndexTableMapping IndexTableMappingFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new IndexTableMapping
            {
                Id = row["id"]?.ToString(),
                Key = row["key"]?.ToString(),
                TableName = row["tablename"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"])
            };
        }

        internal static DocumentValue DocumentValueFromDataRow(DataRow row)
        {
            if (row == null) return null;

            int? position = null;
            if (row["position"] != DBNull.Value)
                position = Convert.ToInt32(row["position"]);

            return new DocumentValue
            {
                Id = row["id"]?.ToString(),
                DocumentId = row["documentid"]?.ToString(),
                Position = position,
                Value = row["value"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"])
            };
        }

        internal static string ToTimestamp(DateTime dt)
        {
            return dt.ToString(TimestampFormat);
        }

        internal static FieldConstraint FieldConstraintFromDataRow(DataRow row)
        {
            if (row == null) return null;

            var constraint = new FieldConstraint
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                FieldPath = row["fieldpath"]?.ToString(),
                DataType = row["datatype"] != DBNull.Value ? row["datatype"]?.ToString() : null,
                Required = row["required"] != DBNull.Value && Convert.ToBoolean(row["required"]),
                Nullable = row["nullable"] == DBNull.Value || Convert.ToBoolean(row["nullable"]),
                RegexPattern = row["regexpattern"] != DBNull.Value ? row["regexpattern"]?.ToString() : null,
                ArrayElementType = row["arrayelementtype"] != DBNull.Value ? row["arrayelementtype"]?.ToString() : null,
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };

            if (row["minvalue"] != DBNull.Value)
                constraint.MinValue = Convert.ToDecimal(row["minvalue"]);

            if (row["maxvalue"] != DBNull.Value)
                constraint.MaxValue = Convert.ToDecimal(row["maxvalue"]);

            if (row["minlength"] != DBNull.Value)
                constraint.MinLength = Convert.ToInt32(row["minlength"]);

            if (row["maxlength"] != DBNull.Value)
                constraint.MaxLength = Convert.ToInt32(row["maxlength"]);

            if (row["allowedvalues"] != DBNull.Value)
            {
                string allowedValuesJson = row["allowedvalues"]?.ToString();
                if (!string.IsNullOrWhiteSpace(allowedValuesJson))
                {
                    try
                    {
                        constraint.AllowedValues = JsonSerializer.Deserialize<List<string>>(allowedValuesJson);
                    }
                    catch
                    {
                        constraint.AllowedValues = new List<string>();
                    }
                }
            }

            return constraint;
        }

        internal static IndexedField IndexedFieldFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new IndexedField
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                FieldPath = row["fieldpath"]?.ToString(),
                CreatedUtc = Convert.ToDateTime(row["createdutc"]),
                LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"])
            };
        }
    }
}
