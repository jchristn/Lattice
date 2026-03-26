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
                bool hasFilters = query.Filters.Count > 0 || query.Labels.Count > 0 || query.Tags.Count > 0;

                if (!hasFilters && !string.IsNullOrWhiteSpace(query.CollectionId))
                {
                    // --- Fast path: no filters, pure collection pagination ---
                    // Use database-level COUNT and LIMIT/OFFSET instead of loading all IDs into memory.

                    result.TotalRecords = await _Repo.Documents.CountInCollection(query.CollectionId, token);

                    if (result.TotalRecords > 0 && query.Skip < result.TotalRecords)
                    {
                        // ReadAllInCollection uses SQL LIMIT/OFFSET — only fetches the page we need
                        List<string> pagedIds = new List<string>();
                        int taken = 0;
                        await foreach (Document doc in _Repo.Documents.ReadAllInCollection(
                            query.CollectionId, query.Ordering, query.Skip, token))
                        {
                            pagedIds.Add(doc.Id);
                            taken++;
                            if (taken >= query.MaxResults) break;
                        }

                        if (pagedIds.Count > 0)
                        {
                            await LoadDocumentsIntoResult(result, pagedIds, query, token);
                        }
                    }
                }
                else
                {
                    // --- Filter path: collect candidate IDs from filters, labels, tags ---

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

                    // Apply label filters
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

                    // Apply tag filters
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

                    if (candidateDocIds == null)
                        candidateDocIds = new HashSet<string>();

                    // Scope filter results to the target collection efficiently:
                    // Instead of loading all collection IDs, load the candidate documents
                    // and check collectionId in memory (candidates are already a reduced set).
                    if (!string.IsNullOrWhiteSpace(query.CollectionId) && candidateDocIds.Count > 0)
                    {
                        Dictionary<string, Document> candidateDocs = await _Repo.Documents.ReadByIds(candidateDocIds.ToList(), token);
                        candidateDocIds = new HashSet<string>(
                            candidateDocs.Values
                                .Where(d => d.CollectionId == query.CollectionId)
                                .Select(d => d.Id));
                    }

                    result.TotalRecords = candidateDocIds.Count;

                    // Apply pagination in memory over the filtered candidate set
                    List<string> pagedIds = candidateDocIds.Skip(query.Skip).Take(query.MaxResults).ToList();

                    if (pagedIds.Count > 0)
                    {
                        await LoadDocumentsIntoResult(result, pagedIds, query, token);
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
        /// Load documents by IDs and populate the search result.
        /// </summary>
        private async Task LoadDocumentsIntoResult(
            SearchResult result,
            List<string> pagedIds,
            SearchQuery query,
            CancellationToken token)
        {
            Dictionary<string, Document> documentsById;
            if (query.IncludeLabels || query.IncludeTags)
            {
                documentsById = await _Repo.Documents.ReadByIdsWithLabelsAndTags(pagedIds, query.IncludeLabels, query.IncludeTags, token);
            }
            else
            {
                documentsById = await _Repo.Documents.ReadByIds(pagedIds, token);
            }

            Collection? collection = null;
            if (query.IncludeContent && !string.IsNullOrWhiteSpace(query.CollectionId))
            {
                collection = await _Repo.Collections.ReadById(query.CollectionId, token);
            }

            foreach (string docId in pagedIds)
            {
                if (documentsById.TryGetValue(docId, out Document doc))
                {
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

        /// <inheritdoc />
        public Task<SearchResult> SearchBySql(string collectionId, string sql, CancellationToken token = default)
        {
            SearchQuery query = _SqlParser.Parse(sql);
            query.CollectionId = collectionId;
            return Search(query, token);
        }

        /// <inheritdoc />
        public Task<SearchResult> SearchBySql(string collectionId, string sql, List<string> labels, Dictionary<string, string> tags, CancellationToken token = default)
        {
            SearchQuery query = _SqlParser.Parse(sql);
            query.CollectionId = collectionId;
            query.Labels = labels ?? new List<string>();
            query.Tags = tags ?? new Dictionary<string, string>();
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
