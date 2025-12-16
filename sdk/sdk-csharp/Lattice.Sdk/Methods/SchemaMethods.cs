using System.Text.Json;
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
            ResponseContext response = await _client.RequestAsync("GET", "/v1.0/schemas", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                List<Schema>? schemas = JsonSerializer.Deserialize<List<Schema>>(response.Data.Value.GetRawText(), _client.JsonOptions);
                return schemas ?? new List<Schema>();
            }
            return new List<Schema>();
        }

        public async Task<Schema?> ReadByIdAsync(string schemaId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("GET", $"/v1.0/schemas/{schemaId}", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<Schema>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }

        public async Task<List<SchemaElement>> GetElementsAsync(string schemaId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("GET", $"/v1.0/schemas/{schemaId}/elements", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                List<SchemaElement>? elements = JsonSerializer.Deserialize<List<SchemaElement>>(response.Data.Value.GetRawText(), _client.JsonOptions);
                return elements ?? new List<SchemaElement>();
            }
            return new List<SchemaElement>();
        }
    }
}
