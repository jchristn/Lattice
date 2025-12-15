namespace Lattice.Core.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Client.Interfaces;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;
    using Lattice.Core.Search;

    /// <summary>
    /// Search methods implementation.
    /// </summary>
    public class SearchMethods : ISearchMethods
    {
        #region Private-Members

        private readonly LatticeClient _Client;
        private readonly RepositoryBase _Repo;
        private readonly SqlParser _SqlParser;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate search methods.
        /// </summary>
        /// <param name="client">Lattice client.</param>
        /// <param name="repo">Repository.</param>
        /// <param name="sqlParser">SQL parser.</param>
        public SearchMethods(
            LatticeClient client,
            RepositoryBase repo,
            SqlParser sqlParser)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _SqlParser = sqlParser ?? throw new ArgumentNullException(nameof(sqlParser));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Task<SearchResult> SearchBySql(string collectionId, string sql, CancellationToken token = default)
        {
            SearchQuery query = _SqlParser.Parse(sql);
            query.CollectionId = collectionId;
            return Search(query, token);
        }

        /// <inheritdoc />
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
                        foreach (KeyValuePair<string, string> tag in docWithData.Tags)
                        {
                            doc.Tags[tag.Key] = tag.Value;
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
