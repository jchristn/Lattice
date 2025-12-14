namespace Lattice.Core.Repositories.Sqlite
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// A thread-safe connection pool for SQLite connections.
    /// </summary>
    internal sealed class SqliteConnectionPool : IDisposable
    {
        #region Private-Members

        private readonly string _ConnectionString;
        private readonly int _MaxPoolSize;
        private readonly ConcurrentBag<SqliteConnection> _AvailableConnections;
        private readonly SemaphoreSlim _PoolSemaphore;
        private readonly object _CreationLock = new object();
        private int _CreatedCount;
        private bool _Disposed;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the connection pool.
        /// </summary>
        /// <param name="connectionString">The connection string for creating connections.</param>
        /// <param name="maxPoolSize">Maximum number of connections in the pool.</param>
        public SqliteConnectionPool(string connectionString, int maxPoolSize)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (maxPoolSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "Pool size must be at least 1.");

            _ConnectionString = connectionString;
            _MaxPoolSize = maxPoolSize;
            _AvailableConnections = new ConcurrentBag<SqliteConnection>();
            _PoolSemaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
            _CreatedCount = 0;
            _Disposed = false;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Acquire a connection from the pool. The connection must be returned via ReturnConnection.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>An open SQLite connection.</returns>
        public SqliteConnection AcquireConnection(CancellationToken token = default)
        {
            ThrowIfDisposed();

            _PoolSemaphore.Wait(token);

            try
            {
                if (_AvailableConnections.TryTake(out SqliteConnection connection))
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        return connection;

                    // Connection was closed unexpectedly, dispose and create new
                    connection.Dispose();
                    Interlocked.Decrement(ref _CreatedCount);
                }

                // Create a new connection if we haven't reached the limit
                return CreateConnection();
            }
            catch
            {
                _PoolSemaphore.Release();
                throw;
            }
        }

        /// <summary>
        /// Return a connection to the pool.
        /// </summary>
        /// <param name="connection">The connection to return.</param>
        public void ReturnConnection(SqliteConnection connection)
        {
            if (connection == null)
                return;

            if (_Disposed)
            {
                // Pool is disposed, just close the connection
                connection.Close();
                connection.Dispose();
                return;
            }

            if (connection.State == System.Data.ConnectionState.Open)
            {
                _AvailableConnections.Add(connection);
            }
            else
            {
                // Connection is no longer valid, dispose it
                connection.Dispose();
                Interlocked.Decrement(ref _CreatedCount);
            }

            _PoolSemaphore.Release();
        }

        /// <summary>
        /// Execute an action with a pooled connection, ensuring the connection is returned.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="action">The action to execute with the connection.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The result of the action.</returns>
        public T UseConnection<T>(Func<SqliteConnection, T> action, CancellationToken token = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            SqliteConnection connection = AcquireConnection(token);
            try
            {
                return action(connection);
            }
            finally
            {
                ReturnConnection(connection);
            }
        }

        /// <summary>
        /// Execute an action with a pooled connection, ensuring the connection is returned.
        /// </summary>
        /// <param name="action">The action to execute with the connection.</param>
        /// <param name="token">Cancellation token.</param>
        public void UseConnection(Action<SqliteConnection> action, CancellationToken token = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            SqliteConnection connection = AcquireConnection(token);
            try
            {
                action(connection);
            }
            finally
            {
                ReturnConnection(connection);
            }
        }

        /// <summary>
        /// Dispose all connections in the pool.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed)
                return;

            _Disposed = true;

            // Dispose all available connections
            while (_AvailableConnections.TryTake(out SqliteConnection connection))
            {
                connection.Close();
                connection.Dispose();
            }

            _PoolSemaphore.Dispose();
        }

        #endregion

        #region Private-Methods

        private SqliteConnection CreateConnection()
        {
            lock (_CreationLock)
            {
                SqliteConnection connection = new SqliteConnection(_ConnectionString);
                connection.Open();
                Interlocked.Increment(ref _CreatedCount);
                return connection;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed)
                throw new ObjectDisposedException(nameof(SqliteConnectionPool));
        }

        #endregion
    }
}
