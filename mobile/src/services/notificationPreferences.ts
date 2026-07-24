/** Persist notification preferences in SecureStore. */
import * as SecureStore from "expo-secure-store";
import {
  DEFAULT_NOTIFICATION_PREFERENCES,
  type NotificationPreferences,
} from "@/types/notifications";

const KEY = "captap.notificationPreferences";

export async function loadNotificationPreferences(): Promise<NotificationPreferences> {
  try {
    const raw = await SecureStore.getItemAsync(KEY);
    if (!raw) {
      return { ...DEFAULT_NOTIFICATION_PREFERENCES };
    }
    const parsed = JSON.parse(raw) as Partial<NotificationPreferences>;
    return {
      ...DEFAULT_NOTIFICATION_PREFERENCES,
      ...parsed,
    };
  } catch {
    return { ...DEFAULT_NOTIFICATION_PREFERENCES };
  }
}

export async function saveNotificationPreferences(prefs: NotificationPreferences): Promise<void> {
  await SecureStore.setItemAsync(KEY, JSON.stringify(prefs));
}
