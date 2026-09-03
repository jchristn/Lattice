"""
Lattice SDK Exceptions

Custom exceptions for the Lattice SDK.
"""

from typing import Optional


class LatticeException(Exception):
    """Base exception for all Lattice SDK errors."""

    def __init__(self, message: str, status_code: Optional[int] = None):
        super().__init__(message)
        self.message = message
        self.status_code = status_code


class LatticeConnectionError(LatticeException):
    """Raised when unable to connect to the Lattice server."""

    def __init__(self, message: str, original_error: Optional[Exception] = None):
        super().__init__(message)
        self.original_error = original_error


class LatticeApiError(LatticeException):
    """Raised when the API returns an error response.

    The new Lattice error contract is a raw JSON body of the shape
    ``{ "error": "<message>", "detail"?: <structured> }`` accompanied by a
    non-2xx HTTP status code. ``detail`` carries any structured error
    information (e.g. schema validation errors, lock metadata) and
    ``request_id`` is populated from the ``X-Lattice-Request-Id`` response
    header when present.
    """

    def __init__(
        self,
        message: str,
        status_code: int,
        error_message: Optional[str] = None,
        detail: Optional[object] = None,
        request_id: Optional[str] = None,
    ):
        super().__init__(message, status_code)
        self.error_message = error_message or message
        self.detail = detail
        self.request_id = request_id


class LatticeValidationError(LatticeException):
    """Raised when request validation fails."""

    def __init__(self, message: str, field: Optional[str] = None):
        super().__init__(message)
        self.field = field
