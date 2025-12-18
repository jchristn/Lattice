"""
Lattice SDK Models

Data models for the Lattice REST API.
"""

from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Optional, List, Dict, Any


class SchemaEnforcementMode(str, Enum):
    """Schema enforcement mode for collections."""
    NONE = "None"
    STRICT = "Strict"
    FLEXIBLE = "Flexible"
    PARTIAL = "Partial"


class IndexingMode(str, Enum):
    """Indexing mode for collections."""
    ALL = "All"
    SELECTIVE = "Selective"
    NONE = "None"


class SearchCondition(str, Enum):
    """Search condition operators."""
    EQUALS = "Equals"
    NOT_EQUALS = "NotEquals"
    GREATER_THAN = "GreaterThan"
    GREATER_THAN_OR_EQUAL_TO = "GreaterThanOrEqualTo"
    LESS_THAN = "LessThan"
    LESS_THAN_OR_EQUAL_TO = "LessThanOrEqualTo"
    IS_NULL = "IsNull"
    IS_NOT_NULL = "IsNotNull"
    CONTAINS = "Contains"
    STARTS_WITH = "StartsWith"
    ENDS_WITH = "EndsWith"
    LIKE = "Like"


class EnumerationOrder(str, Enum):
    """Enumeration ordering options."""
    CREATED_ASCENDING = "CreatedAscending"
    CREATED_DESCENDING = "CreatedDescending"
    LAST_UPDATE_ASCENDING = "LastUpdateAscending"
    LAST_UPDATE_DESCENDING = "LastUpdateDescending"
    NAME_ASCENDING = "NameAscending"
    NAME_DESCENDING = "NameDescending"


class DataType(str, Enum):
    """Data types for field constraints and schema elements."""
    STRING = "string"
    INTEGER = "integer"
    NUMBER = "number"
    BOOLEAN = "boolean"
    ARRAY = "array"
    OBJECT = "object"
    NULL = "null"


@dataclass
class Collection:
    """Represents a Lattice collection."""
    id: str = ""
    name: str = ""
    description: Optional[str] = None
    documents_directory: Optional[str] = None
    labels: List[str] = field(default_factory=list)
    tags: Dict[str, str] = field(default_factory=dict)
    created_utc: Optional[datetime] = None
    last_update_utc: Optional[datetime] = None
    schema_enforcement_mode: SchemaEnforcementMode = SchemaEnforcementMode.NONE
    indexing_mode: IndexingMode = IndexingMode.ALL

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Collection":
        """Create a Collection from a dictionary."""
        if data is None:
            return None
        return cls(
            id=data.get("id", ""),
            name=data.get("name", ""),
            description=data.get("description"),
            documents_directory=data.get("documentsDirectory"),
            labels=data.get("labels", []) or [],
            tags=data.get("tags", {}) or {},
            created_utc=_parse_datetime(data.get("createdUtc")),
            last_update_utc=_parse_datetime(data.get("lastUpdateUtc")),
            schema_enforcement_mode=_parse_schema_enforcement_mode(data.get("schemaEnforcementMode", "None")),
            indexing_mode=_parse_indexing_mode(data.get("indexingMode", "All"))
        )


@dataclass
class Document:
    """Represents a Lattice document."""
    id: str = ""
    collection_id: str = ""
    schema_id: str = ""
    name: Optional[str] = None
    labels: List[str] = field(default_factory=list)
    tags: Dict[str, str] = field(default_factory=dict)
    created_utc: Optional[datetime] = None
    last_update_utc: Optional[datetime] = None
    content: Optional[Any] = None
    content_length: int = 0
    sha256_hash: Optional[str] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Document":
        """Create a Document from a dictionary."""
        if data is None:
            return None
        return cls(
            id=data.get("id", ""),
            collection_id=data.get("collectionId", ""),
            schema_id=data.get("schemaId", ""),
            name=data.get("name"),
            labels=data.get("labels", []) or [],
            tags=data.get("tags", {}) or {},
            created_utc=_parse_datetime(data.get("createdUtc")),
            last_update_utc=_parse_datetime(data.get("lastUpdateUtc")),
            content=data.get("content"),
            content_length=data.get("contentLength", 0),
            sha256_hash=data.get("sha256Hash")
        )


@dataclass
class Schema:
    """Represents a Lattice schema."""
    id: str = ""
    name: Optional[str] = None
    hash: Optional[str] = None
    created_utc: Optional[datetime] = None
    last_update_utc: Optional[datetime] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Schema":
        """Create a Schema from a dictionary."""
        if data is None:
            return None
        return cls(
            id=data.get("id", ""),
            name=data.get("name"),
            hash=data.get("hash"),
            created_utc=_parse_datetime(data.get("createdUtc")),
            last_update_utc=_parse_datetime(data.get("lastUpdateUtc"))
        )


