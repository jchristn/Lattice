namespace Lattice.Core.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Client.Interfaces;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;
    using Lattice.Core.Telemetry;

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
            using OperationScope op = LatticeTelemetry.StartOperation("index.get_mappings", null);
            try
            {
                List<IndexTableMapping> mappings = new List<IndexTableMapping>();
                await foreach (IndexTableMapping mapping in _Repo.Indexes.GetAllMappings(token))
                {
                    mappings.Add(mapping);
                }
                return mappings;
            }
            catch (Exception e)
            {
                op.Fail(e);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IndexTableMapping> GetMappingByKey(string key, CancellationToken token = default)
        {
            using OperationScope op = LatticeTelemetry.StartOperation("index.get_mapping", null);
            try
            {
                return await _Repo.Indexes.GetMappingByKey(key, token);
            }
            catch (Exception e)
            {
                op.Fail(e);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<IndexTableEntry>> GetTableEntries(string tableName, int skip = 0, int limit = 100, CancellationToken token = default)
        {
            using OperationScope op = LatticeTelemetry.StartOperation("index.get_entries", null);
            try
            {
                return await _Repo.Indexes.GetTableEntries(tableName, skip, limit, token);
            }
            catch (Exception e)
            {
                op.Fail(e);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<long> GetTableEntryCount(string tableName, CancellationToken token = default)
        {
            using OperationScope op = LatticeTelemetry.StartOperation("index.count_entries", null);
            try
            {
                return await _Repo.Indexes.GetTableEntryCount(tableName, token);
            }
            catch (Exception e)
            {
                op.Fail(e);
                throw;
            }
        }

        #endregion
    }
}
