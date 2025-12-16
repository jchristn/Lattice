using System.Text.Json;
using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
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

            ResponseContext response = await _client.RequestAsync(
                "POST",
                $"/v1.0/collections/{query.CollectionId}/documents/search",
                data,
                cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<SearchResult>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }

        public async Task<SearchResult?> SearchBySqlAsync(string collectionId, string sqlExpression, CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["sqlExpression"] = sqlExpression
            };

            ResponseContext response = await _client.RequestAsync(
                "POST",
                $"/v1.0/collections/{collectionId}/documents/search",
                data,
                cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<SearchResult>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }

        public async Task<SearchResult?> EnumerateAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            return await SearchAsync(query, cancellationToken);
        }
    }
}
