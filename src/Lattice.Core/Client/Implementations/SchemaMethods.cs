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
    /// Schema methods implementation.
    /// </summary>
    public class SchemaMethods : ISchemaMethods
    {
        #region Private-Members

        private readonly LatticeClient _Client;
        private readonly RepositoryBase _Repo;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate schema methods.
        /// </summary>
        /// <param name="client">Lattice client.</param>
        /// <param name="repo">Repository.</param>
        public SchemaMethods(
            LatticeClient client,
            RepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<List<Schema>> ReadAll(CancellationToken token = default)
        {
            using OperationScope op = LatticeTelemetry.StartOperation("schema.read_all", null);
            try
            {
                List<Schema> schemas = new List<Schema>();
                await foreach (Schema schema in _Repo.Schemas.ReadAll(token))
                {
                    schemas.Add(schema);
                }
                return schemas;
            }
            catch (Exception e)
            {
                op.Fail(e);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Schema> ReadById(string id, CancellationToken token = default)
        {
            using OperationScope op = LatticeTelemetry.StartOperation("schema.read", null);
            try
            {
                return await _Repo.Schemas.ReadById(id, token);
            }
            catch (Exception e)
            {
                op.Fail(e);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<SchemaElement>> GetElements(string schemaId, CancellationToken token = default)
        {
            using OperationScope op = LatticeTelemetry.StartOperation("schema.get_elements", null);
            try
            {
                List<SchemaElement> elements = new List<SchemaElement>();
                await foreach (SchemaElement element in _Repo.SchemaElements.ReadBySchemaId(schemaId, token))
                {
                    elements.Add(element);
                }
                return elements;
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
