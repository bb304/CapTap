/** Sync the device IANA time zone to the backend after authentication. */
import { userApi } from "@/api/user";

export function deviceTimeZoneId(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  } catch {
    return "UTC";
  }
}

export async function syncDeviceTimeZone(): Promise<void> {
  const timeZoneId = deviceTimeZoneId();
  try {
    await userApi.updateTimeZone({ timeZoneId });
  } catch {
    // Non-blocking — adherence falls back to UTC until the next sync.
  }
}