@dataclass
class SchemaElement:
    """Represents an element within a schema."""
    id: str = ""
    schema_id: str = ""
    position: int = 0
    key: str = ""
    data_type: str = ""
    nullable: bool = False
    created_utc: Optional[datetime] = None
    last_update_utc: Optional[datetime] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "SchemaElement":
        """Create a SchemaElement from a dictionary."""
        if data is None:
            return None
        return cls(
            id=data.get("id", ""),
            schema_id=data.get("schemaId", ""),
            position=data.get("position", 0),
            key=data.get("key", ""),
            data_type=data.get("dataType", ""),
            nullable=data.get("nullable", False),
            created_utc=_parse_datetime(data.get("createdUtc")),
            last_update_utc=_parse_datetime(data.get("lastUpdateUtc"))
        )


@dataclass
class FieldConstraint:
    """Represents a field constraint for schema validation."""
    id: str = ""
    collection_id: str = ""
    field_path: str = ""
    data_type: Optional[str] = None
    required: bool = False
    nullable: bool = True
    regex_pattern: Optional[str] = None
    min_value: Optional[float] = None
    max_value: Optional[float] = None
    min_length: Optional[int] = None
    max_length: Optional[int] = None
    allowed_values: Optional[List[str]] = None
    array_element_type: Optional[str] = None
    created_utc: Optional[datetime] = None
    last_update_utc: Optional[datetime] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "FieldConstraint":
        """Create a FieldConstraint from a dictionary."""
        if data is None:
            return None
        return cls(
            id=data.get("id", ""),
            collection_id=data.get("collectionId", ""),
            field_path=data.get("fieldPath", ""),
            data_type=data.get("dataType"),
            required=data.get("required", False),
            nullable=data.get("nullable", True),
            regex_pattern=data.get("regexPattern"),
            min_value=data.get("minValue"),
            max_value=data.get("maxValue"),
            min_length=data.get("minLength"),
            max_length=data.get("maxLength"),
            allowed_values=data.get("allowedValues"),
            array_element_type=data.get("arrayElementType"),
            created_utc=_parse_datetime(data.get("createdUtc")),
            last_update_utc=_parse_datetime(data.get("lastUpdateUtc"))
        )

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for API requests."""
        result = {
            "fieldPath": self.field_path
        }
        if self.data_type:
            result["dataType"] = self.data_type
        if self.required:
            result["required"] = self.required
        if not self.nullable:
            result["nullable"] = self.nullable
        if self.regex_pattern:
            result["regexPattern"] = self.regex_pattern
        if self.min_value is not None:
            result["minValue"] = self.min_value
        if self.max_value is not None:
            result["maxValue"] = self.max_value
        if self.min_length is not None:
            result["minLength"] = self.min_length
        if self.max_length is not None:
            result["maxLength"] = self.max_length
        if self.allowed_values:
            result["allowedValues"] = self.allowed_values
        if self.array_element_type:
            result["arrayElementType"] = self.array_element_type
        return result


@dataclass
class IndexedField:
    """Represents an indexed field in a collection."""
    id: str = ""
    collection_id: str = ""
    field_path: str = ""
    created_utc: Optional[datetime] = None
    last_update_utc: Optional[datetime] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "IndexedField":
        """Create an IndexedField from a dictionary."""
        if data is None:
            return None
        return cls(
            id=data.get("id", ""),
            collection_id=data.get("collectionId", ""),
            field_path=data.get("fieldPath", ""),
            created_utc=_parse_datetime(data.get("createdUtc")),
            last_update_utc=_parse_datetime(data.get("lastUpdateUtc"))
        )


@dataclass
class SearchFilter:
    """Represents a search filter."""
    field: str
    condition: SearchCondition
    value: Optional[str] = None

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for API requests."""
        result = {
            "field": self.field,
            "condition": self.condition.value
        }
        if self.value is not None:
            result["value"] = self.value
        return result


