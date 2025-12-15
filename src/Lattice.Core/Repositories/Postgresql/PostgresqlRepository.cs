namespace Lattice.Core.Repositories.Postgresql
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;
    using Lattice.Core.Repositories.Interfaces;
    using Lattice.Core.Repositories.Postgresql.Implementations;
    using Lattice.Core.Repositories.Postgresql.Queries;

    /// <summary>
    /// PostgreSQL implementation of the repository.
    /// </summary>
    public class PostgresqlRepository : RepositoryBase
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
        public override IIndexMethods Indexes { get; }

        /// <inheritdoc />
        public override IFieldConstraintMethods FieldConstraints { get; }

        /// <inheritdoc />
        public override IIndexedFieldMethods IndexedFields { get; }

        /// <inheritdoc />
        public override IObjectLockMethods ObjectLocks { get; }

        #endregion

        #region Private-Members

        private readonly string _ConnectionString;
        private readonly ReaderWriterLockSlim _ReadWriteLock;
        private bool _Disposed = false;

        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the PostgreSQL repository.
        /// </summary>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        public PostgresqlRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _ConnectionString = connectionString;
            _ReadWriteLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            // Initialize method implementations
            Collections = new CollectionMethods(this);
            Documents = new DocumentMethods(this);
            Schemas = new SchemaMethods(this);
            SchemaElements = new SchemaElementMethods(this);
            Values = new ValueMethods(this);
            Labels = new LabelMethods(this);
            Tags = new TagMethods(this);
            Indexes = new IndexMethods(this);
            FieldConstraints = new FieldConstraintMethods(this);
            IndexedFields = new IndexedFieldMethods(this);
            ObjectLocks = new ObjectLockMethods(this);
        }

        /// <summary>
        /// Instantiate the PostgreSQL repository with individual connection parameters.
        /// </summary>
        /// <param name="host">Server hostname or IP.</param>
        /// <param name="database">Database name.</param>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="port">Port number (default 5432).</param>
        public PostgresqlRepository(string host, string database, string username, string password, int port = 5432)
            : this($"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;")
        {
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            // PostgreSQL supports CREATE TABLE/INDEX IF NOT EXISTS
            string[] statements = SetupQueries.CreateTablesAndIndices().Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string statement in statements)
            {
                string trimmed = statement.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    try
                    {
                        ExecuteNonQuery(trimmed + ";");
                    }
                    catch (PostgresException ex) when (ex.SqlState == "42P07" || ex.SqlState == "42710")
                    {
                        // Table or index already exists - ignore
                    }
                }
            }

            // Run migrations for existing databases
            foreach (string migration in SetupQueries.GetMigrationStatements())
            {
                try
                {
                    ExecuteNonQuery(migration);
                }
                catch (PostgresException ex) when (ex.SqlState == "42701")
                {
                    // Column already exists - ignore
                }
            }
        }

        /// <inheritdoc />
        public override void Flush()
        {
            // PostgreSQL is not an in-memory database, so Flush is a no-op
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (_Disposed) return;

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

            if (isTransaction)
            {
                return ExecuteWithWriteLock(query, useTransaction: true);
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

            if (isTransaction)
            {
                return await Task.Run(() => ExecuteWithWriteLock(query, useTransaction: true), token);
            }

            return await Task.Run(() => ExecuteWithReadLock(query), token);
        }

        /// <summary>
        /// Execute a parameterized query.
        /// </summary>
        internal DataTable ExecuteParameterizedQuery(string query, params NpgsqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            return ExecuteParameterizedWithReadLock(query, parameters);
        }

        /// <summary>
        /// Execute a parameterized query asynchronously.
        /// </summary>
        internal async Task<DataTable> ExecuteParameterizedQueryAsync(string query, CancellationToken token = default, params NpgsqlParameter[] parameters)
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
                using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                    {
                        return cmd.ExecuteNonQuery();
                    }
                }
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
                    using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                    {
                        connection.Open();
                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                        {
                            return cmd.ExecuteNonQuery();
                        }
                    }
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
        internal int ExecuteParameterizedNonQuery(string query, params NpgsqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            _ReadWriteLock.EnterWriteLock();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                _ReadWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Execute a parameterized non-query command asynchronously.
        /// </summary>
        internal async Task<int> ExecuteParameterizedNonQueryAsync(string query, CancellationToken token = default, params NpgsqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            token.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                _ReadWriteLock.EnterWriteLock();
                try
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                    {
                        connection.Open();
                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                        {
                            if (parameters != null)
                            {
                                cmd.Parameters.AddRange(parameters);
                            }
                            return cmd.ExecuteNonQuery();
                        }
                    }
                }
                finally
                {
                    _ReadWriteLock.ExitWriteLock();
                }
            }, token);
        }

        /// <summary>
        /// Execute multiple statements in a transaction.
        /// </summary>
        internal async Task ExecuteTransactionAsync(string[] statements, CancellationToken token = default)
        {
            if (statements == null || statements.Length == 0)
                return;

            token.ThrowIfCancellationRequested();

            _ReadWriteLock.EnterWriteLock();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                {
                    await connection.OpenAsync(token);
                    using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token))
                    {
                        try
                        {
                            foreach (string statement in statements)
                            {
                                if (!string.IsNullOrWhiteSpace(statement))
                                {
                                    using (NpgsqlCommand cmd = new NpgsqlCommand(statement, connection, transaction))
                                    {
                                        await cmd.ExecuteNonQueryAsync(token);
                                    }
                                }
                            }
                            await transaction.CommitAsync(token);
                        }
                        catch
                        {
                            await transaction.RollbackAsync(token);
                            throw;
                        }
                    }
                }
            }
            finally
            {
                _ReadWriteLock.ExitWriteLock();
            }
        }

        #endregion

        #region Private-Methods

        private DataTable ExecuteWithReadLock(string query)
        {
            _ReadWriteLock.EnterReadLock();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            return LoadDataTableWithoutConstraints(reader);
                        }
                    }
                }
            }
            finally
            {
                _ReadWriteLock.ExitReadLock();
            }
        }

        private DataTable ExecuteWithWriteLock(string query, bool useTransaction = false)
        {
            _ReadWriteLock.EnterWriteLock();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                {
                    connection.Open();

                    if (useTransaction)
                    {
                        using (NpgsqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection, transaction))
                                {
                                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                                    {
                                        DataTable result = LoadDataTableWithoutConstraints(reader);
                                        reader.Close();
                                        transaction.Commit();
                                        return result;
                                    }
                                }
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                    else
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                        {
                            using (NpgsqlDataReader reader = cmd.ExecuteReader())
                            {
                                return LoadDataTableWithoutConstraints(reader);
                            }
                        }
                    }
                }
            }
            finally
            {
                _ReadWriteLock.ExitWriteLock();
            }
        }

        private DataTable ExecuteParameterizedWithReadLock(string query, NpgsqlParameter[] parameters)
        {
            _ReadWriteLock.EnterReadLock();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            return LoadDataTableWithoutConstraints(reader);
                        }
                    }
                }
            }
            finally
            {
                _ReadWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Load data from a NpgsqlDataReader into a DataTable without inferring constraints.
        /// </summary>
        private static DataTable LoadDataTableWithoutConstraints(NpgsqlDataReader reader)
        {
            DataTable result = new DataTable();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                Type columnType = reader.GetFieldType(i);
                DataColumn column = new DataColumn(columnName, columnType);
                column.AllowDBNull = true;
                result.Columns.Add(column);
            }

            while (reader.Read())
            {
                DataRow row = result.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                }
                result.Rows.Add(row);
            }

            return result;
        }

        #endregion
    }
}
