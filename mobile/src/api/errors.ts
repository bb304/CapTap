/**
 * Centralized API error model.
 *
 * The Axios client normalizes every failure into an {@link ApiClientError}
 * so screens and hooks never inspect raw Axios internals. UI copy is derived
 * via {@link toUserMessage} for consistent, non-sensitive messaging.
 */

export type ApiErrorKind =
  | "network"
  | "timeout"
  | "unauthorized"
  | "forbidden"
  | "notFound"
  | "validation"
  | "conflict"
  | "rateLimited"
  | "server"
  | "unknown";

export class ApiClientError extends Error {
  readonly kind: ApiErrorKind;
  /** HTTP status, when a response was received. */
  readonly status?: number;
  /** Backend error code (e.g. VALIDATION_ERROR), when provided. */
  readonly code?: string;

  constructor(params: { kind: ApiErrorKind; message: string; status?: number; code?: string }) {
    super(params.message);
    this.name = "ApiClientError";
    this.kind = params.kind;
    this.status = params.status;
    this.code = params.code;
  }
}

/** Map an HTTP status to a coarse error kind. */
export function kindFromStatus(status: number): ApiErrorKind {
  switch (status) {
    case 400:
      return "validation";
    case 401:
      return "unauthorized";
    case 403:
      return "forbidden";
    case 404:
      return "notFound";
    case 409:
      return "conflict";
    case 429:
      return "rateLimited";
    default:
      if (status >= 500) return "server";
      return "unknown";
  }
}

/**
 * Produce user-facing copy. Backend messages are surfaced only for safe,
 * client-actionable cases (validation / auth). Everything else uses generic
 * copy so we never leak server internals.
 */
export function toUserMessage(error: unknown): string {
  if (error instanceof ApiClientError) {
    switch (error.kind) {
      case "network":
        return "No internet connection. CapTap saved what it can offline and will sync when you're back.";
      case "timeout":
        return "That took too long. Please try again.";
      case "unauthorized":
        return error.message || "Your session has expired. Please log in again.";
      case "forbidden":
        return "You don't have access to that.";
      case "notFound":
        return "We couldn't find what you were looking for.";
      case "validation":
      case "conflict":
        return error.message || "Please check the details and try again.";
      case "rateLimited":
        return "Too many attempts. Please wait a moment and try again.";
      case "server":
        return "Something went wrong on our end. Please try again shortly.";
      default:
        return error.message || "Something went wrong. Please try again.";
    }
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return "Something went wrong. Please try again.";
}

/** True when the failure is transient and safe to auto-retry. */
export function isRetryableError(error: unknown): boolean {
  if (error instanceof ApiClientError) {
    return error.kind === "network" || error.kind === "timeout" || error.kind === "server";
  }
  return false;
}
