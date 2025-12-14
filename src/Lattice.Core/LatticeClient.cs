namespace Lattice.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Flattening;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;
    using Lattice.Core.Repositories.Sqlite;
    using Lattice.Core.Schema;
    using Lattice.Core.Search;

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

            _Repo = new SqliteRepository(Settings.DatabaseFilename, Settings.InMemory);
            _Repo.InitializeRepository();

            _SchemaGenerator = new SchemaGenerator();
            _JsonFlattener = new JsonFlattener();
            _SqlParser = new SqlParser();
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
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created collection.</returns>
        public async Task<Collection> CreateCollection(
            string name,
            string description = null,
            string documentsDirectory = null,
            List<string> labels = null,
            Dictionary<string, string> tags = null,
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
                Tags = tags ?? new Dictionary<string, string>()
            };

            // Ensure documents directory exists
            if (!Directory.Exists(collection.DocumentsDirectory))
                Directory.CreateDirectory(collection.DocumentsDirectory);

            // Create collection
            Collection created = await _Repo.Collections.Create(collection, token);

            // Create labels
            if (collection.Labels.Count > 0)
            {
                List<CollectionLabel> collectionLabels = collection.Labels.Select(l => new CollectionLabel
                {
                    Id = IdGenerator.NewCollectionLabelId(),
                    CollectionId = created.Id,
                    LabelValue = l
                }).ToList();
                await _Repo.CollectionLabels.CreateMany(collectionLabels, token);
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
            await foreach (CollectionLabel label in _Repo.CollectionLabels.ReadByCollectionId(id, token))
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
                await foreach (CollectionLabel label in _Repo.CollectionLabels.ReadByCollectionId(collection.Id, token))
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
            await _Repo.CollectionLabels.DeleteByCollectionId(id, token);
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

                // Create index tables for each key
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

            // Create document
            Document document = new Document
            {
                Id = IdGenerator.NewDocumentId(),
                CollectionId = collectionId,
                SchemaId = schema.Id,
                Name = name,
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

            // Create labels
            if (document.Labels.Count > 0)
            {
                List<Label> labelEntities = document.Labels.Select(l => new Label
                {
                    Id = IdGenerator.NewLabelId(),
                    DocumentId = document.Id,
                    LabelValue = l
                }).ToList();
                await _Repo.Labels.CreateMany(labelEntities, token);
            }

            // Create tags
            if (document.Tags.Count > 0)
            {
                List<Tag> tagEntities = document.Tags.Select(t => new Tag
                {
                    Id = IdGenerator.NewTagId(),
                    DocumentId = document.Id,
                    Key = t.Key,
                    Value = t.Value
                }).ToList();
                await _Repo.Tags.CreateMany(tagEntities, token);
            }

            // Flatten and index document values
            List<FlattenedValue> flattenedValues = _JsonFlattener.Flatten(json);
            IEnumerable<IGrouping<string, FlattenedValue>> valuesByKey = flattenedValues.GroupBy(v => v.Key);

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
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document or null if not found.</returns>
        public async Task<Document> GetDocument(string id, bool includeContent = false, CancellationToken token = default)
        {
            Document? document = await _Repo.Documents.ReadById(id, token);
            if (document == null) return null;

            // Load labels
            await foreach (Label label in _Repo.Labels.ReadByDocumentId(id, token))
            {
                document.Labels.Add(label.LabelValue);
            }

            // Load tags
            await foreach (Tag tag in _Repo.Tags.ReadByDocumentId(id, token))
            {
                document.Tags[tag.Key] = tag.Value;
            }

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
        /// Get all documents in a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of documents.</returns>
        public async Task<List<Document>> GetDocuments(string collectionId, CancellationToken token = default)
        {
            // First, get all documents in the collection
            List<Document> documents = new List<Document>();
            List<string> documentIds = new List<string>();

            await foreach (Document document in _Repo.Documents.ReadAllInCollection(collectionId, token: token))
            {
                documents.Add(document);
                documentIds.Add(document.Id);
            }

            if (documentIds.Count == 0) return documents;

            // Batch load labels and tags (eliminates N+1 queries)
            Dictionary<string, List<string>> labelsByDocId = await _Repo.Labels.ReadByDocumentIds(documentIds, token);
            Dictionary<string, Dictionary<string, string>> tagsByDocId = await _Repo.Tags.ReadByDocumentIds(documentIds, token);

            // Apply labels and tags to documents
            foreach (Document document in documents)
            {
                if (labelsByDocId.TryGetValue(document.Id, out List<string> labels))
                {
                    foreach (string label in labels)
                    {
                        document.Labels.Add(label);
                    }
                }

                if (tagsByDocId.TryGetValue(document.Id, out Dictionary<string, string> tags))
                {
                    foreach (KeyValuePair<string, string> tag in tags)
                    {
                        document.Tags[tag.Key] = tag.Value;
                    }
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

                // Load full documents using batch operations (eliminates N+1 queries)
                if (pagedIds.Count > 0)
                {
                    Dictionary<string, Document> documentsById = await _Repo.Documents.ReadByIds(pagedIds, token);
                    Dictionary<string, List<string>> labelsByDocId = await _Repo.Labels.ReadByDocumentIds(pagedIds, token);
                    Dictionary<string, Dictionary<string, string>> tagsByDocId = await _Repo.Tags.ReadByDocumentIds(pagedIds, token);

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
                            // Apply labels
                            if (labelsByDocId.TryGetValue(docId, out List<string> labels))
                            {
                                foreach (string label in labels)
                                {
                                    doc.Labels.Add(label);
                                }
                            }

                            // Apply tags
                            if (tagsByDocId.TryGetValue(docId, out Dictionary<string, string> tags))
                            {
                                foreach (KeyValuePair<string, string> tag in tags)
                                {
                                    doc.Tags[tag.Key] = tag.Value;
                                }
                            }

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
        public Task<EnumerationResult<Document>> Enumerate(EnumerationQuery query, CancellationToken token = default)
        {
            return _Repo.Documents.Enumerate(query, token);
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
