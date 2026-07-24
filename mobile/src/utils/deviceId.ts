/** Stable device identifier for audit headers (not a secret). */
import * as SecureStore from "expo-secure-store";

const KEY = "captap.deviceId";

function createId(): string {
  return `dev-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

let cached: string | null = null;

export async function getDeviceId(): Promise<string> {
  if (cached) return cached;
  try {
    const existing = await SecureStore.getItemAsync(KEY);
    if (existing) {
      cached = existing;
      return existing;
    }
    const created = createId();
    await SecureStore.setItemAsync(KEY, created);
    cached = created;
    return created;
  } catch {
    const fallback = createId();
    cached = fallback;
    return fallback;
  }
}