@dataclass
class SearchQuery:
    """Represents a search query."""
    collection_id: str
    filters: Optional[List[SearchFilter]] = None
    labels: Optional[List[str]] = None
    tags: Optional[Dict[str, str]] = None
    max_results: Optional[int] = None
    skip: Optional[int] = None
    ordering: Optional[EnumerationOrder] = None
    include_content: bool = False

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for API requests."""
        result = {}
        if self.filters:
            result["filters"] = [f.to_dict() for f in self.filters]
        if self.labels:
            result["labels"] = self.labels
        if self.tags:
            result["tags"] = self.tags
        if self.max_results is not None:
            result["maxResults"] = self.max_results
        if self.skip is not None:
            result["skip"] = self.skip
        if self.ordering:
            result["ordering"] = self.ordering.value
        if self.include_content:
            result["includeContent"] = self.include_content
        return result


@dataclass
class SearchResult:
    """Represents search results."""
    success: bool = False
    timestamp: Optional[datetime] = None
    max_results: Optional[int] = None
    continuation_token: Optional[str] = None
    end_of_results: bool = False
    total_records: int = 0
    records_remaining: int = 0
    documents: List[Document] = field(default_factory=list)

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "SearchResult":
        """Create a SearchResult from a dictionary."""
        if data is None:
            return None
        docs = []
        if data.get("documents"):
            docs = [Document.from_dict(d) for d in data["documents"]]

        timestamp = None
        if data.get("timestamp"):
            ts = data["timestamp"]
            if isinstance(ts, dict):
                timestamp = _parse_datetime(ts.get("utc"))
            else:
                timestamp = _parse_datetime(ts)

        return cls(
            success=data.get("success", False),
            timestamp=timestamp,
            max_results=data.get("maxResults"),
            continuation_token=data.get("continuationToken"),
            end_of_results=data.get("endOfResults", False),
            total_records=data.get("totalRecords", 0),
            records_remaining=data.get("recordsRemaining", 0),
            documents=docs
        )


@dataclass
class IndexRebuildResult:
    """Represents the result of an index rebuild operation."""
    collection_id: str = ""
    documents_processed: int = 0
    indexes_created: List[str] = field(default_factory=list)
    indexes_dropped: List[str] = field(default_factory=list)
    values_inserted: int = 0
    duration: Optional[str] = None
    duration_ms: float = 0
    errors: List[str] = field(default_factory=list)
    success: bool = False

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "IndexRebuildResult":
        """Create an IndexRebuildResult from a dictionary."""
        if data is None:
            return None
        return cls(
            collection_id=data.get("collectionId", ""),
            documents_processed=data.get("documentsProcessed", 0),
            indexes_created=data.get("indexesCreated", []) or [],
            indexes_dropped=data.get("indexesDropped", []) or [],
            values_inserted=data.get("valuesInserted", 0),
            duration=data.get("duration"),
            duration_ms=data.get("durationMs", 0),
            errors=data.get("errors", []) or [],
            success=data.get("success", False)
        )


@dataclass
class ResponseContext:
    """Represents the standard API response wrapper."""
    success: bool = False
    status_code: int = 0
    error_message: Optional[str] = None
    data: Optional[Any] = None
    headers: Dict[str, str] = field(default_factory=dict)
    processing_time_ms: float = 0
    guid: Optional[str] = None
    timestamp_utc: Optional[datetime] = None

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "ResponseContext":
        """Create a ResponseContext from a dictionary."""
        if data is None:
            return None
        return cls(
            success=data.get("success", False),
            status_code=data.get("statusCode", 0),
            error_message=data.get("errorMessage"),
            data=data.get("data"),
            headers=data.get("headers", {}) or {},
            processing_time_ms=data.get("processingTimeMs", 0),
            guid=data.get("guid"),
            timestamp_utc=_parse_datetime(data.get("timestampUtc"))
        )


@dataclass
class IndexTableMapping:
    """Represents an index table mapping."""
    key: str = ""
    table_name: str = ""

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "IndexTableMapping":
        """Create an IndexTableMapping from a dictionary."""
        if data is None:
            return None
        return cls(
            key=data.get("key", ""),
            table_name=data.get("tableName", "")
        )


def _parse_datetime(value: Any) -> Optional[datetime]:
    """Parse a datetime value from various formats."""
    if value is None:
        return None
    if isinstance(value, datetime):
        return value
    if isinstance(value, str):
        # Try parsing ISO format
        try:
            # Handle formats like "2024-01-15T10:30:00Z" or "2024-01-15T10:30:00.123Z"
            value = value.rstrip("Z")
            if "." in value:
                return datetime.fromisoformat(value)
            else:
                return datetime.fromisoformat(value)
        except ValueError:
            return None
    return None


def _parse_schema_enforcement_mode(value: Any) -> SchemaEnforcementMode:
    """Parse a SchemaEnforcementMode from various formats."""
    if value is None:
        return SchemaEnforcementMode.NONE
    if isinstance(value, SchemaEnforcementMode):
        return value
    if isinstance(value, str):
        # Handle case-insensitive matching
        value_lower = value.lower()
        if value_lower == "none":
            return SchemaEnforcementMode.NONE
        elif value_lower == "strict":
            return SchemaEnforcementMode.STRICT
        elif value_lower == "flexible":
            return SchemaEnforcementMode.FLEXIBLE
        elif value_lower == "partial":
            return SchemaEnforcementMode.PARTIAL
    return SchemaEnforcementMode.NONE


def _parse_indexing_mode(value: Any) -> IndexingMode:
    """Parse an IndexingMode from various formats."""
    if value is None:
        return IndexingMode.ALL
    if isinstance(value, IndexingMode):
        return value
    if isinstance(value, str):
        # Handle case-insensitive matching
        value_lower = value.lower()
        if value_lower == "all":
            return IndexingMode.ALL
        elif value_lower == "selective":
            return IndexingMode.SELECTIVE
        elif value_lower == "none":
            return IndexingMode.NONE
    return IndexingMode.ALL
