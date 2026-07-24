/**
 * The single Axios instance for all CapTap API calls.
 *
 * Responsibilities:
 *  - Base URL, timeout, JSON headers (configured once here).
 *  - Attach the Authorization header from the session store.
 *  - Development-only request/response logging (no tokens or secrets).
 *  - Normalize every failure into an {@link ApiClientError}.
 *  - Transparently refresh on 401 and retry the original request once.
 */
import axios, {
  AxiosError,
  AxiosHeaders,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from "axios";
import { API_BASE_URL, API_TIMEOUT_MS, IS_DEV } from "@/constants/env";
import { ApiClientError, kindFromStatus } from "./errors";
import { session } from "./session";
import type { ApiResponse } from "./types";

type RetryableConfig = InternalAxiosRequestConfig & { _retry?: boolean };

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: API_TIMEOUT_MS,
  headers: { "Content-Type": "application/json", Accept: "application/json" },
});

function isAuthRoute(url?: string): boolean {
  return Boolean(url && url.includes("/auth/"));
}

// ── Request: auth header + dev logging ──────────────────────────────────
apiClient.interceptors.request.use(async (config) => {
  const token = session.getAccessToken();
  const headers = AxiosHeaders.from(config.headers);
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  try {
    const { getDeviceId } = await import("@/utils/deviceId");
    headers.set("X-Device-Id", await getDeviceId());
  } catch {
    // Device id is optional for audit.
  }

  config.headers = headers;

  if (IS_DEV) {
    // Never log headers/bodies — they can contain tokens or passwords.
    console.log(`[api] → ${config.method?.toUpperCase()} ${config.url}`);
  }

  return config;
});

// ── Response: dev logging + 401 refresh + error normalization ───────────
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    if (IS_DEV) {
      console.log(`[api] ← ${response.status} ${response.config.url}`);
    }
    return response;
  },
  async (error: AxiosError<ApiResponse<unknown>>) => {
    const original = error.config as RetryableConfig | undefined;

    // Attempt a single transparent refresh on 401 (except for auth routes).
    if (
      error.response?.status === 401 &&
      original &&
      !original._retry &&
      !isAuthRoute(original.url)
    ) {
      original._retry = true;
      const newToken = await session.refresh();
      if (newToken) {
        const headers = AxiosHeaders.from(original.headers);
        headers.set("Authorization", `Bearer ${newToken}`);
        original.headers = headers;
        return apiClient(original);
      }
    }

    throw normalizeError(error);
  },
);

/** Convert any Axios failure into a typed {@link ApiClientError}. */
export function normalizeError(error: unknown): ApiClientError {
  if (error instanceof ApiClientError) return error;

  const axiosError = error as AxiosError<ApiResponse<unknown>>;

  if (axiosError.code === "ECONNABORTED") {
    return new ApiClientError({ kind: "timeout", message: "Request timed out." });
  }

  if (!axiosError.response) {
    return new ApiClientError({
      kind: "network",
      message: "Network request failed.",
    });
  }

  const status = axiosError.response.status;
  const body = axiosError.response.data;
  const backend = body?.error;

  return new ApiClientError({
    kind: kindFromStatus(status),
    status,
    code: backend?.code,
    message: backend?.message ?? body?.message ?? `Request failed (${status}).`,
  });
}

/** Unwrap an `ApiResponse<T>` body, throwing on an unsuccessful payload. */
export function unwrap<T>(response: AxiosResponse<ApiResponse<T>>): T {
  const body = response.data;
  if (!body.success || body.data === undefined || body.data === null) {
    throw new ApiClientError({
      kind: body.error ? kindFromStatus(response.status) : "unknown",
      status: response.status,
      code: body.error?.code,
      message: body.error?.message ?? body.message ?? "Unexpected response.",
    });
  }
  return body.data;
}
