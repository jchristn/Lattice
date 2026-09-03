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

        public async Task<EnumerationResult<IndexTableMapping>?> GetMappingsAsync(
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (maxResults != null)
                queryParams["maxResults"] = maxResults.Value.ToString();
            if (skip != null)
                queryParams["skip"] = skip.Value.ToString();

            return await _client.RequestJsonAsync<EnumerationResult<IndexTableMapping>>("GET", "/v1.0/tables", queryParams: queryParams, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<EnumerationResult<IndexTableEntry>?> GetTableEntriesAsync(
            string tableName,
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (maxResults != null)
                queryParams["maxResults"] = maxResults.Value.ToString();
            if (skip != null)
                queryParams["skip"] = skip.Value.ToString();

            return await _client.RequestJsonAsync<EnumerationResult<IndexTableEntry>>("GET", $"/v1.0/tables/{tableName}/entries", queryParams: queryParams, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
