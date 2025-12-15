namespace Lattice.Core.Repositories.Sqlite.Implementations
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQLite implementation of object lock methods.
    /// </summary>
    internal class ObjectLockMethods : IObjectLockMethods
    {
        private readonly SqliteRepository _Repo;

        internal ObjectLockMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<LockAcquisitionResult> TryAcquireLock(
            string collectionId,
            string documentName,
            string hostname,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (string.IsNullOrWhiteSpace(documentName)) throw new ArgumentNullException(nameof(documentName));
            if (string.IsNullOrWhiteSpace(hostname)) throw new ArgumentNullException(nameof(hostname));
            token.ThrowIfCancellationRequested();

            // Check for existing lock
            ObjectLock existingLock = await ReadByCollectionAndDocument(collectionId, documentName, token);
            if (existingLock != null)
            {
                return LockAcquisitionResult.Blocked(existingLock);
            }

            // Create new lock
            ObjectLock newLock = new ObjectLock
            {
                Id = IdGenerator.NewObjectLockId(),
                CollectionId = collectionId,
                DocumentName = documentName,
                Hostname = hostname,
                CreatedUtc = DateTime.UtcNow
            };

            string query = $@"
                INSERT INTO objectlocks (id, collectionid, documentname, hostname, createdutc)
                VALUES ('{Sanitizer.Sanitize(newLock.Id)}',
                        '{Sanitizer.Sanitize(newLock.CollectionId)}',
                        '{Sanitizer.Sanitize(newLock.DocumentName)}',
                        '{Sanitizer.Sanitize(newLock.Hostname)}',
                        '{Converters.ToTimestamp(newLock.CreatedUtc)}');
            ";

            try
            {
                await _Repo.ExecuteNonQueryAsync(query, token);
                return LockAcquisitionResult.Acquired(newLock);
            }
            catch (Exception)
            {
                // Unique constraint violation - another process acquired the lock
                existingLock = await ReadByCollectionAndDocument(collectionId, documentName, token);
                if (existingLock != null)
                {
                    return LockAcquisitionResult.Blocked(existingLock);
                }
                throw;
            }
        }

        public async Task ReleaseLock(string lockId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(lockId)) throw new ArgumentNullException(nameof(lockId));
            token.ThrowIfCancellationRequested();

            string query = $"DELETE FROM objectlocks WHERE id = '{Sanitizer.Sanitize(lockId)}';";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task ReleaseLockByDocument(
            string collectionId,
            string documentName,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (string.IsNullOrWhiteSpace(documentName)) throw new ArgumentNullException(nameof(documentName));
            token.ThrowIfCancellationRequested();

            string query = $@"
                DELETE FROM objectlocks
                WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}'
                AND documentname = '{Sanitizer.Sanitize(documentName)}';
            ";
            await _Repo.ExecuteNonQueryAsync(query, token);
        }

        public async Task<ObjectLock> ReadByCollectionAndDocument(
            string collectionId,
            string documentName,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            if (string.IsNullOrWhiteSpace(documentName)) throw new ArgumentNullException(nameof(documentName));
            token.ThrowIfCancellationRequested();

            string query = $@"
                SELECT * FROM objectlocks
                WHERE collectionid = '{Sanitizer.Sanitize(collectionId)}'
                AND documentname = '{Sanitizer.Sanitize(documentName)}';
            ";
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token);

            if (result.Rows.Count > 0)
                return Converters.ObjectLockFromDataRow(result.Rows[0]);

            return null;
        }

        public async Task<int> DeleteExpiredLocks(int expirationSeconds, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            DateTime expirationThreshold = DateTime.UtcNow.AddSeconds(-expirationSeconds);
            string query = $@"
                DELETE FROM objectlocks
                WHERE createdutc < '{Converters.ToTimestamp(expirationThreshold)}';
            ";
            return await _Repo.ExecuteNonQueryAsync(query, token);
        }
    }
}
