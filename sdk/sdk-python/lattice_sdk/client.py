"""
Lattice SDK Client

Main client for interacting with the Lattice REST API.
"""

import json
from typing import Optional, List, Dict, Any, Callable
from urllib.parse import urljoin
import requests

from .models import (
    Collection,
    Document,
    Schema,
    SchemaElement,
    FieldConstraint,
    IndexedField,
    SearchResult,
    IndexRebuildResult,
    SearchQuery,
    SearchFilter,
    IndexTableMapping,
    IndexTableEntry,
    EnumerationResult,
    SchemaEnforcementMode,
    IndexingMode,
    EnumerationOrder,
    BatchIngestDocument
)
from .exceptions import (
    LatticeException,
    LatticeConnectionError,
    LatticeApiError,
    LatticeValidationError
)


class LatticeClient:
    """
    Client for interacting with the Lattice REST API.

    Usage:
        client = LatticeClient("http://localhost:8000")
        collections = client.collection.read_all()
    """

    def __init__(self, base_url: str, timeout: int = 30):
        """
        Initialize the Lattice client.

        Args:
            base_url: The base URL of the Lattice server (e.g., "http://localhost:8000")
            timeout: Request timeout in seconds (default: 30)
        """
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self._session = requests.Session()

        # Request/response correlation id from the most recent response's
        # X-Lattice-Request-Id header (replaces the old envelope `guid`).
        self.last_request_id: Optional[str] = None

        # Initialize method groups
        self.collection = CollectionMethods(self)
        self.document = DocumentMethods(self)
        self.search = SearchMethods(self)
        self.schema = SchemaMethods(self)
        self.index = IndexMethods(self)

    def _request(
        self,
        method: str,
        path: str,
        data: Optional[Dict[str, Any]] = None,
        params: Optional[Dict[str, Any]] = None
    ) -> Any:
        """
        Make an HTTP request to the Lattice API.

        The Lattice REST API returns raw payloads (no response envelope):

        - HEAD requests: no body. Returns ``True`` for a 2xx status, else
          ``False`` (used for existence checks).
        - Success (HTTP 2xx): the response body IS the payload. Returns the
          parsed JSON body directly, or ``None`` when the body is empty.
        - Error (non-2xx): the body is ``{ "error": ..., "detail"?: ... }``.
          Raises :class:`LatticeApiError` with that message (falling back to
          the HTTP reason phrase), the status code, and any structured detail.

        Args:
            method: HTTP method (GET, POST, PUT, DELETE, HEAD)
            path: API path (e.g., "/v1.0/collections")
            data: Request body data (for POST/PUT)
            params: Query parameters

        Returns:
            The parsed payload (dict/list/str), ``None`` for an empty body,
            or a ``bool`` for HEAD requests.
        """
        url = f"{self.base_url}{path}"

        try:
            response = self._session.request(
                method=method,
                url=url,
                json=data,
                params=params,
                timeout=self.timeout
            )
        except requests.ConnectionError as e:
            raise LatticeConnectionError(f"Failed to connect to {url}", e)
        except requests.Timeout as e:
            raise LatticeConnectionError(f"Request to {url} timed out", e)
        except requests.RequestException as e:
            raise LatticeException(f"Request failed: {str(e)}")

        # Correlation id now travels in a response header (was body `guid`).
        self.last_request_id = response.headers.get("X-Lattice-Request-Id")

        is_success = 200 <= response.status_code < 300

        # HEAD requests carry no body; the status code is the answer.
        if method.upper() == "HEAD":
            return is_success

        if not is_success:
            self._raise_for_error(response)

        # Success: the body IS the payload. Empty body => None (don't call
        # json() on an empty body).
        if not response.content:
            return None
        try:
            return response.json()
        except json.JSONDecodeError:
            return response.text

    def _raise_for_error(self, response: requests.Response) -> None:
        """Raise a LatticeApiError from a non-2xx response.

        Parses the ``{ "error", "detail"? }`` error body. Falls back to the
        raw text or HTTP reason phrase when the body is missing or not JSON.
        """
        error_message: Optional[str] = None
        detail: Any = None

        if response.content:
            try:
                body = response.json()
                if isinstance(body, dict):
                    error_message = body.get("error")
                    detail = body.get("detail")
                else:
                    error_message = str(body)
            except json.JSONDecodeError:
                error_message = response.text or None

        if not error_message:
            error_message = response.reason or f"HTTP {response.status_code}"

        raise LatticeApiError(
            error_message,
            response.status_code,
            error_message=error_message,
            detail=detail,
            request_id=self.last_request_id,
        )

    def health_check(self) -> bool:
        """
        Check if the Lattice server is healthy.

        Returns:
            True if the server is healthy, False otherwise
        """
        try:
            self._request("GET", "/v1.0/health")
            return True
        except LatticeException:
            return False


