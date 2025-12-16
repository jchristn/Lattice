/**
 * Lattice SDK Exceptions
 *
 * Custom exceptions for the Lattice SDK.
 */

/**
 * Base exception for all Lattice SDK errors.
 */
export class LatticeError extends Error {
    public statusCode?: number;

    constructor(message: string, statusCode?: number) {
        super(message);
        this.name = "LatticeError";
        this.statusCode = statusCode;
    }
}

/**
 * Raised when unable to connect to the Lattice server.
 */
export class LatticeConnectionError extends LatticeError {
    public originalError?: Error;

    constructor(message: string, originalError?: Error) {
        super(message);
        this.name = "LatticeConnectionError";
        this.originalError = originalError;
    }
}

/**
 * Raised when the API returns an error response.
 */
export class LatticeApiError extends LatticeError {
    public errorMessage?: string;

    constructor(message: string, statusCode: number, errorMessage?: string) {
        super(message, statusCode);
        this.name = "LatticeApiError";
        this.errorMessage = errorMessage || message;
    }
}

/**
 * Raised when request validation fails.
 */
export class LatticeValidationError extends LatticeError {
    public field?: string;

    constructor(message: string, field?: string) {
        super(message);
        this.name = "LatticeValidationError";
        this.field = field;
    }
}
