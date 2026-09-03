namespace Lattice.Sdk.Methods
{
    using Lattice.Sdk.Models;

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

        public async Task<EnumerationResult<Schema>?> ReadAllAsync(
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (maxResults != null)
                queryParams["maxResults"] = maxResults.Value.ToString();
            if (skip != null)
                queryParams["skip"] = skip.Value.ToString();

            return await _client.RequestJsonAsync<EnumerationResult<Schema>>("GET", "/v1.0/schemas", queryParams: queryParams, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<Schema?> ReadByIdAsync(string schemaId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestJsonAsync<Schema>("GET", $"/v1.0/schemas/{schemaId}", nullOnNotFound: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<EnumerationResult<SchemaElement>?> GetElementsAsync(
            string schemaId,
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (maxResults != null)
                queryParams["maxResults"] = maxResults.Value.ToString();
            if (skip != null)
                queryParams["skip"] = skip.Value.ToString();

            return await _client.RequestJsonAsync<EnumerationResult<SchemaElement>>("GET", $"/v1.0/schemas/{schemaId}/elements", queryParams: queryParams, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
