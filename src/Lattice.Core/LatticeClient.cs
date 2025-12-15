namespace Lattice.Core
{
    using System;
    using Lattice.Core.Client.Implementations;
    using Lattice.Core.Client.Interfaces;
    using Lattice.Core.Flattening;
    using Lattice.Core.Repositories;
    using Lattice.Core.Repositories.Sqlite;
    using Lattice.Core.Schema;
    using Lattice.Core.Search;
    using Lattice.Core.Validation;

    /// <summary>
    /// Main client for interacting with the Lattice document store.
    /// </summary>
    public class LatticeClient : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Settings for the client.
        /// </summary>
        public LatticeSettings Settings { get; }

        /// <summary>
        /// Collection methods.
        /// </summary>
        public ICollectionMethods Collection { get; }

        /// <summary>
        /// Document methods.
        /// </summary>
        public IDocumentMethods Document { get; }

        /// <summary>
        /// Search methods.
        /// </summary>
        public ISearchMethods Search { get; }

        /// <summary>
        /// Schema methods.
        /// </summary>
        public ISchemaMethods Schema { get; }

        /// <summary>
        /// Index methods.
        /// </summary>
        public IIndexMethods Index { get; }

        #endregion

        #region Private-Members

        private readonly RepositoryBase _Repo;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the Lattice client.
        /// </summary>
        /// <param name="settings">Client settings.</param>
        public LatticeClient(LatticeSettings settings = null)
        {
            Settings = settings ?? new LatticeSettings();

            _Repo = new SqliteRepository(Settings.Database.Filename, Settings.InMemory);
            _Repo.InitializeRepository();

            ISchemaGenerator schemaGenerator = new SchemaGenerator();
            IJsonFlattener jsonFlattener = new JsonFlattener();
            SqlParser sqlParser = new SqlParser();
            ISchemaValidator schemaValidator = new SchemaValidator();

            // Initialize method groups
            Collection = new CollectionMethods(this, _Repo, Settings, jsonFlattener);
            Document = new DocumentMethods(this, _Repo, Settings, schemaGenerator, jsonFlattener, schemaValidator);
            Search = new SearchMethods(this, _Repo, sqlParser);
            Schema = new SchemaMethods(this, _Repo);
            Index = new IndexMethods(this, _Repo);
        }

        /// <summary>
        /// Instantiate the Lattice client with a custom repository.
        /// </summary>
        /// <param name="repo">Repository implementation.</param>
        /// <param name="settings">Client settings.</param>
        public LatticeClient(RepositoryBase repo, LatticeSettings settings = null)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            Settings = settings ?? new LatticeSettings();

            ISchemaGenerator schemaGenerator = new SchemaGenerator();
            IJsonFlattener jsonFlattener = new JsonFlattener();
            SqlParser sqlParser = new SqlParser();
            ISchemaValidator schemaValidator = new SchemaValidator();

            // Initialize method groups
            Collection = new CollectionMethods(this, _Repo, Settings, jsonFlattener);
            Document = new DocumentMethods(this, _Repo, Settings, schemaGenerator, jsonFlattener, schemaValidator);
            Search = new SearchMethods(this, _Repo, sqlParser);
            Schema = new SchemaMethods(this, _Repo);
            Index = new IndexMethods(this, _Repo);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Flush in-memory data to disk.
        /// </summary>
        public void Flush()
        {
            _Repo.Flush();
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;

            _Repo?.Dispose();
            _Disposed = true;
        }

        #endregion
    }
}
