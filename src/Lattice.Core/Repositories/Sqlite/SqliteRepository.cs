namespace Lattice.Core.Repositories.Sqlite
{
    using System;
    using System.Data;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.Sqlite;
    using Lattice.Core.Repositories.Interfaces;
    using Lattice.Core.Repositories.Sqlite.Implementations;
    using Lattice.Core.Repositories.Sqlite.Queries;

    /// <summary>
    /// SQLite implementation of the repository.
    /// </summary>
    public class SqliteRepository : RepositoryBase
    {
        #region Public-Members

        /// <inheritdoc />
        public override ICollectionMethods Collections { get; }

        /// <inheritdoc />
        public override IDocumentMethods Documents { get; }

        /// <inheritdoc />
        public override ISchemaMethods Schemas { get; }

        /// <inheritdoc />
        public override ISchemaElementMethods SchemaElements { get; }

        /// <inheritdoc />
        public override IValueMethods Values { get; }

        /// <inheritdoc />
        public override ILabelMethods Labels { get; }

        /// <inheritdoc />
        public override ITagMethods Tags { get; }

        /// <inheritdoc />
        public override ICollectionLabelMethods CollectionLabels { get; }

        /// <inheritdoc />
        public override IIndexMethods Indexes { get; }

        /// <summary>
        /// The number of connections in the pool.
        /// </summary>
        public int PoolSize { get; }

        #endregion

        #region Private-Members

        private readonly string _Filename;
        private readonly bool _InMemory;
        private readonly string _ConnectionString;
        private readonly SqliteConnectionPool _ConnectionPool;
        private readonly ReaderWriterLockSlim _ReadWriteLock;
        private readonly SqliteConnection _KeepaliveConnection; // Required for in-memory mode
        private bool _Disposed = false;

        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";
        private const int DefaultPoolSize = 4;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the SQLite repository.
        /// </summary>
        /// <param name="filename">Database filename.</param>
        /// <param name="inMemory">Use in-memory mode.</param>
        /// <param name="poolSize">Number of connections in the pool. Default is 4.</param>
        public SqliteRepository(string filename = "lattice.db", bool inMemory = false, int poolSize = DefaultPoolSize)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException(nameof(filename));
            if (poolSize < 1)
                throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be at least 1.");

            _Filename = filename;
            _InMemory = inMemory;
            PoolSize = poolSize;

            if (!_InMemory)
            {
                _ConnectionString = $"Data Source={_Filename};Pooling=false";
            }
            else
            {
                // Shared cache mode allows multiple connections to the same in-memory database
                _ConnectionString = "Data Source=LatticeMemory;Mode=Memory;Cache=Shared";

                // Keep one connection open to prevent in-memory database from being destroyed
                _KeepaliveConnection = new SqliteConnection(_ConnectionString);
                _KeepaliveConnection.Open();
            }

            _ConnectionPool = new SqliteConnectionPool(_ConnectionString, poolSize);
            _ReadWriteLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            // Initialize method implementations
            Collections = new CollectionMethods(this);
            Documents = new DocumentMethods(this);
            Schemas = new SchemaMethods(this);
            SchemaElements = new SchemaElementMethods(this);
            Values = new ValueMethods(this);
            Labels = new LabelMethods(this);
            Tags = new TagMethods(this);
            CollectionLabels = new CollectionLabelMethods(this);
            Indexes = new IndexMethods(this);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            // Load existing database into memory if applicable
            if (_InMemory && File.Exists(_Filename))
            {
                using (SqliteConnection diskDatabase = new SqliteConnection($"Data Source={_Filename};Pooling=false"))
                {
                    diskDatabase.Open();
                    diskDatabase.BackupDatabase(_KeepaliveConnection);
                }
            }

            // Enable WAL mode for better concurrent read/write performance
            // WAL allows readers and writers to proceed concurrently
            ExecuteNonQuery("PRAGMA journal_mode=WAL;");

            // Create tables and indices
            ExecuteNonQuery(SetupQueries.CreateTablesAndIndices());
        }

        /// <inheritdoc />
        public override void Flush()
        {
            if (_InMemory)
            {
                string backupPath = _Filename + ".backup";

                // Create backup of existing file
                if (File.Exists(_Filename))
                    File.Copy(_Filename, backupPath, true);

                try
                {
                    // Delete existing file to replace it
                    if (File.Exists(_Filename))
                        File.Delete(_Filename);

                    using (SqliteConnection diskDatabase = new SqliteConnection($"Data Source={_Filename};Pooling=false"))
                    {
                        diskDatabase.Open();
                        _KeepaliveConnection.BackupDatabase(diskDatabase);
                    }

                    // Remove backup on success
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                }
                catch
                {
                    // Restore from backup on failure
                    if (File.Exists(backupPath))
                    {
                        if (File.Exists(_Filename))
                            File.Delete(_Filename);
                        File.Copy(backupPath, _Filename, true);
                        File.Delete(backupPath);
                    }
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (_Disposed) return;

            _ConnectionPool?.Dispose();
            _KeepaliveConnection?.Close();
            _KeepaliveConnection?.Dispose();
            _ReadWriteLock?.Dispose();

            _Disposed = true;
        }

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Execute a query and return the result as a DataTable.
        /// </summary>
        internal DataTable ExecuteQuery(string query, bool isTransaction = false)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            query = query.Trim();

            // Transactions require write lock since they may modify data
            if (isTransaction)
            {
                query = "BEGIN TRANSACTION; " + query + " COMMIT;";
                return ExecuteWithWriteLock(query);
            }

            return ExecuteWithReadLock(query);
        }

        /// <summary>
        /// Execute a query asynchronously.
        /// </summary>
        internal async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            query = query.Trim();
            token.ThrowIfCancellationRequested();

            // Transactions require write lock since they may modify data
            if (isTransaction)
            {
                query = "BEGIN TRANSACTION; " + query + " COMMIT;";
                return await Task.Run(() => ExecuteWithWriteLock(query), token);
            }

            return await Task.Run(() => ExecuteWithReadLock(query), token);
        }

        /// <summary>
        /// Execute a parameterized query.
        /// </summary>
        internal DataTable ExecuteParameterizedQuery(string query, params SqliteParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            return ExecuteParameterizedWithReadLock(query, parameters);
        }

        /// <summary>
        /// Execute a parameterized query asynchronously.
        /// </summary>
        internal async Task<DataTable> ExecuteParameterizedQueryAsync(string query, CancellationToken token = default, params SqliteParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            token.ThrowIfCancellationRequested();

            return await Task.Run(() => ExecuteParameterizedWithReadLock(query, parameters), token);
        }

        /// <summary>
        /// Execute a non-query command.
        /// </summary>
        internal int ExecuteNonQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            _ReadWriteLock.EnterWriteLock();
            try
            {
                return _ConnectionPool.UseConnection(connection =>
                {
                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        return cmd.ExecuteNonQuery();
                    }
                });
            }
            finally
            {
                _ReadWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Execute a non-query command asynchronously.
        /// </summary>
        internal async Task<int> ExecuteNonQueryAsync(string query, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            token.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                _ReadWriteLock.EnterWriteLock();
                try
                {
                    return _ConnectionPool.UseConnection(connection =>
                    {
                        using (SqliteCommand cmd = new SqliteCommand(query, connection))
                        {
                            return cmd.ExecuteNonQuery();
                        }
                    }, token);
                }
                finally
                {
                    _ReadWriteLock.ExitWriteLock();
                }
            }, token);
        }

        /// <summary>
        /// Execute a parameterized non-query command.
        /// </summary>
        internal int ExecuteParameterizedNonQuery(string query, params SqliteParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            _ReadWriteLock.EnterWriteLock();
            try
            {
                return _ConnectionPool.UseConnection(connection =>
                {
                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }
                        return cmd.ExecuteNonQuery();
                    }
                });
            }
            finally
            {
                _ReadWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Execute a parameterized non-query command asynchronously.
        /// </summary>
        internal async Task<int> ExecuteParameterizedNonQueryAsync(string query, CancellationToken token = default, params SqliteParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            token.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                _ReadWriteLock.EnterWriteLock();
                try
                {
                    return _ConnectionPool.UseConnection(connection =>
                    {
                        using (SqliteCommand cmd = new SqliteCommand(query, connection))
                        {
                            if (parameters != null)
                            {
                                cmd.Parameters.AddRange(parameters);
                            }
                            return cmd.ExecuteNonQuery();
                        }
                    }, token);
                }
                finally
                {
                    _ReadWriteLock.ExitWriteLock();
                }
            }, token);
        }

        #endregion

        #region Private-Methods

        private DataTable ExecuteWithReadLock(string query)
        {
            _ReadWriteLock.EnterReadLock();
            try
            {
                return _ConnectionPool.UseConnection(connection =>
                {
                    DataTable result = new DataTable();
                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            result.Load(reader);
                        }
                    }
                    return result;
                });
            }
            finally
            {
                _ReadWriteLock.ExitReadLock();
            }
        }

        private DataTable ExecuteWithWriteLock(string query)
        {
            _ReadWriteLock.EnterWriteLock();
            try
            {
                return _ConnectionPool.UseConnection(connection =>
                {
                    DataTable result = new DataTable();
                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            result.Load(reader);
                        }
                    }
                    return result;
                });
            }
            finally
            {
                _ReadWriteLock.ExitWriteLock();
            }
        }

        private DataTable ExecuteParameterizedWithReadLock(string query, SqliteParameter[] parameters)
        {
            _ReadWriteLock.EnterReadLock();
            try
            {
                return _ConnectionPool.UseConnection(connection =>
                {
                    DataTable result = new DataTable();
                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            result.Load(reader);
                        }
                    }
                    return result;
                });
            }
            finally
            {
                _ReadWriteLock.ExitReadLock();
            }
        }

        #endregion
    }
}
