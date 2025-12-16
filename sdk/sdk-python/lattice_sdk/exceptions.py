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
    """Raised when the API returns an error response."""

    def __init__(self, message: str, status_code: int, error_message: Optional[str] = None):
        super().__init__(message, status_code)
        self.error_message = error_message or message


class LatticeValidationError(LatticeException):
    """Raised when request validation fails."""

    def __init__(self, message: str, field: Optional[str] = None):
        super().__init__(message)
        self.field = field
