"""
Lattice SDK for Python

A comprehensive REST SDK for consuming a Lattice server.
"""

from .client import LatticeClient
from .models import (
    Collection,
    Document,
    BatchIngestDocument,
    Schema,
    SchemaElement,
    FieldConstraint,
    IndexedField,
    SearchResult,
    IndexRebuildResult,
    ResponseContext,
    IndexTableMapping,
    IndexTableEntry,
    EnumerationResult,
    SchemaEnforcementMode,
    IndexingMode,
    SearchCondition,
    EnumerationOrder,
    DataType
)
from .exceptions import (
    LatticeException,
    LatticeConnectionError,
    LatticeApiError,
    LatticeValidationError
)

__version__ = "0.3.0"
__all__ = [
    "LatticeClient",
    "Collection",
    "Document",
    "BatchIngestDocument",
    "Schema",
    "SchemaElement",
    "FieldConstraint",
    "IndexedField",
    "SearchResult",
    "IndexRebuildResult",
    "ResponseContext",
    "IndexTableMapping",
    "IndexTableEntry",
    "EnumerationResult",
    "SchemaEnforcementMode",
    "IndexingMode",
    "SearchCondition",
    "EnumerationOrder",
    "DataType",
    "LatticeException",
    "LatticeConnectionError",
    "LatticeApiError",
    "LatticeValidationError"
]
