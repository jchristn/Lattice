namespace Lattice.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Exceptions;
    using Lattice.Core.Flattening;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;
    using Lattice.Core.Repositories.Sqlite;
    using Lattice.Core.Schema;
    using Lattice.Core.Search;
    using Lattice.Core.Validation;

    /// <summary>
    /// Main client for interacting with the Lattice document store.
    /// </summary>
    public class LatticeClient : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Settings for the client.
        /// </summary>
        public LatticeSettings Settings { get; }

        #endregion

        #region Private-Members

        private readonly RepositoryBase _Repo;
        private readonly ISchemaGenerator _SchemaGenerator;
        private readonly IJsonFlattener _JsonFlattener;
        private readonly SqlParser _SqlParser;
        private readonly ISchemaValidator _SchemaValidator;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the Lattice client.
        /// </summary>
        /// <param name="settings">Client settings.</param>
        public LatticeClient(LatticeSettings settings = null)
        {
            Settings = settings ?? new LatticeSettings();

            _Repo = new SqliteRepository(Settings.Database.Filename, Settings.InMemory);
            _Repo.InitializeRepository();

            _SchemaGenerator = new SchemaGenerator();
            _JsonFlattener = new JsonFlattener();
            _SqlParser = new SqlParser();
            _SchemaValidator = new SchemaValidator();
        }

        /// <summary>
        /// Instantiate the Lattice client with a custom repository.
        /// </summary>
        /// <param name="repo">Repository implementation.</param>
        /// <param name="settings">Client settings.</param>
        public LatticeClient(RepositoryBase repo, LatticeSettings settings = null)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            Settings = settings ?? new LatticeSettings();

            _SchemaGenerator = new SchemaGenerator();
            _JsonFlattener = new JsonFlattener();
            _SqlParser = new SqlParser();
            _SchemaValidator = new SchemaValidator();
        }

        #endregion

        #region Public-Methods-Collections

        /// <summary>
        /// Create a new collection.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <param name="description">Collection description.</param>
        /// <param name="documentsDirectory">Directory for storing documents.</param>
        /// <param name="labels">Collection labels.</param>
        /// <param name="tags">Collection tags.</param>
        /// <param name="schemaEnforcementMode">Schema enforcement mode for documents.</param>
        /// <param name="fieldConstraints">Field constraints for schema validation.</param>
        /// <param name="indexingMode">Indexing mode for documents.</param>
        /// <param name="indexedFields">Fields to index (when indexingMode is Selective).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created collection.</returns>
        public async Task<Collection> CreateCollection(
            string name,
            string description = null,
            string documentsDirectory = null,
            List<string> labels = null,
            Dictionary<string, string> tags = null,
            SchemaEnforcementMode schemaEnforcementMode = SchemaEnforcementMode.None,
            List<FieldConstraint> fieldConstraints = null,
            IndexingMode indexingMode = IndexingMode.All,
            List<string> indexedFields = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Collection collection = new Collection
            {
                Id = IdGenerator.NewCollectionId(),
                Name = name,
                Description = description,
                DocumentsDirectory = documentsDirectory ?? Path.Combine(Settings.DefaultDocumentsDirectory, name),
                Labels = labels ?? new List<string>(),
                Tags = tags ?? new Dictionary<string, string>(),
                SchemaEnforcementMode = schemaEnforcementMode,
                IndexingMode = indexingMode
            };

            // Ensure documents directory exists
            if (!Directory.Exists(collection.DocumentsDirectory))
                Directory.CreateDirectory(collection.DocumentsDirectory);

            // Create collection
            Collection created = await _Repo.Collections.Create(collection, token);

            // Create labels
            if (collection.Labels.Count > 0)
            {
                List<Label> collectionLabels = collection.Labels.Select(l => new Label
                {
                    Id = IdGenerator.NewLabelId(),
                    CollectionId = created.Id,
                    DocumentId = null,
                    LabelValue = l
                }).ToList();
                await _Repo.Labels.CreateMany(collectionLabels, token);
            }

            // Create tags
            if (collection.Tags.Count > 0)
            {
                List<Tag> collectionTags = collection.Tags.Select(t => new Tag
                {
                    Id = IdGenerator.NewTagId(),
                    CollectionId = created.Id,
                    Key = t.Key,
                    Value = t.Value
                }).ToList();
                await _Repo.Tags.CreateMany(collectionTags, token);
            }

            // Create field constraints
            if (fieldConstraints != null && fieldConstraints.Count > 0)
            {
                foreach (var constraint in fieldConstraints)
                {
                    constraint.Id = IdGenerator.NewFieldConstraintId();
                    constraint.CollectionId = created.Id;
                    constraint.CreatedUtc = DateTime.UtcNow;
                    constraint.LastUpdateUtc = DateTime.UtcNow;
                }
                await _Repo.FieldConstraints.CreateMany(fieldConstraints, token);
            }

            // Create indexed fields
            if (indexingMode == IndexingMode.Selective && indexedFields != null && indexedFields.Count > 0)
            {
                List<IndexedField> indexedFieldEntities = indexedFields.Select(f => new IndexedField
                {
                    Id = IdGenerator.NewIndexedFieldId(),
                    CollectionId = created.Id,
                    FieldPath = f,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                }).ToList();
                await _Repo.IndexedFields.CreateMany(indexedFieldEntities, token);
            }

            created.Labels = collection.Labels;
            created.Tags = collection.Tags;

            return created;
        }

        /// <summary>
        /// Get a collection by ID.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Collection or null if not found.</returns>
        public async Task<Collection> GetCollection(string id, CancellationToken token = default)
        {
            Collection? collection = await _Repo.Collections.ReadById(id, token);
            if (collection == null) return null;

            // Load labels
            await foreach (Label label in _Repo.Labels.ReadByCollectionId(id, token))
            {
                collection.Labels.Add(label.LabelValue);
            }

            // Load tags
            await foreach (Tag tag in _Repo.Tags.ReadByCollectionId(id, token))
            {
                collection.Tags[tag.Key] = tag.Value;
            }

            return collection;
        }

        /// <summary>
        /// Get all collections.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of collections.</returns>
        public async Task<List<Collection>> GetCollections(CancellationToken token = default)
        {
            List<Collection> collections = new List<Collection>();

            await foreach (Collection collection in _Repo.Collections.ReadAll(token))
            {
                // Load labels
                await foreach (Label label in _Repo.Labels.ReadByCollectionId(collection.Id, token))
                {
                    collection.Labels.Add(label.LabelValue);
                }

                // Load tags
                await foreach (Tag tag in _Repo.Tags.ReadByCollectionId(collection.Id, token))
                {
                    collection.Tags[tag.Key] = tag.Value;
                }

                collections.Add(collection);
            }

            return collections;
        }

        /// <summary>
        /// Delete a collection and all its documents.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task DeleteCollection(string id, CancellationToken token = default)
        {
            Collection? collection = await _Repo.Collections.ReadById(id, token);
            if (collection == null) return;

            // Delete all documents in collection
            await foreach (Document doc in _Repo.Documents.ReadAllInCollection(id, token: token))
            {
                await DeleteDocumentInternal(doc.Id, collection.DocumentsDirectory, token);
            }

            // Delete collection labels and tags (cascade should handle this)
            await _Repo.Labels.DeleteByCollectionId(id, token);
            await _Repo.Tags.DeleteByCollectionId(id, token);

            // Delete collection
            await _Repo.Collections.Delete(id, token);
        }

        /// <summary>
        /// Check if a collection exists.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        public Task<bool> CollectionExists(string id, CancellationToken token = default)
        {
            return _Repo.Collections.Exists(id, token);
        }

        /// <summary>
        /// Get the field constraints for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of field constraints.</returns>
        public Task<List<FieldConstraint>> GetCollectionConstraints(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return _Repo.FieldConstraints.ReadByCollectionId(collectionId, token);
        }

        /// <summary>
        /// Get the indexed fields for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of indexed fields.</returns>
        public Task<List<IndexedField>> GetCollectionIndexedFields(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return _Repo.IndexedFields.ReadByCollectionId(collectionId, token);
        }

        /// <summary>
        /// Update the schema constraints for a collection.
        /// Existing documents are NOT re-validated. New documents will be validated against the new schema.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="schemaEnforcementMode">New schema enforcement mode.</param>
        /// <param name="fieldConstraints">New field constraints (replaces existing).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated collection.</returns>
        public async Task<Collection> UpdateCollectionConstraints(
            string collectionId,
            SchemaEnforcementMode schemaEnforcementMode,
            List<FieldConstraint> fieldConstraints = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            Collection? collection = await _Repo.Collections.ReadById(collectionId, token);
            if (collection == null)
                throw new ArgumentException($"Collection {collectionId} not found", nameof(collectionId));

            // Update collection's enforcement mode
            collection.SchemaEnforcementMode = schemaEnforcementMode;
            collection = await _Repo.Collections.Update(collection, token);

            // Delete existing constraints
            await _Repo.FieldConstraints.DeleteByCollectionId(collectionId, token);

            // Create new constraints if provided
            if (fieldConstraints != null && fieldConstraints.Count > 0)
            {
                foreach (var constraint in fieldConstraints)
                {
                    constraint.Id = IdGenerator.NewFieldConstraintId();
                    constraint.CollectionId = collectionId;
                    constraint.CreatedUtc = DateTime.UtcNow;
                    constraint.LastUpdateUtc = DateTime.UtcNow;
                }
                await _Repo.FieldConstraints.CreateMany(fieldConstraints, token);
            }

            return collection;
        }

        /// <summary>
        /// Update the indexing configuration for a collection.
        /// NOTE: Does not automatically rebuild indexes. Call RebuildIndexes() after this.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="indexingMode">New indexing mode.</param>
        /// <param name="indexedFields">Fields to index (when mode is Selective).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated collection.</returns>
        public async Task<Collection> UpdateCollectionIndexing(
            string collectionId,
            IndexingMode indexingMode,
            List<string> indexedFields = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            Collection? collection = await _Repo.Collections.ReadById(collectionId, token);
            if (collection == null)
                throw new ArgumentException($"Collection {collectionId} not found", nameof(collectionId));

            // Update collection's indexing mode
            collection.IndexingMode = indexingMode;
            collection = await _Repo.Collections.Update(collection, token);

            // Delete existing indexed fields
            await _Repo.IndexedFields.DeleteByCollectionId(collectionId, token);

            // Create new indexed fields if provided and mode is Selective
            if (indexingMode == IndexingMode.Selective && indexedFields != null && indexedFields.Count > 0)
            {
                List<IndexedField> indexedFieldEntities = indexedFields.Select(f => new IndexedField
                {
                    Id = IdGenerator.NewIndexedFieldId(),
                    CollectionId = collectionId,
                    FieldPath = f,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                }).ToList();
                await _Repo.IndexedFields.CreateMany(indexedFieldEntities, token);
            }

            return collection;
        }

        /// <summary>
        /// Rebuild all indexes for a collection based on current IndexingMode.
        /// This is a potentially long-running operation for large collections.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="dropUnusedIndexes">Whether to drop index tables not in the indexed fields list.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Rebuild result.</returns>
        public async Task<IndexRebuildResult> RebuildIndexes(
            string collectionId,
            bool dropUnusedIndexes = true,
            IProgress<IndexRebuildProgress> progress = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            var result = new IndexRebuildResult { CollectionId = collectionId };
            var stopwatch = Stopwatch.StartNew();

            // Load collection
            Collection? collection = await _Repo.Collections.ReadById(collectionId, token);
            if (collection == null)
                throw new ArgumentException($"Collection {collectionId} not found", nameof(collectionId));

            // Get all documents in collection
            List<Document> documents = new List<Document>();
            await foreach (Document doc in _Repo.Documents.ReadAllInCollection(collectionId, token: token))
            {
                documents.Add(doc);
            }

            progress?.Report(new IndexRebuildProgress
            {
                TotalDocuments = documents.Count,
                CurrentPhase = "Scanning"
            });

            // If dropping unused indexes, identify and clear values from tables not in indexed fields
            if (dropUnusedIndexes && collection.IndexingMode == IndexingMode.Selective)
            {
                progress?.Report(new IndexRebuildProgress
                {
                    TotalDocuments = documents.Count,
                    CurrentPhase = "Dropping"
                });

                List<IndexedField> indexedFieldsList = await _Repo.IndexedFields.ReadByCollectionId(collectionId, token);
                HashSet<string> indexedPaths = new HashSet<string>(indexedFieldsList.Select(f => f.FieldPath), StringComparer.OrdinalIgnoreCase);

                // Get all index tables used by this collection
                List<string> collectionIndexTables = await _Repo.Indexes.GetIndexTablesForCollection(collectionId, token);

                foreach (string tableName in collectionIndexTables)
                {
                    IndexTableMapping? mapping = await _Repo.Indexes.GetMappingByTableName(tableName, token);
                    if (mapping != null && !indexedPaths.Contains(mapping.Key))
                    {
                        await _Repo.Indexes.DeleteValuesFromTable(tableName, collectionId, token);
                        result.IndexesDropped++;
                    }
                }
            }

            // Clear existing values for this collection from relevant index tables
            progress?.Report(new IndexRebuildProgress
            {
                TotalDocuments = documents.Count,
                CurrentPhase = "Clearing"
            });

            await _Repo.Indexes.DeleteValuesByCollectionId(collectionId, token);

            // Skip re-indexing if mode is None
            if (collection.IndexingMode == IndexingMode.None)
            {
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            // Get indexed fields for selective indexing
            HashSet<string> indexedFieldPaths = null;
            if (collection.IndexingMode == IndexingMode.Selective)
            {
                List<IndexedField> indexedFieldsList = await _Repo.IndexedFields.ReadByCollectionId(collectionId, token);
                indexedFieldPaths = new HashSet<string>(indexedFieldsList.Select(f => f.FieldPath), StringComparer.OrdinalIgnoreCase);
            }

            // Re-index each document
            progress?.Report(new IndexRebuildProgress
            {
                TotalDocuments = documents.Count,
                CurrentPhase = "Indexing"
            });

            for (int i = 0; i < documents.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                Document doc = documents[i];
                try
                {
                    // Load raw JSON
                    string jsonPath = Path.Combine(collection.DocumentsDirectory, $"{doc.Id}.json");
                    if (!File.Exists(jsonPath))
                    {
                        result.Errors.Add($"Document {doc.Id}: JSON file not found at {jsonPath}");
                        continue;
                    }

                    string json = await File.ReadAllTextAsync(jsonPath, token);

                    // Flatten values
                    List<FlattenedValue> flattenedValues = _JsonFlattener.Flatten(json);
                    IEnumerable<IGrouping<string, FlattenedValue>> valuesByKey = flattenedValues.GroupBy(v => v.Key);

                    // Filter values based on indexing mode
                    if (collection.IndexingMode == IndexingMode.Selective && indexedFieldPaths != null)
                    {
                        valuesByKey = valuesByKey.Where(g => indexedFieldPaths.Contains(g.Key));
                    }

                    // Collect values by table
                    Dictionary<string, List<DocumentValue>> valuesByTable = new Dictionary<string, List<DocumentValue>>();

                    foreach (IGrouping<string, FlattenedValue> group in valuesByKey)
                    {
                        IndexTableMapping? mapping = await _Repo.Indexes.GetMappingByKey(group.Key, token);
                        if (mapping == null)
                        {
                            // Create index table if it doesn't exist
                            string tableName = HashHelper.GenerateIndexTableName(group.Key);
                            mapping = new IndexTableMapping
                            {
                                Id = IdGenerator.NewIndexTableMappingId(),
                                Key = group.Key,
                                TableName = tableName
                            };
                            await _Repo.Indexes.CreateMapping(mapping, token);
                            await _Repo.Indexes.CreateIndexTable(tableName, token);
                            result.IndexesCreated++;
                        }

                        SchemaElement? schemaElement = await _Repo.SchemaElements.ReadBySchemaIdAndKey(doc.SchemaId, group.Key, token);

                        List<DocumentValue> values = group.Select(v => new DocumentValue
                        {
                            Id = IdGenerator.NewValueId(),
                            DocumentId = doc.Id,
                            SchemaId = doc.SchemaId,
                            SchemaElementId = schemaElement?.Id,
                            Position = v.Position,
                            Value = v.Value
                        }).ToList();

                        if (!valuesByTable.ContainsKey(mapping.TableName))
                        {
                            valuesByTable[mapping.TableName] = new List<DocumentValue>();
                        }
                        valuesByTable[mapping.TableName].AddRange(values);
                        result.ValuesInserted += values.Count;
                    }

                    // Insert values
                    if (valuesByTable.Count > 0)
                    {
                        await _Repo.Indexes.InsertValuesMultiTable(valuesByTable, token);
                    }

                    result.DocumentsProcessed++;

                    progress?.Report(new IndexRebuildProgress
                    {
                        TotalDocuments = documents.Count,
                        ProcessedDocuments = i + 1,
                        CurrentPhase = "Indexing"
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Document {doc.Id}: {ex.Message}");
                }
            }

            result.Duration = stopwatch.Elapsed;
            return result;
        }

        #endregion

        #region Public-Methods-Documents

        /// <summary>
        /// Ingest a JSON document into a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="json">JSON document content.</param>
        /// <param name="name">Document name.</param>
        /// <param name="labels">Document labels.</param>
        /// <param name="tags">Document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Ingested document.</returns>
        public async Task<Document> IngestDocument(
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

        /// <summary>
        /// Get a document by ID.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="includeContent">Include raw JSON content.</param>
        /// <param name="includeLabels">Include document labels.</param>
        /// <param name="includeTags">Include document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document or null if not found.</returns>
        public async Task<Document> GetDocument(
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

        /// <summary>
        /// Get multiple documents by their IDs in a single optimized query.
        /// </summary>
        /// <param name="ids">Document IDs.</param>
        /// <param name="includeContent">Include raw JSON content.</param>
        /// <param name="includeLabels">Include document labels.</param>
        /// <param name="includeTags">Include document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of document ID to document (missing IDs are not included).</returns>
        public async Task<Dictionary<string, Document>> GetDocumentsByIds(
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
                var docsByCollection = documents.Values.GroupBy(d => d.CollectionId);
                foreach (var group in docsByCollection)
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

        /// <summary>
        /// Get all documents in a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="includeLabels">Include document labels.</param>
        /// <param name="includeTags">Include document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of documents.</returns>
        public async Task<List<Document>> GetDocuments(
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

        /// <summary>
        /// Delete a document.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task DeleteDocument(string id, CancellationToken token = default)
        {
            Document? document = await _Repo.Documents.ReadById(id, token);
            if (document == null) return;

            Collection? collection = await _Repo.Collections.ReadById(document.CollectionId, token);
            string documentsDirectory = collection?.DocumentsDirectory ?? Settings.DefaultDocumentsDirectory;

            await DeleteDocumentInternal(id, documentsDirectory, token);
        }

        /// <summary>
        /// Check if a document exists.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        public Task<bool> DocumentExists(string id, CancellationToken token = default)
        {
            return _Repo.Documents.Exists(id, token);
        }

        #endregion

        #region Public-Methods-Search

        /// <summary>
        /// Search documents using a SearchQuery.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        public async Task<SearchResult> Search(SearchQuery query, CancellationToken token = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            SearchResult result = new SearchResult
            {
                MaxResults = query.MaxResults,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            try
            {
                // Find matching document IDs from filters
                HashSet<string> candidateDocIds = null;

                foreach (SearchFilter filter in query.Filters)
                {
                    IndexTableMapping? mapping = await _Repo.Indexes.GetMappingByKey(filter.Field, token);
                    if (mapping == null) continue;

                    HashSet<string> matchingIds = new HashSet<string>();
                    await foreach (string docId in _Repo.Indexes.Search(mapping.TableName, filter, token))
                    {
                        matchingIds.Add(docId);
                    }

                    if (candidateDocIds == null)
                    {
                        candidateDocIds = matchingIds;
                    }
                    else
                    {
                        candidateDocIds.IntersectWith(matchingIds);
                    }
                }

                // Apply label filters using JOIN queries for efficient first-pass filtering
                if (query.Labels.Count > 0)
                {
                    HashSet<string> labelDocIds = await _Repo.Labels.FindDocumentIdsByLabels(query.Labels.ToList(), token);

                    if (candidateDocIds == null)
                    {
                        candidateDocIds = labelDocIds;
                    }
                    else
                    {
                        candidateDocIds.IntersectWith(labelDocIds);
                    }
                }

                // Apply tag filters using JOIN queries for efficient first-pass filtering
                if (query.Tags.Count > 0)
                {
                    HashSet<string> tagDocIds = await _Repo.Tags.FindDocumentIdsByTags(query.Tags, token);

                    if (candidateDocIds == null)
                    {
                        candidateDocIds = tagDocIds;
                    }
                    else
                    {
                        candidateDocIds.IntersectWith(tagDocIds);
                    }
                }

                // If no filters, get all documents in collection
                if (candidateDocIds == null)
                {
                    candidateDocIds = new HashSet<string>();
                    if (!string.IsNullOrWhiteSpace(query.CollectionId))
                    {
                        await foreach (Document doc in _Repo.Documents.ReadAllInCollection(query.CollectionId, query.Ordering, token: token))
                        {
                            candidateDocIds.Add(doc.Id);
                        }
                    }
                }

                // Filter by collection if specified
                if (!string.IsNullOrWhiteSpace(query.CollectionId) && candidateDocIds.Count > 0)
                {
                    HashSet<string> collectionDocIds = new HashSet<string>();
                    await foreach (Document doc in _Repo.Documents.ReadAllInCollection(query.CollectionId, token: token))
                    {
                        collectionDocIds.Add(doc.Id);
                    }
                    candidateDocIds.IntersectWith(collectionDocIds);
                }

                result.TotalRecords = candidateDocIds.Count;

                // Apply pagination
                List<string> pagedIds = candidateDocIds.Skip(query.Skip).Take(query.MaxResults).ToList();

                // Load full documents using optimized single JOIN query
                if (pagedIds.Count > 0)
                {
                    // Use optimized batch query with JOINs for documents, labels, and tags
                    Dictionary<string, Document> documentsById;
                    if (query.IncludeLabels || query.IncludeTags)
                    {
                        documentsById = await _Repo.Documents.ReadByIdsWithLabelsAndTags(pagedIds, query.IncludeLabels, query.IncludeTags, token);
                    }
                    else
                    {
                        documentsById = await _Repo.Documents.ReadByIds(pagedIds, token);
                    }

                    // Get collection for content loading if needed
                    Collection? collection = null;
                    if (query.IncludeContent && !string.IsNullOrWhiteSpace(query.CollectionId))
                    {
                        collection = await _Repo.Collections.ReadById(query.CollectionId, token);
                    }

                    // Assemble documents in original order
                    foreach (string docId in pagedIds)
                    {
                        if (documentsById.TryGetValue(docId, out Document doc))
                        {
                            // Load content if requested
                            if (query.IncludeContent)
                            {
                                Collection? docCollection = collection;
                                if (docCollection == null || docCollection.Id != doc.CollectionId)
                                {
                                    docCollection = await _Repo.Collections.ReadById(doc.CollectionId, token);
                                }
                                if (docCollection != null)
                                {
                                    string documentPath = Path.Combine(docCollection.DocumentsDirectory, $"{doc.Id}.json");
                                    if (File.Exists(documentPath))
                                    {
                                        doc.Content = await File.ReadAllTextAsync(documentPath, token);
                                    }
                                }
                            }

                            result.Documents.Add(doc);
                        }
                    }
                }

                result.RecordsRemaining = Math.Max(0, result.TotalRecords - query.Skip - result.Documents.Count);
                result.EndOfResults = result.RecordsRemaining == 0;
                result.Success = true;
            }
            catch (Exception)
            {
                result.Success = false;
                throw;
            }

            result.Timestamp.End = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Search documents using a SQL-like expression.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="sql">SQL-like expression.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        public Task<SearchResult> SearchBySql(string collectionId, string sql, CancellationToken token = default)
        {
            SearchQuery query = _SqlParser.Parse(sql);
            query.CollectionId = collectionId;
            return Search(query, token);
        }

        /// <summary>
        /// Enumerate documents with pagination.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        public async Task<EnumerationResult<Document>> Enumerate(EnumerationQuery query, CancellationToken token = default)
        {
            EnumerationResult<Document> result = await _Repo.Documents.Enumerate(query, token);

            // Load labels and tags if requested and there are documents
            if (result.Objects.Count > 0 && (query.IncludeLabels || query.IncludeTags))
            {
                List<string> documentIds = result.Objects.Select(d => d.Id).ToList();

                // Use optimized single JOIN query for labels and tags
                Dictionary<string, Document> docsWithLabelsAndTags = await _Repo.Documents.ReadByIdsWithLabelsAndTags(
                    documentIds, query.IncludeLabels, query.IncludeTags, token);

                // Apply labels and tags to existing documents (preserving order)
                foreach (Document doc in result.Objects)
                {
                    if (docsWithLabelsAndTags.TryGetValue(doc.Id, out Document docWithData))
                    {
                        foreach (string label in docWithData.Labels)
                        {
                            doc.Labels.Add(label);
                        }
                        foreach (var tag in docWithData.Tags)
                        {
                            doc.Tags[tag.Key] = tag.Value;
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Public-Methods-Schemas

        /// <summary>
        /// Get all schemas.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of schemas.</returns>
        public async Task<List<Models.Schema>> GetSchemas(CancellationToken token = default)
        {
            List<Models.Schema> schemas = new List<Models.Schema>();
            await foreach (Models.Schema schema in _Repo.Schemas.ReadAll(token))
            {
                schemas.Add(schema);
            }
            return schemas;
        }

        /// <summary>
        /// Get a schema by ID.
        /// </summary>
        /// <param name="id">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schema or null if not found.</returns>
        public Task<Models.Schema> GetSchema(string id, CancellationToken token = default)
        {
            return _Repo.Schemas.ReadById(id, token);
        }

        /// <summary>
        /// Get schema elements for a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of schema elements.</returns>
        public async Task<List<SchemaElement>> GetSchemaElements(string schemaId, CancellationToken token = default)
        {
            List<SchemaElement> elements = new List<SchemaElement>();
            await foreach (SchemaElement element in _Repo.SchemaElements.ReadBySchemaId(schemaId, token))
            {
                elements.Add(element);
            }
            return elements;
        }

        #endregion

        #region Public-Methods-IndexTables

        /// <summary>
        /// Get all index table mappings.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of index table mappings.</returns>
        public async Task<List<IndexTableMapping>> GetIndexTableMappings(CancellationToken token = default)
        {
            List<IndexTableMapping> mappings = new List<IndexTableMapping>();
            await foreach (IndexTableMapping mapping in _Repo.Indexes.GetAllMappings(token))
            {
                mappings.Add(mapping);
            }
            return mappings;
        }

        /// <summary>
        /// Get an index table mapping by key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Index table mapping or null if not found.</returns>
        public Task<IndexTableMapping> GetIndexTableMapping(string key, CancellationToken token = default)
        {
            return _Repo.Indexes.GetMappingByKey(key, token);
        }

        #endregion

        #region Public-Methods-Utility

        /// <summary>
        /// Flush in-memory data to disk.
        /// </summary>
        public void Flush()
        {
            _Repo.Flush();
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;

            _Repo?.Dispose();
            _Disposed = true;
        }

        #endregion

        #region Private-Methods

        private async Task DeleteDocumentInternal(string documentId, string documentsDirectory, CancellationToken token)
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