class CollectionMethods:
    """Methods for managing collections."""

    def __init__(self, client: LatticeClient):
        self._client = client

    def create(
        self,
        name: str,
        description: Optional[str] = None,
        documents_directory: Optional[str] = None,
        labels: Optional[List[str]] = None,
        tags: Optional[Dict[str, str]] = None,
        schema_enforcement_mode: SchemaEnforcementMode = SchemaEnforcementMode.NONE,
        field_constraints: Optional[List[FieldConstraint]] = None,
        indexing_mode: IndexingMode = IndexingMode.ALL,
        indexed_fields: Optional[List[str]] = None
    ) -> Optional[Collection]:
        """
        Create a new collection.

        Args:
            name: Collection name
            description: Optional description
            documents_directory: Optional directory for document storage
            labels: Optional list of labels
            tags: Optional dictionary of tags
            schema_enforcement_mode: Schema validation mode
            field_constraints: Optional list of field constraints
            indexing_mode: Indexing mode for the collection
            indexed_fields: Optional list of fields to index (for selective mode)

        Returns:
            The created Collection, or None if creation failed
        """
        data = {"name": name}

        if description:
            data["description"] = description
        if documents_directory:
            data["documentsDirectory"] = documents_directory
        if labels:
            data["labels"] = labels
        if tags:
            data["tags"] = tags
        if schema_enforcement_mode != SchemaEnforcementMode.NONE:
            data["schemaEnforcementMode"] = schema_enforcement_mode.value
        if field_constraints:
            data["fieldConstraints"] = [c.to_dict() for c in field_constraints]
        if indexing_mode != IndexingMode.ALL:
            data["indexingMode"] = indexing_mode.value
        if indexed_fields:
            data["indexedFields"] = indexed_fields

        payload = self._client._request("PUT", "/v1.0/collections", data=data)

        if payload:
            return Collection.from_dict(payload)
        return None

    def read_all(
        self,
        max_results: Optional[int] = None,
        skip: Optional[int] = None
    ) -> EnumerationResult:
        """
        Get all collections.

        Args:
            max_results: Optional page size (maxResults query param)
            skip: Optional number of records to skip (skip query param)

        Returns:
            An EnumerationResult whose ``objects`` are Collection instances
        """
        params: Dict[str, Any] = {}
        if max_results is not None:
            params["maxResults"] = max_results
        if skip is not None:
            params["skip"] = skip

        payload = self._client._request(
            "GET", "/v1.0/collections", params=params or None
        )

        if payload:
            return EnumerationResult.from_dict(payload, Collection.from_dict)
        return EnumerationResult()

    def read_by_id(self, collection_id: str) -> Optional[Collection]:
        """
        Get a collection by ID.

        Args:
            collection_id: The collection ID

        Returns:
            The Collection, or None if not found
        """
        try:
            payload = self._client._request("GET", f"/v1.0/collections/{collection_id}")
        except LatticeApiError as e:
            if e.status_code == 404:
                return None
            raise

        if payload:
            return Collection.from_dict(payload)
        return None

    def exists(self, collection_id: str) -> bool:
        """
        Check if a collection exists.

        Args:
            collection_id: The collection ID

        Returns:
            True if the collection exists
        """
        return self._client._request("HEAD", f"/v1.0/collections/{collection_id}")

    def delete(self, collection_id: str) -> bool:
        """
        Delete a collection.

        Args:
            collection_id: The collection ID

        Returns:
            True if the collection was deleted
        """
        try:
            self._client._request("DELETE", f"/v1.0/collections/{collection_id}")
            return True
        except LatticeApiError:
            return False

    def get_constraints(self, collection_id: str) -> List[FieldConstraint]:
        """
        Get field constraints for a collection.

        Args:
            collection_id: The collection ID

        Returns:
            List of field constraints
        """
        payload = self._client._request(
            "GET", f"/v1.0/collections/{collection_id}/constraints"
        )

        if payload:
            field_constraints = payload.get("fieldConstraints", [])
            if field_constraints:
                return [FieldConstraint.from_dict(c) for c in field_constraints]
        return []

    def update_constraints(
        self,
        collection_id: str,
        schema_enforcement_mode: SchemaEnforcementMode,
        field_constraints: Optional[List[FieldConstraint]] = None
    ) -> bool:
        """
        Update constraints for a collection.

        Args:
            collection_id: The collection ID
            schema_enforcement_mode: The enforcement mode
            field_constraints: Optional list of constraints

        Returns:
            True if the update was successful
        """
        data = {
            "schemaEnforcementMode": schema_enforcement_mode.value
        }
        if field_constraints:
            data["fieldConstraints"] = [c.to_dict() for c in field_constraints]

        self._client._request(
            "PUT", f"/v1.0/collections/{collection_id}/constraints", data=data
        )
        return True

    def get_indexed_fields(self, collection_id: str) -> List[IndexedField]:
        """
        Get indexed fields for a collection.

        Args:
            collection_id: The collection ID

        Returns:
            List of indexed fields
        """
        payload = self._client._request(
            "GET", f"/v1.0/collections/{collection_id}/indexing"
        )

        if payload:
            indexed_fields = payload.get("indexedFields", [])
            if indexed_fields:
                return [IndexedField.from_dict(f) for f in indexed_fields]
        return []

    def update_indexing(
        self,
        collection_id: str,
        indexing_mode: IndexingMode,
        indexed_fields: Optional[List[str]] = None,
        rebuild_indexes: bool = False
    ) -> bool:
        """
        Update indexing configuration for a collection.

        Args:
            collection_id: The collection ID
            indexing_mode: The indexing mode
            indexed_fields: Optional list of field paths to index
            rebuild_indexes: Whether to rebuild indexes after update

        Returns:
            True if the update was successful
        """
        data = {
            "indexingMode": indexing_mode.value,
            "rebuildIndexes": rebuild_indexes
        }
        if indexed_fields:
            data["indexedFields"] = indexed_fields

        self._client._request(
            "PUT", f"/v1.0/collections/{collection_id}/indexing", data=data
        )
        return True

    def rebuild_indexes(
        self,
        collection_id: str,
        drop_unused_indexes: bool = True,
        progress_callback: Optional[Callable[[int, int], None]] = None
    ) -> Optional[IndexRebuildResult]:
        """
        Rebuild indexes for a collection.

        Args:
            collection_id: The collection ID
            drop_unused_indexes: Whether to drop unused index tables
            progress_callback: Optional callback for progress updates

        Returns:
            IndexRebuildResult with details about the operation
        """
        data = {"dropUnusedIndexes": drop_unused_indexes}

        payload = self._client._request(
            "POST", f"/v1.0/collections/{collection_id}/indexes/rebuild", data=data
        )

        if payload:
            return IndexRebuildResult.from_dict(payload)
        return None


