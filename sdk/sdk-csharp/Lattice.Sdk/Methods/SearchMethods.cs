namespace Lattice.Sdk.Methods
{
    using Lattice.Sdk.Models;

    /// <summary>
    /// Implementation of search methods.
    /// </summary>
    internal class SearchMethods : ISearchMethods
    {
        private readonly LatticeClient _client;

        public SearchMethods(LatticeClient client)
        {
            _client = client;
        }

        public async Task<SearchResult?> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            if (query.Filters != null && query.Filters.Count > 0)
                data["filters"] = query.Filters;
            if (query.Labels != null && query.Labels.Count > 0)
                data["labels"] = query.Labels;
            if (query.Tags != null && query.Tags.Count > 0)
                data["tags"] = query.Tags;
            if (query.MaxResults.HasValue)
                data["maxResults"] = query.MaxResults.Value;
            if (query.Skip.HasValue)
                data["skip"] = query.Skip.Value;
            if (query.Ordering.HasValue)
                data["ordering"] = query.Ordering.Value.ToString();
            if (query.IncludeContent)
                data["includeContent"] = true;

            return await _client.RequestJsonAsync<SearchResult>(
                "POST",
                $"/v1.0/collections/{query.CollectionId}/documents/search",
                data,
                cancellationToken: cancellationToken);
        }

        public async Task<SearchResult?> SearchBySqlAsync(string collectionId, string sqlExpression, CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["sqlExpression"] = sqlExpression
            };

            return await _client.RequestJsonAsync<SearchResult>(
                "POST",
                $"/v1.0/collections/{collectionId}/documents/search",
                data,
                cancellationToken: cancellationToken);
        }

        public async Task<SearchResult?> EnumerateAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            return await SearchAsync(query, cancellationToken);
        }
    }
}
