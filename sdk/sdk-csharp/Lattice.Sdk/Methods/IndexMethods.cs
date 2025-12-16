using System.Text.Json;
using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Implementation of index management methods.
    /// </summary>
    internal class IndexMethods : IIndexMethods
    {
        private readonly LatticeClient _client;

        public IndexMethods(LatticeClient client)
        {
            _client = client;
        }

        public async Task<List<IndexTableMapping>> GetMappingsAsync(CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("GET", "/v1.0/tables", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                List<IndexTableMapping>? mappings = JsonSerializer.Deserialize<List<IndexTableMapping>>(response.Data.Value.GetRawText(), _client.JsonOptions);
                return mappings ?? new List<IndexTableMapping>();
            }
            return new List<IndexTableMapping>();
        }
    }
}