class DocumentMethods:
    """Methods for managing documents."""

    def __init__(self, client: LatticeClient):
        self._client = client

    def ingest(
        self,
        collection_id: str,
        content: Any,
        name: Optional[str] = None,
        labels: Optional[List[str]] = None,
        tags: Optional[Dict[str, str]] = None
    ) -> Optional[Document]:
        """
        Ingest a new document into a collection.

        Args:
            collection_id: The collection ID
            content: The document content (will be serialized to JSON)
            name: Optional document name
            labels: Optional list of labels
            tags: Optional dictionary of tags

        Returns:
            The created Document, or None if ingestion failed
        """
        data = {"content": content}

        if name:
            data["name"] = name
        if labels:
            data["labels"] = labels
        if tags:
            data["tags"] = tags

        payload = self._client._request(
            "PUT", f"/v1.0/collections/{collection_id}/documents", data=data
        )

        if payload:
            return Document.from_dict(payload)
        return None

    def ingest_batch(
        self,
        collection_id: str,
        documents: List['BatchIngestDocument']
    ) -> Optional[List[Document]]:
        """
        Ingest multiple documents into a collection in a single batch operation.

        Args:
            collection_id: The collection ID
            documents: List of BatchIngestDocument objects to ingest

        Returns:
            List of created Documents, or None if ingestion failed
        """
        data = {
            "documents": [doc.to_dict() for doc in documents]
        }

        payload = self._client._request(
            "PUT", f"/v1.0/collections/{collection_id}/documents/batch", data=data
        )

        if payload:
            return [Document.from_dict(d) for d in payload]
        return None

    def read_all_in_collection(
        self,
        collection_id: str,
        include_content: bool = False,
        include_labels: bool = True,
        include_tags: bool = True,
        max_results: Optional[int] = None,
        skip: Optional[int] = None
    ) -> EnumerationResult:
        """
        Get all documents in a collection.

        Args:
            collection_id: The collection ID
            include_content: Whether to include document content
            include_labels: Whether to include labels
            include_tags: Whether to include tags
            max_results: Optional page size (maxResults query param)
            skip: Optional number of records to skip (skip query param)

        Returns:
            An EnumerationResult whose ``objects`` are Document instances
        """
        params: Dict[str, Any] = {
            "includeContent": str(include_content).lower(),
            "includeLabels": str(include_labels).lower(),
            "includeTags": str(include_tags).lower()
        }
        if max_results is not None:
            params["maxResults"] = max_results
        if skip is not None:
            params["skip"] = skip

        payload = self._client._request(
            "GET", f"/v1.0/collections/{collection_id}/documents", params=params
        )

        if payload:
            return EnumerationResult.from_dict(payload, Document.from_dict)
        return EnumerationResult()

    def read_by_id(
        self,
        collection_id: str,
        document_id: str,
        include_content: bool = False,
        include_labels: bool = True,
        include_tags: bool = True
    ) -> Optional[Document]:
        """
        Get a document by ID.

        Args:
            collection_id: The collection ID
            document_id: The document ID
            include_content: Whether to include document content
            include_labels: Whether to include labels
            include_tags: Whether to include tags

        Returns:
            The Document, or None if not found
        """
        # First, get document metadata (without content)
        params = {
            "includeContent": "false",
            "includeLabels": str(include_labels).lower(),
            "includeTags": str(include_tags).lower()
        }

        try:
            payload = self._client._request(
                "GET", f"/v1.0/collections/{collection_id}/documents/{document_id}", params=params
            )
        except LatticeApiError as e:
            if e.status_code == 404:
                return None
            raise

        if not payload:
            return None

        document = Document.from_dict(payload)

        # If content is requested, make a separate call to get the raw content
        # The server returns raw JSON when includeContent=true
        if include_content and document is not None:
            content_response = self._client._session.get(
                f"{self._client.base_url}/v1.0/collections/{collection_id}/documents/{document_id}",
                params={"includeContent": "true"},
                timeout=self._client.timeout
            )
            if content_response.status_code == 200:
                try:
                    document.content = content_response.json()
                except json.JSONDecodeError:
                    document.content = content_response.text

        return document

    def exists(self, collection_id: str, document_id: str) -> bool:
        """
        Check if a document exists.

        Args:
            collection_id: The collection ID
            document_id: The document ID

        Returns:
            True if the document exists
        """
        return self._client._request("HEAD", f"/v1.0/collections/{collection_id}/documents/{document_id}")

    def delete(self, collection_id: str, document_id: str) -> bool:
        """
        Delete a document.

        Args:
            collection_id: The collection ID
            document_id: The document ID

        Returns:
            True if the document was deleted
        """
        try:
            self._client._request("DELETE", f"/v1.0/collections/{collection_id}/documents/{document_id}")
            return True
        except LatticeApiError:
            return False


