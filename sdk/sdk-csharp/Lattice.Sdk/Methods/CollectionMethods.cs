using System.Text.Json;
using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Implementation of collection management methods.
    /// </summary>
    internal class CollectionMethods : ICollectionMethods
    {
        private readonly LatticeClient _client;

        public CollectionMethods(LatticeClient client)
        {
            _client = client;
        }

        public async Task<Collection?> CreateAsync(
            string name,
            string? description = null,
            string? documentsDirectory = null,
            List<string>? labels = null,
            Dictionary<string, string>? tags = null,
            SchemaEnforcementMode schemaEnforcementMode = SchemaEnforcementMode.None,
            List<FieldConstraint>? fieldConstraints = null,
            IndexingMode indexingMode = IndexingMode.All,
            List<string>? indexedFields = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["name"] = name
            };

            if (!string.IsNullOrEmpty(description))
                data["description"] = description;
            if (!string.IsNullOrEmpty(documentsDirectory))
                data["documentsDirectory"] = documentsDirectory;
            if (labels != null && labels.Count > 0)
                data["labels"] = labels;
            if (tags != null && tags.Count > 0)
                data["tags"] = tags;
            if (schemaEnforcementMode != SchemaEnforcementMode.None)
                data["schemaEnforcementMode"] = (int)schemaEnforcementMode;
            if (fieldConstraints != null && fieldConstraints.Count > 0)
                data["fieldConstraints"] = fieldConstraints;
            if (indexingMode != IndexingMode.All)
                data["indexingMode"] = (int)indexingMode;
            if (indexedFields != null && indexedFields.Count > 0)
                data["indexedFields"] = indexedFields;

            ResponseContext response = await _client.RequestAsync("PUT", "/v1.0/collections", data, cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<Collection>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }

        public async Task<List<Collection>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("GET", "/v1.0/collections", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                List<Collection>? collections = JsonSerializer.Deserialize<List<Collection>>(response.Data.Value.GetRawText(), _client.JsonOptions);
                return collections ?? new List<Collection>();
            }
            return new List<Collection>();
        }

        public async Task<Collection?> ReadByIdAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("GET", $"/v1.0/collections/{collectionId}", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<Collection>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }

        public async Task<bool> ExistsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("HEAD", $"/v1.0/collections/{collectionId}", cancellationToken: cancellationToken);
            return response.Success;
        }

        public async Task<bool> DeleteAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("DELETE", $"/v1.0/collections/{collectionId}", cancellationToken: cancellationToken);
            return response.Success;
        }

        public async Task<ConstraintsResponse?> GetConstraintsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("GET", $"/v1.0/collections/{collectionId}/constraints", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<ConstraintsResponse>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }

        public async Task<bool> UpdateConstraintsAsync(
            string collectionId,
            SchemaEnforcementMode schemaEnforcementMode,
            List<FieldConstraint>? fieldConstraints = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["schemaEnforcementMode"] = (int)schemaEnforcementMode
            };

            if (fieldConstraints != null && fieldConstraints.Count > 0)
                data["fieldConstraints"] = fieldConstraints;

            ResponseContext response = await _client.RequestAsync("PUT", $"/v1.0/collections/{collectionId}/constraints", data, cancellationToken: cancellationToken);
            return response.Success;
        }

        public async Task<List<IndexedField>> GetIndexedFieldsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            ResponseContext response = await _client.RequestAsync("GET", $"/v1.0/collections/{collectionId}/indexing", cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                IndexingConfiguration? config = JsonSerializer.Deserialize<IndexingConfiguration>(response.Data.Value.GetRawText(), _client.JsonOptions);
                return config?.IndexedFields ?? new List<IndexedField>();
            }
            return new List<IndexedField>();
        }

        public async Task<bool> UpdateIndexingAsync(
            string collectionId,
            IndexingMode indexingMode,
            List<string>? indexedFields = null,
            bool rebuildIndexes = false,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["indexingMode"] = (int)indexingMode,
                ["rebuildIndexes"] = rebuildIndexes
            };

            if (indexedFields != null && indexedFields.Count > 0)
                data["indexedFields"] = indexedFields;

            ResponseContext response = await _client.RequestAsync("PUT", $"/v1.0/collections/{collectionId}/indexing", data, cancellationToken: cancellationToken);
            return response.Success;
        }

        public async Task<IndexRebuildResult?> RebuildIndexesAsync(
            string collectionId,
            bool dropUnusedIndexes = true,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["dropUnusedIndexes"] = dropUnusedIndexes
            };

            ResponseContext response = await _client.RequestAsync("POST", $"/v1.0/collections/{collectionId}/indexes/rebuild", data, cancellationToken: cancellationToken);

            if (response.Success && response.Data.HasValue)
            {
                return JsonSerializer.Deserialize<IndexRebuildResult>(response.Data.Value.GetRawText(), _client.JsonOptions);
            }
            return null;
        }
    }
}
