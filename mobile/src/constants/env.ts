/**
 * Centralized environment configuration.
 *
 * Only `EXPO_PUBLIC_*` variables are inlined into the app bundle by Expo.
 * Values are loaded per-mode from `.env.development` / `.env.production`.
 * Never read `process.env` directly elsewhere — import from here.
 */

const DEFAULT_API_URL = "http://localhost:5001";

/** Base URL of the CapTap API (no trailing slash). */
export const API_BASE_URL = (process.env.EXPO_PUBLIC_API_URL ?? DEFAULT_API_URL).replace(
  /\/+$/,
  "",
);

/** True in development builds. Used to gate verbose request logging. */
export const IS_DEV = __DEV__;

/** Network timeout for API requests, in milliseconds. */
export const API_TIMEOUT_MS = 15_000;
