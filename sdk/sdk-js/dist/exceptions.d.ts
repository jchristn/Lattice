/**
 * Lattice SDK Exceptions
 *
 * Custom exceptions for the Lattice SDK.
 */
/**
 * Base exception for all Lattice SDK errors.
 */
export declare class LatticeError extends Error {
    statusCode?: number;
    constructor(message: string, statusCode?: number);
}
/**
 * Raised when unable to connect to the Lattice server.
 */
export declare class LatticeConnectionError extends LatticeError {
    originalError?: Error;
    constructor(message: string, originalError?: Error);
}
/**
 * Raised when the API returns an error response.
 */
export declare class LatticeApiError extends LatticeError {
    errorMessage?: string;
    constructor(message: string, statusCode: number, errorMessage?: string);
}
/**
 * Raised when request validation fails.
 */
export declare class LatticeValidationError extends LatticeError {
    field?: string;
    constructor(message: string, field?: string);
}
