namespace Lattice.Core.Repositories
{
    using System;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// Abstract base class for repository implementations.
    /// </summary>
    public abstract class RepositoryBase : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Collection methods.
        /// </summary>
        public abstract ICollectionMethods Collections { get; }

        /// <summary>
        /// Document methods.
        /// </summary>
        public abstract IDocumentMethods Documents { get; }

        /// <summary>
        /// Schema methods.
        /// </summary>
        public abstract ISchemaMethods Schemas { get; }

        /// <summary>
        /// Schema element methods.
        /// </summary>
        public abstract ISchemaElementMethods SchemaElements { get; }

        /// <summary>
        /// Document value methods.
        /// </summary>
        public abstract IValueMethods Values { get; }

        /// <summary>
        /// Document label methods.
        /// </summary>
        public abstract ILabelMethods Labels { get; }

        /// <summary>
        /// Document tag methods.
        /// </summary>
        public abstract ITagMethods Tags { get; }

        /// <summary>
        /// Index table methods.
        /// </summary>
        public abstract IIndexMethods Indexes { get; }

        /// <summary>
        /// Field constraint methods.
        /// </summary>
        public abstract IFieldConstraintMethods FieldConstraints { get; }

        /// <summary>
        /// Indexed field methods.
        /// </summary>
        public abstract IIndexedFieldMethods IndexedFields { get; }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize the repository (create tables, etc.).
        /// </summary>
        public abstract void InitializeRepository();

        /// <summary>
        /// Flush in-memory data to disk (for in-memory mode).
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public abstract void Dispose();

        #endregion
    }
}