class SearchMethods:
    """Methods for searching documents."""

    def __init__(self, client: LatticeClient):
        self._client = client

    def search(self, query: SearchQuery) -> Optional[SearchResult]:
        """
        Search for documents.

        Args:
            query: The search query

        Returns:
            SearchResult with matching documents
        """
        payload = self._client._request(
            "POST",
            f"/v1.0/collections/{query.collection_id}/documents/search",
            data=query.to_dict()
        )

        if payload:
            return SearchResult.from_dict(payload)
        return None

    def search_by_sql(
        self,
        collection_id: str,
        sql_expression: str
    ) -> Optional[SearchResult]:
        """
        Search documents using a SQL-like expression.

        Args:
            collection_id: The collection ID
            sql_expression: The SQL-like query expression

        Returns:
            SearchResult with matching documents
        """
        data = {"sqlExpression": sql_expression}

        payload = self._client._request(
            "POST",
            f"/v1.0/collections/{collection_id}/documents/search",
            data=data
        )

        if payload:
            return SearchResult.from_dict(payload)
        return None

    def enumerate(self, query: SearchQuery) -> Optional[SearchResult]:
        """
        Enumerate documents in a collection.

        Args:
            query: The search query (filters optional)

        Returns:
            SearchResult with documents
        """
        # Enumeration uses the same endpoint as search
        return self.search(query)


