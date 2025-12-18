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
    ResponseContext,
    SearchQuery,
    SearchFilter,
    IndexTableMapping,
    SchemaEnforcementMode,
    IndexingMode,
    EnumerationOrder
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
    ) -> ResponseContext:
        """
        Make an HTTP request to the Lattice API.

        Args:
            method: HTTP method (GET, POST, PUT, DELETE, HEAD)
            path: API path (e.g., "/v1.0/collections")
            data: Request body data (for POST/PUT)
            params: Query parameters

        Returns:
            ResponseContext with the API response
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

            # For HEAD requests, we don't have a body
            if method.upper() == "HEAD":
                return ResponseContext(
                    success=response.status_code == 200,
                    status_code=response.status_code,
                    headers=dict(response.headers)
                )

            # Parse the response
            if response.content:
                try:
                    response_data = response.json()
                    return ResponseContext.from_dict(response_data)
                except json.JSONDecodeError:
                    return ResponseContext(
                        success=response.status_code < 400,
                        status_code=response.status_code,
                        data=response.text,
                        headers=dict(response.headers)
                    )
            else:
                return ResponseContext(
                    success=response.status_code < 400,
                    status_code=response.status_code,
                    headers=dict(response.headers)
                )

        except requests.ConnectionError as e:
            raise LatticeConnectionError(f"Failed to connect to {url}", e)
        except requests.Timeout as e:
            raise LatticeConnectionError(f"Request to {url} timed out", e)
        except requests.RequestException as e:
            raise LatticeException(f"Request failed: {str(e)}")

    def health_check(self) -> bool:
        """
        Check if the Lattice server is healthy.

        Returns:
            True if the server is healthy, False otherwise
        """
        try:
            response = self._request("GET", "/v1.0/health")
            return response.success
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

        response = self._client._request("PUT", "/v1.0/collections", data=data)

        if response.success and response.data:
            return Collection.from_dict(response.data)
        return None

    def read_all(self) -> List[Collection]:
        """
        Get all collections.

        Returns:
            List of all collections
        """
        response = self._client._request("GET", "/v1.0/collections")

        if response.success and response.data:
            return [Collection.from_dict(c) for c in response.data]
        return []

    def read_by_id(self, collection_id: str) -> Optional[Collection]:
        """
        Get a collection by ID.

        Args:
            collection_id: The collection ID

        Returns:
            The Collection, or None if not found
        """
        response = self._client._request("GET", f"/v1.0/collections/{collection_id}")

        if response.success and response.data:
            return Collection.from_dict(response.data)
        return None

    def exists(self, collection_id: str) -> bool:
        """
        Check if a collection exists.

        Args:
            collection_id: The collection ID

        Returns:
            True if the collection exists
        """
        response = self._client._request("HEAD", f"/v1.0/collections/{collection_id}")
        return response.success

    def delete(self, collection_id: str) -> bool:
        """
        Delete a collection.

        Args:
            collection_id: The collection ID

        Returns:
            True if the collection was deleted
        """
        response = self._client._request("DELETE", f"/v1.0/collections/{collection_id}")
        return response.success

    def get_constraints(self, collection_id: str) -> List[FieldConstraint]:
        """
        Get field constraints for a collection.

        Args:
            collection_id: The collection ID

        Returns:
            List of field constraints
        """
        response = self._client._request(
            "GET", f"/v1.0/collections/{collection_id}/constraints"
        )

        if response.success and response.data:
            field_constraints = response.data.get("fieldConstraints", [])
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

        response = self._client._request(
            "PUT", f"/v1.0/collections/{collection_id}/constraints", data=data
        )
        return response.success

    def get_indexed_fields(self, collection_id: str) -> List[IndexedField]:
        """
        Get indexed fields for a collection.

        Args:
            collection_id: The collection ID

        Returns:
            List of indexed fields
        """
        response = self._client._request(
            "GET", f"/v1.0/collections/{collection_id}/indexing"
        )

        if response.success and response.data:
            indexed_fields = response.data.get("indexedFields", [])
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

        response = self._client._request(
            "PUT", f"/v1.0/collections/{collection_id}/indexing", data=data
        )
        return response.success

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

        response = self._client._request(
            "POST", f"/v1.0/collections/{collection_id}/indexes/rebuild", data=data
        )

        if response.success and response.data:
            return IndexRebuildResult.from_dict(response.data)
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

        response = self._client._request(
            "PUT", f"/v1.0/collections/{collection_id}/documents", data=data
        )

        if response.success and response.data:
            return Document.from_dict(response.data)
        return None

    def read_all_in_collection(
        self,
        collection_id: str,
        include_content: bool = False,
        include_labels: bool = True,
        include_tags: bool = True
    ) -> List[Document]:
        """
        Get all documents in a collection.

        Args:
            collection_id: The collection ID
            include_content: Whether to include document content
            include_labels: Whether to include labels
            include_tags: Whether to include tags

        Returns:
            List of documents
        """
        params = {
            "includeContent": str(include_content).lower(),
            "includeLabels": str(include_labels).lower(),
            "includeTags": str(include_tags).lower()
        }

        response = self._client._request(
            "GET", f"/v1.0/collections/{collection_id}/documents", params=params
        )

        if response.success and response.data:
            return [Document.from_dict(d) for d in response.data]
        return []

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

        response = self._client._request(
            "GET", f"/v1.0/collections/{collection_id}/documents/{document_id}", params=params
        )

        if not response.success or not response.data:
            return None

        document = Document.from_dict(response.data)

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
        response = self._client._request("HEAD", f"/v1.0/collections/{collection_id}/documents/{document_id}")
        return response.success

    def delete(self, collection_id: str, document_id: str) -> bool:
        """
        Delete a document.

        Args:
            collection_id: The collection ID
            document_id: The document ID

        Returns:
            True if the document was deleted
        """
        response = self._client._request("DELETE", f"/v1.0/collections/{collection_id}/documents/{document_id}")
        return response.success


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
        response = self._client._request(
            "POST",
            f"/v1.0/collections/{query.collection_id}/documents/search",
            data=query.to_dict()
        )

        if response.success and response.data:
            return SearchResult.from_dict(response.data)
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

        response = self._client._request(
            "POST",
            f"/v1.0/collections/{collection_id}/documents/search",
            data=data
        )

        if response.success and response.data:
            return SearchResult.from_dict(response.data)
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

    def read_all(self) -> List[Schema]:
        """
        Get all schemas.

        Returns:
            List of all schemas
        """
        response = self._client._request("GET", "/v1.0/schemas")

        if response.success and response.data:
            return [Schema.from_dict(s) for s in response.data]
        return []

    def read_by_id(self, schema_id: str) -> Optional[Schema]:
        """
        Get a schema by ID.

        Args:
            schema_id: The schema ID

        Returns:
            The Schema, or None if not found
        """
        response = self._client._request("GET", f"/v1.0/schemas/{schema_id}")

        if response.success and response.data:
            return Schema.from_dict(response.data)
        return None

    def get_elements(self, schema_id: str) -> List[SchemaElement]:
        """
        Get elements for a schema.

        Args:
            schema_id: The schema ID

        Returns:
            List of schema elements
        """
        response = self._client._request("GET", f"/v1.0/schemas/{schema_id}/elements")

        if response.success and response.data:
            return [SchemaElement.from_dict(e) for e in response.data]
        return []


class IndexMethods:
    """Methods for managing indexes."""

    def __init__(self, client: LatticeClient):
        self._client = client

    def get_mappings(self) -> List[IndexTableMapping]:
        """
        Get all index table mappings.

        Returns:
            List of index table mappings
        """
        response = self._client._request("GET", "/v1.0/tables")

        if response.success and response.data:
            return [IndexTableMapping.from_dict(m) for m in response.data]
        return []
