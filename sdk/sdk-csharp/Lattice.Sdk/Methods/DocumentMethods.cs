using System.Text.Json;
using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Implementation of document management methods.
    /// </summary>
    internal class DocumentMethods : IDocumentMethods
    {
        private readonly LatticeClient _client;

        public DocumentMethods(LatticeClient client)
        {
            _client = client;
        }

        public async Task<Document?> IngestAsync(
            string collectionId,
            object content,
            string? name = null,
            List<string>? labels = null,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            // Serialize content without camelCase transformation to preserve user's field names
            JsonSerializerOptions contentOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            string contentJson = JsonSerializer.Serialize(content, contentOptions);
            using JsonDocument contentDoc = JsonDocument.Parse(contentJson);

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["content"] = contentDoc.RootElement.Clone()
            };

            if (!string.IsNullOrEmpty(name))
                data["name"] = name;
            if (labels != null && labels.Count > 0)
                data["labels"] = labels;
            if (tags != null && tags.Count > 0)
                data["tags"] = tags;

            ResponseContext response = await _client.RequestAsync("PUT", $"/v1.0/collections/{collectionId}/documents", data, cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<Document>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }

        public async Task<List<Document>> ReadAllInCollectionAsync(
            string collectionId,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>
            {
                ["includeContent"] = includeContent.ToString().ToLower(),
                ["includeLabels"] = includeLabels.ToString().ToLower(),
                ["includeTags"] = includeTags.ToString().ToLower()
            };

            ResponseContext response = await _client.RequestAsync("GET", $"/v1.0/collections/{collectionId}/documents", queryParams: queryParams, cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                List<Document>? documents = JsonSerializer.Deserialize<List<Document>>(response.Data.Value.GetRawText(), _client.JsonOptions);
                return documents ?? new List<Document>();
            }
            return new List<Document>();
        }

        public async Task<Document?> ReadByIdAsync(
            string collectionId,
            string documentId,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken cancellationToken = default)
        {
            // First, get the document metadata (without content)
            Dictionary<string, string> queryParams = new Dictionary<string, string>
            {
                ["includeContent"] = "false",
                ["includeLabels"] = includeLabels.ToString().ToLower(),
                ["includeTags"] = includeTags.ToString().ToLower()
            };

            ResponseContext response = await _client.RequestAsync("GET", $"/v1.0/collections/{collectionId}/documents/{documentId}", queryParams: queryParams, cancellationToken: cancellationToken);

            if (!response.Success || !response.Data.HasValue)
            {
                return null;
            }

            Document? document = JsonSerializer.Deserialize<Document>(response.Data.Value.GetRawText(), _client.JsonOptions);

            // If content is requested, make a separate call to get the raw content
            if (includeContent && document != null)
            {
                JsonElement? content = await _client.RequestRawContentAsync("GET", $"/v1.0/collections/{collectionId}/documents/{documentId}?includeContent=true", cancellationToken);
                if (content.HasValue)
                {
                    document.Content = content.Value;
                }
            }

            return document;
        }

        public async Task<bool> ExistsAsync(string collectionId, string documentId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("HEAD", $"/v1.0/collections/{collectionId}/documents/{documentId}", cancellationToken: cancellationToken);
            return response.Success;
        }

        public async Task<bool> DeleteAsync(string collectionId, string documentId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("DELETE", $"/v1.0/collections/{collectionId}/documents/{documentId}", cancellationToken: cancellationToken);
            return response.Success;
        }
    }
}
