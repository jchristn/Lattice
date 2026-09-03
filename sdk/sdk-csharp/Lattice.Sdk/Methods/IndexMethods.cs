namespace Lattice.Sdk.Methods
{
    using Lattice.Sdk.Models;

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
            List<IndexTableMapping>? mappings = await _client.RequestJsonAsync<List<IndexTableMapping>>("GET", "/v1.0/tables", cancellationToken: cancellationToken).ConfigureAwait(false);
            return mappings ?? new List<IndexTableMapping>();
        }
    }
}
