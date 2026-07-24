/**
 * Session / token manager.
 *
 * Single source of truth for auth tokens. Tokens live in memory for fast
 * synchronous access by the Axios interceptor and are persisted to
 * Expo SecureStore (never AsyncStorage). UI components never touch this module
 * directly — they consume {@link AuthContext}, which wraps it in React state.
 *
 * The refresh call is de-duplicated (single-flight) so concurrent 401s trigger
 * exactly one network refresh.
 */
import axios from "axios";
import * as SecureStore from "expo-secure-store";
import { API_BASE_URL, API_TIMEOUT_MS } from "@/constants/env";
import type { ApiResponse, AuthTokens } from "./types";

const ACCESS_TOKEN_KEY = "captap.accessToken";
const REFRESH_TOKEN_KEY = "captap.refreshToken";
const REFRESH_ENDPOINT = "/api/v1/auth/refresh";

type ExpireListener = () => void;

let accessToken: string | null = null;
let refreshToken: string | null = null;
let inFlightRefresh: Promise<string | null> | null = null;
const expireListeners = new Set<ExpireListener>();

/**
 * Bare Axios instance used only for refresh, to avoid interceptor recursion.
 * Exported so tests can mock the refresh endpoint in isolation.
 */
export const refreshClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: API_TIMEOUT_MS,
  headers: { "Content-Type": "application/json", Accept: "application/json" },
});

export const session = {
  getAccessToken(): string | null {
    return accessToken;
  },

  getRefreshToken(): string | null {
    return refreshToken;
  },

  hasSession(): boolean {
    return Boolean(accessToken && refreshToken);
  },

  /** Persist a new token pair to memory + SecureStore. */
  async setTokens(tokens: AuthTokens): Promise<void> {
    accessToken = tokens.accessToken;
    refreshToken = tokens.refreshToken;
    await Promise.all([
      SecureStore.setItemAsync(ACCESS_TOKEN_KEY, tokens.accessToken),
      SecureStore.setItemAsync(REFRESH_TOKEN_KEY, tokens.refreshToken),
    ]);
    // require avoids circular ESM dynamic-import issues under Jest.
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    require("./tokenRefresh").scheduleProactiveRefresh();
  },

  /** Load tokens from SecureStore into memory (call on app start). */
  async restore(): Promise<boolean> {
    const [storedAccess, storedRefresh] = await Promise.all([
      SecureStore.getItemAsync(ACCESS_TOKEN_KEY),
      SecureStore.getItemAsync(REFRESH_TOKEN_KEY),
    ]);
    accessToken = storedAccess;
    refreshToken = storedRefresh;
    const ok = Boolean(accessToken && refreshToken);
    if (ok) {
      // eslint-disable-next-line @typescript-eslint/no-require-imports
      require("./tokenRefresh").scheduleProactiveRefresh();
    }
    return ok;
  },

  /** Clear memory + SecureStore. */
  async clear(): Promise<void> {
    accessToken = null;
    refreshToken = null;
    inFlightRefresh = null;
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    require("./tokenRefresh").cancelProactiveRefresh();
    await Promise.all([
      SecureStore.deleteItemAsync(ACCESS_TOKEN_KEY),
      SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY),
    ]);
  },

  /**
   * Exchange the refresh token for a new pair. De-duplicated so parallel
   * callers share one request. Returns the new access token, or null on
   * failure (session is cleared and listeners are notified).
   */
  async refresh(): Promise<string | null> {
    if (inFlightRefresh) return inFlightRefresh;

    const current = refreshToken;
    if (!current) {
      await this.expire();
      return null;
    }

    inFlightRefresh = (async () => {
      try {
        const { data } = await refreshClient.post<ApiResponse<AuthTokens>>(
          REFRESH_ENDPOINT,
          { refreshToken: current },
        );
        if (!data.success || !data.data) {
          await session.expire();
          return null;
        }
        await session.setTokens(data.data);
        return data.data.accessToken;
      } catch {
        await session.expire();
        return null;
      } finally {
        inFlightRefresh = null;
      }
    })();

    return inFlightRefresh;
  },

  /** Clear the session and notify listeners (e.g. to redirect to Login). */
  async expire(): Promise<void> {
    await this.clear();
    expireListeners.forEach((listener) => listener());
  },

  /** Subscribe to session-expiry. Returns an unsubscribe function. */
  onExpire(listener: ExpireListener): () => void {
    expireListeners.add(listener);
    return () => expireListeners.delete(listener);
  },
};
