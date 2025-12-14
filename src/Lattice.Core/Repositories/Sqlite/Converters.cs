namespace Lattice.Core.Repositories.Sqlite
{
    using System;
    using System.Data;
    using Lattice.Core.Models;

    /// <summary>
    /// Converts DataRow objects to model objects.
    /// </summary>
    internal static class Converters
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Collection CollectionFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Collection
            {
                Id = row["id"]?.ToString(),
                Name = row["name"]?.ToString(),
                Description = row["description"]?.ToString(),
                DocumentsDirectory = row["documentsdirectory"]?.ToString(),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static Document DocumentFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Document
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                SchemaId = row["schemaid"]?.ToString(),
                Name = row["name"]?.ToString(),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static Schema SchemaFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Schema
            {
                Id = row["id"]?.ToString(),
                Name = row["name"]?.ToString(),
                Hash = row["hash"]?.ToString(),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
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
                Nullable = Convert.ToInt32(row["nullable"]) == 1,
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static Label LabelFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Label
            {
                Id = row["id"]?.ToString(),
                DocumentId = row["documentid"]?.ToString(),
                LabelValue = row["labelvalue"]?.ToString(),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
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
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static CollectionLabel CollectionLabelFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new CollectionLabel
            {
                Id = row["id"]?.ToString(),
                CollectionId = row["collectionid"]?.ToString(),
                LabelValue = row["labelvalue"]?.ToString(),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
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
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString())
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
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString())
            };
        }

        internal static string ToTimestamp(DateTime dt)
        {
            return dt.ToString(TimestampFormat);
        }
    }
}
