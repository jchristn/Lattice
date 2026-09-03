namespace Lattice.Sdk.Methods
{
    using Lattice.Sdk.Models;

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

            return await _client.RequestJsonAsync<Collection>("PUT", "/v1.0/collections", data, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<Collection>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            List<Collection>? collections = await _client.RequestJsonAsync<List<Collection>>("GET", "/v1.0/collections", cancellationToken: cancellationToken).ConfigureAwait(false);
            return collections ?? new List<Collection>();
        }

        public async Task<Collection?> ReadByIdAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestJsonAsync<Collection>("GET", $"/v1.0/collections/{collectionId}", nullOnNotFound: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestStatusAsync("HEAD", $"/v1.0/collections/{collectionId}", throwOnError: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DeleteAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestStatusAsync("DELETE", $"/v1.0/collections/{collectionId}", cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<ConstraintsResponse?> GetConstraintsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            return await _client.RequestJsonAsync<ConstraintsResponse>("GET", $"/v1.0/collections/{collectionId}/constraints", nullOnNotFound: true, cancellationToken: cancellationToken).ConfigureAwait(false);
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

            return await _client.RequestStatusAsync("PUT", $"/v1.0/collections/{collectionId}/constraints", data, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<IndexedField>> GetIndexedFieldsAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            IndexingConfiguration? config = await _client.RequestJsonAsync<IndexingConfiguration>("GET", $"/v1.0/collections/{collectionId}/indexing", cancellationToken: cancellationToken).ConfigureAwait(false);
            return config?.IndexedFields ?? new List<IndexedField>();
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

            return await _client.RequestStatusAsync("PUT", $"/v1.0/collections/{collectionId}/indexing", data, cancellationToken: cancellationToken).ConfigureAwait(false);
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

            return await _client.RequestJsonAsync<IndexRebuildResult>("POST", $"/v1.0/collections/{collectionId}/indexes/rebuild", data, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
