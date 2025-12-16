"""
Lattice SDK for Python

A comprehensive REST SDK for consuming a Lattice server.
"""

from .client import LatticeClient
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

__version__ = "1.0.0"
__all__ = [
    "LatticeClient",
    "Collection",
    "Document",
    "Schema",
    "SchemaElement",
    "FieldConstraint",
    "IndexedField",
    "SearchResult",
    "IndexRebuildResult",
    "ResponseContext",
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
