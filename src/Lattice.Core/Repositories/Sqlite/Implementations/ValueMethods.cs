namespace Lattice.Core.Repositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// SQLite implementation of document value methods.
    /// Note: This is a stub implementation. Actual values are stored in dynamic index tables.
    /// </summary>
    internal class ValueMethods : IValueMethods
    {
        private readonly SqliteRepository _Repo;

        internal ValueMethods(SqliteRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public Task<DocumentValue> Create(DocumentValue value, CancellationToken token = default)
        {
            // Values are stored in dynamic index tables, not a central values table
            // This method is a placeholder for the interface
            throw new NotSupportedException("Values are stored in dynamic index tables. Use IIndexMethods.InsertValue instead.");
        }

        public Task<List<DocumentValue>> CreateMany(List<DocumentValue> values, CancellationToken token = default)
        {
            // Values are stored in dynamic index tables, not a central values table
            throw new NotSupportedException("Values are stored in dynamic index tables. Use IIndexMethods.InsertValues instead.");
        }

        public async IAsyncEnumerable<DocumentValue> ReadByDocumentId(string documentId, [EnumeratorCancellation] CancellationToken token = default)
        {
            // This would need to query all index tables for the document
            // For now, return empty as values are distributed across index tables
            await Task.CompletedTask;
            yield break;
        }

        public Task DeleteByDocumentId(string documentId, CancellationToken token = default)
        {
            // Values are deleted from index tables
            // This method is a placeholder for the interface
            throw new NotSupportedException("Values are stored in dynamic index tables. Delete from each index table individually.");
        }
    }
}
