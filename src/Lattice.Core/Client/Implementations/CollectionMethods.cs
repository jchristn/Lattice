namespace Lattice.Core.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Client.Interfaces;
    using Lattice.Core.Flattening;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;

    /// <summary>
    /// Collection methods implementation.
    /// </summary>
    public class CollectionMethods : ICollectionMethods
    {
        #region Private-Members

        private readonly LatticeClient _Client;
        private readonly RepositoryBase _Repo;
        private readonly LatticeSettings _Settings;
        private readonly IJsonFlattener _JsonFlattener;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate collection methods.
        /// </summary>
        /// <param name="client">Lattice client.</param>
        /// <param name="repo">Repository.</param>
        /// <param name="settings">Settings.</param>
        /// <param name="jsonFlattener">JSON flattener.</param>
        public CollectionMethods(
            LatticeClient client,
            RepositoryBase repo,
            LatticeSettings settings,
            IJsonFlattener jsonFlattener)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _JsonFlattener = jsonFlattener ?? throw new ArgumentNullException(nameof(jsonFlattener));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Collection> Create(
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
                DocumentsDirectory = documentsDirectory ?? Path.Combine(_Settings.DefaultDocumentsDirectory, name),
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
                foreach (FieldConstraint constraint in fieldConstraints)
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

        /// <inheritdoc />
        public async Task<Collection> ReadById(string id, CancellationToken token = default)
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

        /// <inheritdoc />
        public async Task<List<Collection>> ReadAll(CancellationToken token = default)
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

        /// <inheritdoc />
        public async Task Delete(string id, CancellationToken token = default)
        {
            Collection? collection = await _Repo.Collections.ReadById(id, token);
            if (collection == null) return;

            // Delete all documents in collection
            await foreach (Document doc in _Repo.Documents.ReadAllInCollection(id, token: token))
            {
                await _Client.Document.Delete(doc.Id, token);
            }

            // Delete collection labels and tags (cascade should handle this)
            await _Repo.Labels.DeleteByCollectionId(id, token);
            await _Repo.Tags.DeleteByCollectionId(id, token);

            // Delete collection
            await _Repo.Collections.Delete(id, token);
        }

        /// <inheritdoc />
        public Task<bool> Exists(string id, CancellationToken token = default)
        {
            return _Repo.Collections.Exists(id, token);
        }

        /// <inheritdoc />
        public Task<List<FieldConstraint>> GetConstraints(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return _Repo.FieldConstraints.ReadByCollectionId(collectionId, token);
        }

        /// <inheritdoc />
        public Task<List<IndexedField>> GetIndexedFields(string collectionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            return _Repo.IndexedFields.ReadByCollectionId(collectionId, token);
        }

        /// <inheritdoc />
        public async Task<Collection> UpdateConstraints(
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
                foreach (FieldConstraint constraint in fieldConstraints)
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

        /// <inheritdoc />
        public async Task<Collection> UpdateIndexing(
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

        /// <inheritdoc />
        public async Task<IndexRebuildResult> RebuildIndexes(
            string collectionId,
            bool dropUnusedIndexes = true,
            IProgress<IndexRebuildProgress> progress = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentNullException(nameof(collectionId));

            IndexRebuildResult result = new IndexRebuildResult { CollectionId = collectionId };
            Stopwatch stopwatch = Stopwatch.StartNew();

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
    }
}
