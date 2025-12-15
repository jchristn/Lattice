namespace Lattice.Core.Repositories.SqlServer
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.SqlClient;
    using Lattice.Core.Repositories.Interfaces;
    using Lattice.Core.Repositories.SqlServer.Implementations;
    using Lattice.Core.Repositories.SqlServer.Queries;

    /// <summary>
    /// SQL Server implementation of the repository.
    /// </summary>
    public class SqlServerRepository : RepositoryBase
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

        #endregion

        #region Private-Members

        private readonly string _ConnectionString;
        private readonly ReaderWriterLockSlim _ReadWriteLock;
        private bool _Disposed = false;

        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the SQL Server repository.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        public SqlServerRepository(string connectionString)
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
        }

        /// <summary>
        /// Instantiate the SQL Server repository with individual connection parameters.
        /// </summary>
        /// <param name="server">Server hostname or IP.</param>
        /// <param name="database">Database name.</param>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="trustServerCertificate">Trust server certificate (default true for development).</param>
        public SqlServerRepository(string server, string database, string username, string password, bool trustServerCertificate = true)
            : this($"Server={server};Database={database};User Id={username};Password={password};TrustServerCertificate={trustServerCertificate};")
        {
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            // SQL Server uses batched statements with GO separator
            // We'll execute each statement individually
            string[] statements = SetupQueries.CreateTablesAndIndices()
                .Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string statement in statements)
            {
                string trimmed = statement.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    try
                    {
                        ExecuteNonQuery(trimmed);
                    }
                    catch (SqlException)
                    {
                        // Table or index might already exist - ignore
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
                catch (SqlException)
                {
                    // Column might already exist - ignore
                }
            }
        }

        /// <inheritdoc />
        public override void Flush()
        {
            // SQL Server is not an in-memory database, so Flush is a no-op
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
        internal DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            return ExecuteParameterizedWithReadLock(query, parameters);
        }

        /// <summary>
        /// Execute a parameterized query asynchronously.
        /// </summary>
        internal async Task<DataTable> ExecuteParameterizedQueryAsync(string query, CancellationToken token = default, params SqlParameter[] parameters)
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
                using (SqlConnection connection = new SqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(query, connection))
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
                    using (SqlConnection connection = new SqlConnection(_ConnectionString))
                    {
                        connection.Open();
                        using (SqlCommand cmd = new SqlCommand(query, connection))
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
        internal int ExecuteParameterizedNonQuery(string query, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            _ReadWriteLock.EnterWriteLock();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(query, connection))
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
        internal async Task<int> ExecuteParameterizedNonQueryAsync(string query, CancellationToken token = default, params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            token.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                _ReadWriteLock.EnterWriteLock();
                try
                {
                    using (SqlConnection connection = new SqlConnection(_ConnectionString))
                    {
                        connection.Open();
                        using (SqlCommand cmd = new SqlCommand(query, connection))
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
                using (SqlConnection connection = new SqlConnection(_ConnectionString))
                {
                    await connection.OpenAsync(token);
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (string statement in statements)
                            {
                                if (!string.IsNullOrWhiteSpace(statement))
                                {
                                    using (SqlCommand cmd = new SqlCommand(statement, connection, transaction))
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
                using (SqlConnection connection = new SqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
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
                using (SqlConnection connection = new SqlConnection(_ConnectionString))
                {
                    connection.Open();

                    if (useTransaction)
                    {
                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                using (SqlCommand cmd = new SqlCommand(query, connection, transaction))
                                {
                                    using (SqlDataReader reader = cmd.ExecuteReader())
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
                        using (SqlCommand cmd = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
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

        private DataTable ExecuteParameterizedWithReadLock(string query, SqlParameter[] parameters)
        {
            _ReadWriteLock.EnterReadLock();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }
                        using (SqlDataReader reader = cmd.ExecuteReader())
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
        /// Load data from a SqlDataReader into a DataTable without inferring constraints.
        /// </summary>
        private static DataTable LoadDataTableWithoutConstraints(SqlDataReader reader)
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
