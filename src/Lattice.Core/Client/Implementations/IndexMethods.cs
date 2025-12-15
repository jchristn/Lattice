namespace Lattice.Core.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Client.Interfaces;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;

    /// <summary>
    /// Index methods implementation.
    /// </summary>
    public class IndexMethods : IIndexMethods
    {
        #region Private-Members

        private readonly LatticeClient _Client;
        private readonly RepositoryBase _Repo;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate index methods.
        /// </summary>
        /// <param name="client">Lattice client.</param>
        /// <param name="repo">Repository.</param>
        public IndexMethods(
            LatticeClient client,
            RepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<List<IndexTableMapping>> GetMappings(CancellationToken token = default)
        {
            List<IndexTableMapping> mappings = new List<IndexTableMapping>();
            await foreach (IndexTableMapping mapping in _Repo.Indexes.GetAllMappings(token))
            {
                mappings.Add(mapping);
            }
            return mappings;
        }

        /// <inheritdoc />
        public Task<IndexTableMapping> GetMappingByKey(string key, CancellationToken token = default)
        {
            return _Repo.Indexes.GetMappingByKey(key, token);
        }

        #endregion
    }
}
