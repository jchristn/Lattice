namespace Lattice.Sdk.Methods
{
    using System.Text.Json;
    using Lattice.Sdk.Models;

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

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["content"] = JsonSerializer.Deserialize<object>(contentJson)!
            };

            if (!string.IsNullOrEmpty(name))
                data["name"] = name;
            if (labels != null && labels.Count > 0)
                data["labels"] = labels;
            if (tags != null && tags.Count > 0)
                data["tags"] = tags;

            return await _client.RequestJsonAsync<Document>("PUT", $"/v1.0/collections/{collectionId}/documents", data, cancellationToken: cancellationToken);
        }

        public async Task<List<Document>?> IngestBatchAsync(
            string collectionId,
            List<BatchIngestDocument> documents,
            CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions contentOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            List<object> docEntries = new List<object>();
            foreach (BatchIngestDocument doc in documents)
            {
                string contentJson = JsonSerializer.Serialize(doc.Content, contentOptions);

                Dictionary<string, object> entry = new Dictionary<string, object>
                {
                    ["content"] = JsonSerializer.Deserialize<object>(contentJson)!
                };

                if (!string.IsNullOrEmpty(doc.Name))
                    entry["name"] = doc.Name;
                if (doc.Labels != null && doc.Labels.Count > 0)
                    entry["labels"] = doc.Labels;
                if (doc.Tags != null && doc.Tags.Count > 0)
                    entry["tags"] = doc.Tags;

                docEntries.Add(entry);
            }

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["documents"] = docEntries
            };

            return await _client.RequestJsonAsync<List<Document>>("PUT", $"/v1.0/collections/{collectionId}/documents/batch", data, cancellationToken: cancellationToken);
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

            List<Document>? documents = await _client.RequestJsonAsync<List<Document>>("GET", $"/v1.0/collections/{collectionId}/documents", queryParams: queryParams, cancellationToken: cancellationToken);
            return documents ?? new List<Document>();
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

            Document? document = await _client.RequestJsonAsync<Document>("GET", $"/v1.0/collections/{collectionId}/documents/{documentId}", queryParams: queryParams, nullOnNotFound: true, cancellationToken: cancellationToken);

            if (document == null)
            {
                return null;
            }

            // If content is requested, make a separate call to get the raw content
            if (includeContent)
            {
                string? content = await _client.RequestRawContentAsync("GET", $"/v1.0/collections/{collectionId}/documents/{documentId}?includeContent=true", cancellationToken);
                if (content != null)
                {
                    document.Content = content;
                }
            }

            return document;
        }

        public async Task<bool> ExistsAsync(string collectionId, string documentId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestStatusAsync("HEAD", $"/v1.0/collections/{collectionId}/documents/{documentId}", throwOnError: false, cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteAsync(string collectionId, string documentId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestStatusAsync("DELETE", $"/v1.0/collections/{collectionId}/documents/{documentId}", cancellationToken: cancellationToken);
        }
    }
}