class SchemaMethods:
    """Methods for managing schemas."""

    def __init__(self, client: LatticeClient):
        self._client = client

    def read_all(
        self,
        max_results: Optional[int] = None,
        skip: Optional[int] = None
    ) -> EnumerationResult:
        """
        Get all schemas.

        Args:
            max_results: Optional page size (maxResults query param)
            skip: Optional number of records to skip (skip query param)

        Returns:
            An EnumerationResult whose ``objects`` are Schema instances
        """
        params: Dict[str, Any] = {}
        if max_results is not None:
            params["maxResults"] = max_results
        if skip is not None:
            params["skip"] = skip

        payload = self._client._request(
            "GET", "/v1.0/schemas", params=params or None
        )

        if payload:
            return EnumerationResult.from_dict(payload, Schema.from_dict)
        return EnumerationResult()

    def read_by_id(self, schema_id: str) -> Optional[Schema]:
        """
        Get a schema by ID.

        Args:
            schema_id: The schema ID

        Returns:
            The Schema, or None if not found
        """
        try:
            payload = self._client._request("GET", f"/v1.0/schemas/{schema_id}")
        except LatticeApiError as e:
            if e.status_code == 404:
                return None
            raise

        if payload:
            return Schema.from_dict(payload)
        return None

    def get_elements(
        self,
        schema_id: str,
        max_results: Optional[int] = None,
        skip: Optional[int] = None
    ) -> EnumerationResult:
        """
        Get elements for a schema.

        Args:
            schema_id: The schema ID
            max_results: Optional page size (maxResults query param)
            skip: Optional number of records to skip (skip query param)

        Returns:
            An EnumerationResult whose ``objects`` are SchemaElement instances
        """
        params: Dict[str, Any] = {}
        if max_results is not None:
            params["maxResults"] = max_results
        if skip is not None:
            params["skip"] = skip

        payload = self._client._request(
            "GET", f"/v1.0/schemas/{schema_id}/elements", params=params or None
        )

        if payload:
            return EnumerationResult.from_dict(payload, SchemaElement.from_dict)
        return EnumerationResult()


class IndexMethods:
    """Methods for managing indexes."""

    def __init__(self, client: LatticeClient):
        self._client = client

    def get_mappings(
        self,
        max_results: Optional[int] = None,
        skip: Optional[int] = None
    ) -> EnumerationResult:
        """
        Get all index table mappings.

        Args:
            max_results: Optional page size (maxResults query param)
            skip: Optional number of records to skip (skip query param)

        Returns:
            An EnumerationResult whose ``objects`` are IndexTableMapping instances
        """
        params: Dict[str, Any] = {}
        if max_results is not None:
            params["maxResults"] = max_results
        if skip is not None:
            params["skip"] = skip

        payload = self._client._request(
            "GET", "/v1.0/tables", params=params or None
        )

        if payload:
            return EnumerationResult.from_dict(payload, IndexTableMapping.from_dict)
        return EnumerationResult()

    def get_table_entries(
        self,
        table_name: str,
        max_results: Optional[int] = None,
        skip: Optional[int] = None
    ) -> EnumerationResult:
        """
        Get entries from a specific index table.

        The entries are returned in the ``objects`` field and the total number
        of entries in the table is available via ``total_records``.

        Args:
            table_name: The name of the index table
            max_results: Optional page size (maxResults query param)
            skip: Optional number of records to skip (skip query param)

        Returns:
            An EnumerationResult whose ``objects`` are IndexTableEntry instances
        """
        params: Dict[str, Any] = {}
        if max_results is not None:
            params["maxResults"] = max_results
        if skip is not None:
            params["skip"] = skip

        payload = self._client._request(
            "GET", f"/v1.0/tables/{table_name}/entries", params=params or None
        )

        if payload:
            return EnumerationResult.from_dict(payload, IndexTableEntry.from_dict)
        return EnumerationResult()
