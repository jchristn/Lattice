namespace Lattice.Core.Helpers
{
    using PrettyId;

    /// <summary>
    /// Helper class for generating K-sortable unique identifiers.
    /// </summary>
    public static class IdGenerator
    {
        #region Public-Members

        /// <summary>
        /// Prefix for collection IDs.
        /// </summary>
        public const string CollectionPrefix = "col_";

        /// <summary>
        /// Prefix for document IDs.
        /// </summary>
        public const string DocumentPrefix = "doc_";

        /// <summary>
        /// Prefix for schema IDs.
        /// </summary>
        public const string SchemaPrefix = "sch_";

        /// <summary>
        /// Prefix for schema element IDs.
        /// </summary>
        public const string SchemaElementPrefix = "sel_";

        /// <summary>
        /// Prefix for document value IDs.
        /// </summary>
        public const string ValuePrefix = "val_";

        /// <summary>
        /// Prefix for document label IDs.
        /// </summary>
        public const string LabelPrefix = "lbl_";

        /// <summary>
        /// Prefix for document tag IDs.
        /// </summary>
        public const string TagPrefix = "tag_";

        /// <summary>
        /// Prefix for index table mapping IDs.
        /// </summary>
        public const string IndexTableMappingPrefix = "itm_";

        /// <summary>
        /// Prefix for field constraint IDs.
        /// </summary>
        public const string FieldConstraintPrefix = "fco_";

        /// <summary>
        /// Prefix for indexed field IDs.
        /// </summary>
        public const string IndexedFieldPrefix = "ixf_";

        /// <summary>
        /// Prefix for object lock IDs.
        /// </summary>
        public const string ObjectLockPrefix = "lock_";

        /// <summary>
        /// Default ID length (excluding prefix).
        /// </summary>
        private const int DefaultIdLength = 24;

        #endregion

        #region Private-Members

        private static readonly PrettyId.IdGenerator _Generator = new PrettyId.IdGenerator();

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generate a new collection ID.
        /// </summary>
        /// <returns>K-sortable collection ID.</returns>
        public static string NewCollectionId() => _Generator.GenerateKSortable(CollectionPrefix, DefaultIdLength + CollectionPrefix.Length);

        /// <summary>
        /// Generate a new document ID.
        /// </summary>
        /// <returns>K-sortable document ID.</returns>
        public static string NewDocumentId() => _Generator.GenerateKSortable(DocumentPrefix, DefaultIdLength + DocumentPrefix.Length);

        /// <summary>
        /// Generate a new schema ID.
        /// </summary>
        /// <returns>K-sortable schema ID.</returns>
        public static string NewSchemaId() => _Generator.GenerateKSortable(SchemaPrefix, DefaultIdLength + SchemaPrefix.Length);

        /// <summary>
        /// Generate a new schema element ID.
        /// </summary>
        /// <returns>K-sortable schema element ID.</returns>
        public static string NewSchemaElementId() => _Generator.GenerateKSortable(SchemaElementPrefix, DefaultIdLength + SchemaElementPrefix.Length);

        /// <summary>
        /// Generate a new value ID.
        /// </summary>
        /// <returns>K-sortable value ID.</returns>
        public static string NewValueId() => _Generator.GenerateKSortable(ValuePrefix, DefaultIdLength + ValuePrefix.Length);

        /// <summary>
        /// Generate a new document label ID.
        /// </summary>
        /// <returns>K-sortable label ID.</returns>
        public static string NewLabelId() => _Generator.GenerateKSortable(LabelPrefix, DefaultIdLength + LabelPrefix.Length);

        /// <summary>
        /// Generate a new document tag ID.
        /// </summary>
        /// <returns>K-sortable tag ID.</returns>
        public static string NewTagId() => _Generator.GenerateKSortable(TagPrefix, DefaultIdLength + TagPrefix.Length);

        /// <summary>
        /// Generate a new index table mapping ID.
        /// </summary>
        /// <returns>K-sortable index table mapping ID.</returns>
        public static string NewIndexTableMappingId() => _Generator.GenerateKSortable(IndexTableMappingPrefix, DefaultIdLength + IndexTableMappingPrefix.Length);

        /// <summary>
        /// Generate a new field constraint ID.
        /// </summary>
        /// <returns>K-sortable field constraint ID.</returns>
        public static string NewFieldConstraintId() => _Generator.GenerateKSortable(FieldConstraintPrefix, DefaultIdLength + FieldConstraintPrefix.Length);

        /// <summary>
        /// Generate a new indexed field ID.
        /// </summary>
        /// <returns>K-sortable indexed field ID.</returns>
        public static string NewIndexedFieldId() => _Generator.GenerateKSortable(IndexedFieldPrefix, DefaultIdLength + IndexedFieldPrefix.Length);

        /// <summary>
        /// Generate a new object lock ID.
        /// </summary>
        /// <returns>K-sortable object lock ID.</returns>
        public static string NewObjectLockId() => _Generator.GenerateKSortable(ObjectLockPrefix, DefaultIdLength + ObjectLockPrefix.Length);

        #endregion
    }
}
