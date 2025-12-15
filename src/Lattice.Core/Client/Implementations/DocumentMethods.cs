namespace Lattice.Core.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Client.Interfaces;
    using Lattice.Core.Exceptions;
    using Lattice.Core.Flattening;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;
    using Lattice.Core.Schema;
    using Lattice.Core.Validation;

    /// <summary>
    /// Document methods implementation.
    /// </summary>
    public class DocumentMethods : IDocumentMethods
    {
        #region Private-Members

        private readonly LatticeClient _Client;
        private readonly RepositoryBase _Repo;
        private readonly LatticeSettings _Settings;
        private readonly ISchemaGenerator _SchemaGenerator;
        private readonly IJsonFlattener _JsonFlattener;
        private readonly ISchemaValidator _SchemaValidator;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate document methods.
        /// </summary>
        /// <param name="client">Lattice client.</param>
        /// <param name="repo">Repository.</param>
        /// <param name="settings">Settings.</param>
        /// <param name="schemaGenerator">Schema generator.</param>
        /// <param name="jsonFlattener">JSON flattener.</param>
        /// <param name="schemaValidator">Schema validator.</param>
        public DocumentMethods(
            LatticeClient client,
            RepositoryBase repo,
            LatticeSettings settings,
            ISchemaGenerator schemaGenerator,
            IJsonFlattener jsonFlattener,
            ISchemaValidator schemaValidator)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _SchemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _JsonFlattener = jsonFlattener ?? throw new ArgumentNullException(nameof(jsonFlattener));
            _SchemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Document> Ingest(
            string collectionId,
            string json,
            string name = null,
            List<string> labels = null,
            Dictionary<string, string> tags = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            // Get collection
            Collection? collection = await _Repo.Collections.ReadById(collectionId, token);
            if (collection == null)
                throw new ArgumentException($"Collection {collectionId} not found", nameof(collectionId));

            // Validate document against schema constraints if enabled
            if (collection.SchemaEnforcementMode != SchemaEnforcementMode.None)
            {
                List<FieldConstraint> fieldConstraints = await _Repo.FieldConstraints.ReadByCollectionId(collectionId, token);
                ValidationResult validationResult = _SchemaValidator.Validate(json, collection.SchemaEnforcementMode, fieldConstraints);

                if (!validationResult.IsValid)
                {
                    throw new SchemaValidationException(collectionId, validationResult.Errors);
                }
            }

            // Generate or find existing schema
            List<SchemaElement> schemaElements = _SchemaGenerator.ExtractElements(json);
            string schemaHash = _SchemaGenerator.ComputeSchemaHash(schemaElements);

            Models.Schema? existingSchema = await _Repo.Schemas.ReadByHash(schemaHash, token);
            Models.Schema schema;

            if (existingSchema != null)
            {
                schema = existingSchema;
            }
            else
            {
                // Create new schema
                schema = new Models.Schema
                {
                    Id = IdGenerator.NewSchemaId(),
                    Hash = schemaHash
                };
                schema = await _Repo.Schemas.Create(schema, token);

                // Create schema elements
                foreach (SchemaElement element in schemaElements)
                {
                    element.Id = IdGenerator.NewSchemaElementId();
                    element.SchemaId = schema.Id;
                }
                await _Repo.SchemaElements.CreateMany(schemaElements, token);

                // Create index tables for each key (if indexing is enabled)
                if (collection.IndexingMode != IndexingMode.None)
                {
                    foreach (SchemaElement element in schemaElements)
                    {
                        IndexTableMapping? mapping = await _Repo.Indexes.GetMappingByKey(element.Key, token);
                        if (mapping == null)
                        {
                            string tableName = HashHelper.GenerateIndexTableName(element.Key);
                            mapping = new IndexTableMapping
                            {
                                Id = IdGenerator.NewIndexTableMappingId(),
                                Key = element.Key,
                                TableName = tableName
                            };
                            await _Repo.Indexes.CreateMapping(mapping, token);
                            await _Repo.Indexes.CreateIndexTable(tableName, token);
                        }
                    }
                }
            }

            // Compute content length and SHA256 hash
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            long contentLength = jsonBytes.Length;
            string sha256Hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(jsonBytes);
                sha256Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // Create document
            Document document = new Document
            {
                Id = IdGenerator.NewDocumentId(),
                CollectionId = collectionId,
                SchemaId = schema.Id,
                Name = name,
                ContentLength = contentLength,
                Sha256Hash = sha256Hash,
                Labels = labels ?? new List<string>(),
                Tags = tags ?? new Dictionary<string, string>()
            };

            // Preserve labels and tags before Create (which returns from DB without them)
            List<string> preservedLabels = document.Labels;
            Dictionary<string, string> preservedTags = document.Tags;

            document = await _Repo.Documents.Create(document, token);

            // Restore labels and tags on the returned document
            document.Labels = preservedLabels;
            document.Tags = preservedTags;

            // Create labels (include collectionId for document-level labels)
            if (document.Labels.Count > 0)
            {
                List<Label> labelEntities = document.Labels.Select(l => new Label
                {
                    Id = IdGenerator.NewLabelId(),
                    CollectionId = collectionId,
                    DocumentId = document.Id,
                    LabelValue = l
                }).ToList();
                await _Repo.Labels.CreateMany(labelEntities, token);
            }

            // Create tags (include collectionId for document-level tags)
            if (document.Tags.Count > 0)
            {
                List<Tag> tagEntities = document.Tags.Select(t => new Tag
                {
                    Id = IdGenerator.NewTagId(),
                    CollectionId = collectionId,
                    DocumentId = document.Id,
                    Key = t.Key,
                    Value = t.Value
                }).ToList();
                await _Repo.Tags.CreateMany(tagEntities, token);
            }

            // Only index if indexing mode is not None
            if (collection.IndexingMode != IndexingMode.None)
            {
                // Get indexed fields for selective indexing
                HashSet<string> indexedFieldPaths = null;
                if (collection.IndexingMode == IndexingMode.Selective)
                {
                    List<IndexedField> indexedFieldsList = await _Repo.IndexedFields.ReadByCollectionId(collectionId, token);
                    indexedFieldPaths = new HashSet<string>(indexedFieldsList.Select(f => f.FieldPath), StringComparer.OrdinalIgnoreCase);
                }

                // Flatten and index document values
                List<FlattenedValue> flattenedValues = _JsonFlattener.Flatten(json);
                IEnumerable<IGrouping<string, FlattenedValue>> valuesByKey = flattenedValues.GroupBy(v => v.Key);

                // Filter values based on indexing mode
                if (collection.IndexingMode == IndexingMode.Selective && indexedFieldPaths != null)
                {
                    valuesByKey = valuesByKey.Where(g => indexedFieldPaths.Contains(g.Key));
                }

                // Collect all values by table name for batch insert (single transaction)
                Dictionary<string, List<DocumentValue>> valuesByTable = new Dictionary<string, List<DocumentValue>>();

                foreach (IGrouping<string, FlattenedValue> group in valuesByKey)
                {
                    IndexTableMapping? mapping = await _Repo.Indexes.GetMappingByKey(group.Key, token);
                    if (mapping != null)
                    {
                        SchemaElement? schemaElement = await _Repo.SchemaElements.ReadBySchemaIdAndKey(schema.Id, group.Key, token);

                        List<DocumentValue> values = group.Select(v => new DocumentValue
                        {
                            Id = IdGenerator.NewValueId(),
                            DocumentId = document.Id,
                            SchemaId = schema.Id,
                            SchemaElementId = schemaElement?.Id,
                            Position = v.Position,
                            Value = v.Value
                        }).ToList();

                        if (!valuesByTable.ContainsKey(mapping.TableName))
                        {
                            valuesByTable[mapping.TableName] = new List<DocumentValue>();
                        }
                        valuesByTable[mapping.TableName].AddRange(values);
                    }
                }

                // Insert all index values in a single transaction
                if (valuesByTable.Count > 0)
                {
                    await _Repo.Indexes.InsertValuesMultiTable(valuesByTable, token);
                }
            }

            // Store raw JSON to disk
            string documentPath = Path.Combine(collection.DocumentsDirectory, $"{document.Id}.json");
            await File.WriteAllTextAsync(documentPath, json, token);

            return document;
        }

        /// <inheritdoc />
        public async Task<Document> ReadById(
            string id,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken token = default)
        {
            // Use optimized single JOIN query when labels or tags are requested
            Document? document;
            if (includeLabels || includeTags)
            {
                document = await _Repo.Documents.ReadByIdWithLabelsAndTags(id, includeLabels, includeTags, token);
            }
            else
            {
                document = await _Repo.Documents.ReadById(id, token);
            }

            if (document == null) return null;

            // Load content if requested
            if (includeContent)
            {
                Collection? collection = await _Repo.Collections.ReadById(document.CollectionId, token);
                if (collection != null)
                {
                    string documentPath = Path.Combine(collection.DocumentsDirectory, $"{document.Id}.json");
                    if (File.Exists(documentPath))
                    {
                        document.Content = await File.ReadAllTextAsync(documentPath, token);
                    }
                }
            }

            return document;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, Document>> ReadByIds(
            List<string> ids,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken token = default)
        {
            if (ids == null || ids.Count == 0)
                return new Dictionary<string, Document>();

            // Use optimized single JOIN query
            Dictionary<string, Document> documents;
            if (includeLabels || includeTags)
            {
                documents = await _Repo.Documents.ReadByIdsWithLabelsAndTags(ids, includeLabels, includeTags, token);
            }
            else
            {
                documents = await _Repo.Documents.ReadByIds(ids, token);
            }

            // Load content if requested
            if (includeContent && documents.Count > 0)
            {
                // Group by collection to minimize collection lookups
                IEnumerable<IGrouping<string, Document>> docsByCollection = documents.Values.GroupBy(d => d.CollectionId);
                foreach (IGrouping<string, Document> group in docsByCollection)
                {
                    Collection? collection = await _Repo.Collections.ReadById(group.Key, token);
                    if (collection != null)
                    {
                        foreach (Document doc in group)
                        {
                            string documentPath = Path.Combine(collection.DocumentsDirectory, $"{doc.Id}.json");
                            if (File.Exists(documentPath))
                            {
                                doc.Content = await File.ReadAllTextAsync(documentPath, token);
                            }
                        }
                    }
                }
            }

            return documents;
        }

        /// <inheritdoc />
        public async Task<List<Document>> ReadAllInCollection(
            string collectionId,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken token = default)
        {
            // First, get all document IDs in the collection
            List<string> documentIds = new List<string>();

            await foreach (Document document in _Repo.Documents.ReadAllInCollection(collectionId, token: token))
            {
                documentIds.Add(document.Id);
            }

            if (documentIds.Count == 0) return new List<Document>();

            // Use optimized batch query with JOINs
            Dictionary<string, Document> documentsById;
            if (includeLabels || includeTags)
            {
                documentsById = await _Repo.Documents.ReadByIdsWithLabelsAndTags(documentIds, includeLabels, includeTags, token);
            }
            else
            {
                documentsById = await _Repo.Documents.ReadByIds(documentIds, token);
            }

            // Return documents in original order
            List<Document> documents = new List<Document>();
            foreach (string docId in documentIds)
            {
                if (documentsById.TryGetValue(docId, out Document doc))
                {
                    documents.Add(doc);
                }
            }

            return documents;
        }

        /// <inheritdoc />
        public async Task Delete(string id, CancellationToken token = default)
        {
            Document? document = await _Repo.Documents.ReadById(id, token);
            if (document == null) return;

            Collection? collection = await _Repo.Collections.ReadById(document.CollectionId, token);
            string documentsDirectory = collection?.DocumentsDirectory ?? _Settings.DefaultDocumentsDirectory;

            await DeleteInternal(id, documentsDirectory, token);
        }

        /// <inheritdoc />
        public Task<bool> Exists(string id, CancellationToken token = default)
        {
            return _Repo.Documents.Exists(id, token);
        }

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Delete a document internal implementation.
        /// </summary>
        internal async Task DeleteInternal(string documentId, string documentsDirectory, CancellationToken token)
        {
            Document? document = await _Repo.Documents.ReadById(documentId, token);
            if (document == null) return;

            // Delete from all index tables
            await foreach (IndexTableMapping mapping in _Repo.Indexes.GetAllMappings(token))
            {
                await _Repo.Indexes.DeleteByDocumentId(mapping.TableName, documentId, token);
            }

            // Delete labels and tags
            await _Repo.Labels.DeleteByDocumentId(documentId, token);
            await _Repo.Tags.DeleteByDocumentId(documentId, token);

            // Delete document record
            await _Repo.Documents.Delete(documentId, token);

            // Delete JSON file
            string documentPath = Path.Combine(documentsDirectory, $"{documentId}.json");
            if (File.Exists(documentPath))
            {
                File.Delete(documentPath);
            }
        }

        #endregion
    }
}
