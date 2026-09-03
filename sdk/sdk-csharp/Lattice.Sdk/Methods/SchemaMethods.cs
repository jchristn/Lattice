using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Implementation of schema management methods.
    /// </summary>
    internal class SchemaMethods : ISchemaMethods
    {
        private readonly LatticeClient _client;

        public SchemaMethods(LatticeClient client)
        {
            _client = client;
        }

        public async Task<List<Schema>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            List<Schema>? schemas = await _client.RequestJsonAsync<List<Schema>>("GET", "/v1.0/schemas", cancellationToken: cancellationToken);
            return schemas ?? new List<Schema>();
        }

        public async Task<Schema?> ReadByIdAsync(string schemaId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestJsonAsync<Schema>("GET", $"/v1.0/schemas/{schemaId}", nullOnNotFound: true, cancellationToken: cancellationToken);
        }

        public async Task<List<SchemaElement>> GetElementsAsync(string schemaId, CancellationToken cancellationToken = default)
        {
            List<SchemaElement>? elements = await _client.RequestJsonAsync<List<SchemaElement>>("GET", $"/v1.0/schemas/{schemaId}/elements", cancellationToken: cancellationToken);
            return elements ?? new List<SchemaElement>();
        }
    }
}
