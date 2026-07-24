/**
 * Proactive access-token refresh.
 *
 * Schedules a refresh shortly before the JWT `exp` claim so the first API
 * call after expiry rarely sees a 401. The Axios interceptor remains the
 * reactive fallback.
 */
import { decodeTokenClaims } from "@/utils/jwt";
import { session } from "./session";

/** Refresh this many seconds before the access token expires. */
const REFRESH_SKEW_SECONDS = 60;

/** setTimeout cannot accept delays above a 32-bit signed int. */
const MAX_TIMEOUT_MS = 2_147_483_647;

let timer: ReturnType<typeof setTimeout> | null = null;
let refreshing = false;

function clearTimer() {
  if (timer) {
    clearTimeout(timer);
    timer = null;
  }
}

/**
 * Schedule (or clear) a proactive refresh based on the current access token.
 * Call after login / restore / refresh; call with no session to clear.
 */
export function scheduleProactiveRefresh(): void {
  clearTimer();

  const token = session.getAccessToken();
  if (!token) return;

  const claims = decodeTokenClaims(token);
  if (!claims?.exp) return;

  const expiresAtMs = claims.exp * 1000;
  const refreshAtMs = expiresAtMs - REFRESH_SKEW_SECONDS * 1000;
  const rawDelay = refreshAtMs - Date.now();

  // Already inside the skew window (or past) — refresh once, then reschedule.
  if (rawDelay <= 0) {
    void runRefresh();
    return;
  }

  const delay = Math.min(rawDelay, MAX_TIMEOUT_MS);

  timer = setTimeout(() => {
    timer = null;
    // If we only advanced partway (delay was capped), reschedule; otherwise refresh.
    if (delay < rawDelay) {
      scheduleProactiveRefresh();
      return;
    }
    void runRefresh();
  }, delay);

  if (typeof timer === "object" && timer !== null && "unref" in timer) {
    (timer as NodeJS.Timeout).unref?.();
  }
}

async function runRefresh() {
  if (refreshing) return;
  refreshing = true;
  try {
    const refreshed = await session.refresh();
    if (refreshed) {
      scheduleProactiveRefresh();
    }
  } finally {
    refreshing = false;
  }
}

export function cancelProactiveRefresh(): void {
  clearTimer();
}
