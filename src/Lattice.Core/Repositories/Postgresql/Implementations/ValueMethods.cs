namespace Lattice.Core.Repositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    internal class ValueMethods : IValueMethods
    {
        private readonly PostgresqlRepository _Repo;

        internal ValueMethods(PostgresqlRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public Task<DocumentValue> Create(DocumentValue value, CancellationToken token = default)
        {
            throw new NotSupportedException("Values are stored in dynamic index tables. Use IIndexMethods.InsertValue instead.");
        }

        public Task<List<DocumentValue>> CreateMany(List<DocumentValue> values, CancellationToken token = default)
        {
            throw new NotSupportedException("Values are stored in dynamic index tables. Use IIndexMethods.InsertValues instead.");
        }

        public async IAsyncEnumerable<DocumentValue> ReadByDocumentId(string documentId, [EnumeratorCancellation] CancellationToken token = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task DeleteByDocumentId(string documentId, CancellationToken token = default)
        {
            throw new NotSupportedException("Values are stored in dynamic index tables. Delete from each index table individually.");
        }
    }
}
