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
        /// Prefix for tenant IDs.
        /// </summary>
        public const string TenantPrefix = "ten_";

        /// <summary>
        /// Prefix for user IDs.
        /// </summary>
        public const string UserPrefix = "usr_";

        /// <summary>
        /// Prefix for credential IDs.
        /// </summary>
        public const string CredentialPrefix = "crd_";

        /// <summary>
        /// Prefix for authentication session IDs.
        /// </summary>
        public const string AuthSessionPrefix = "ses_";

        /// <summary>
        /// Prefix for role IDs.
        /// </summary>
        public const string UserRolePrefix = "rol_";

        /// <summary>
        /// Prefix for permission IDs.
        /// </summary>
        public const string PermissionPrefix = "perm_";

        /// <summary>
        /// Prefix for role/permission map IDs.
        /// </summary>
        public const string RolePermissionMapPrefix = "rpm_";

        /// <summary>
        /// Prefix for user role assignment IDs.
        /// </summary>
        public const string UserRoleAssignmentPrefix = "ura_";

        /// <summary>
        /// Prefix for credential scope assignment IDs.
        /// </summary>
        public const string CredentialScopeAssignmentPrefix = "csa_";

        /// <summary>
        /// Prefix for audit entry IDs.
        /// </summary>
        public const string AuditPrefix = "aud_";

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

        /// <summary>
        /// Generate a new tenant ID.
        /// </summary>
        /// <returns>K-sortable tenant ID.</returns>
        public static string NewTenantId() => _Generator.GenerateKSortable(TenantPrefix, DefaultIdLength + TenantPrefix.Length);

        /// <summary>
        /// Generate a new user ID.
        /// </summary>
        /// <returns>K-sortable user ID.</returns>
        public static string NewUserId() => _Generator.GenerateKSortable(UserPrefix, DefaultIdLength + UserPrefix.Length);

        /// <summary>
        /// Generate a new credential ID.
        /// </summary>
        /// <returns>K-sortable credential ID.</returns>
        public static string NewCredentialId() => _Generator.GenerateKSortable(CredentialPrefix, DefaultIdLength + CredentialPrefix.Length);

        /// <summary>
        /// Generate a new authentication session ID.
        /// </summary>
        /// <returns>K-sortable session ID.</returns>
        public static string NewAuthSessionId() => _Generator.GenerateKSortable(AuthSessionPrefix, DefaultIdLength + AuthSessionPrefix.Length);

        /// <summary>
        /// Generate a new role ID.
        /// </summary>
        /// <returns>K-sortable role ID.</returns>
        public static string NewUserRoleId() => _Generator.GenerateKSortable(UserRolePrefix, DefaultIdLength + UserRolePrefix.Length);

        /// <summary>
        /// Generate a new permission ID.
        /// </summary>
        /// <returns>K-sortable permission ID.</returns>
        public static string NewPermissionId() => _Generator.GenerateKSortable(PermissionPrefix, DefaultIdLength + PermissionPrefix.Length);

        /// <summary>
        /// Generate a new role/permission map ID.
        /// </summary>
        /// <returns>K-sortable role/permission map ID.</returns>
        public static string NewRolePermissionMapId() => _Generator.GenerateKSortable(RolePermissionMapPrefix, DefaultIdLength + RolePermissionMapPrefix.Length);

        /// <summary>
        /// Generate a new user role assignment ID.
        /// </summary>
        /// <returns>K-sortable user role assignment ID.</returns>
        public static string NewUserRoleAssignmentId() => _Generator.GenerateKSortable(UserRoleAssignmentPrefix, DefaultIdLength + UserRoleAssignmentPrefix.Length);

        /// <summary>
        /// Generate a new credential scope assignment ID.
        /// </summary>
        /// <returns>K-sortable credential scope assignment ID.</returns>
        public static string NewCredentialScopeAssignmentId() => _Generator.GenerateKSortable(CredentialScopeAssignmentPrefix, DefaultIdLength + CredentialScopeAssignmentPrefix.Length);

        /// <summary>
        /// Generate a new audit entry ID.
        /// </summary>
        /// <returns>K-sortable audit entry ID.</returns>
        public static string NewAuditId() => _Generator.GenerateKSortable(AuditPrefix, DefaultIdLength + AuditPrefix.Length);

        #endregion
    }
}
