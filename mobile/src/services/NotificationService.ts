/**
 * Local medication reminders via Expo Notifications.
 *
 * Reminders fire on-device (not server push). Requires a development / production
 * build — behavior in Expo Go may be limited.
 */
import { Platform } from "react-native";
import * as Notifications from "expo-notifications";
import type { Medication } from "@/types/medication";
import type { NotificationPreferences } from "@/types/notifications";
import { buildReminderPlan, formatDayKey, reminderIdentifier } from "./reminderScheduler";
import {
  loadNotificationPreferences,
  saveNotificationPreferences,
} from "./notificationPreferences";

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowBanner: true,
    shouldShowList: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});

export type PermissionState = {
  granted: boolean;
  canAskAgain: boolean;
  status: string;
  message?: string;
};

let androidChannelReady = false;

async function ensureAndroidChannel(): Promise<void> {
  if (Platform.OS !== "android" || androidChannelReady) return;
  await Notifications.setNotificationChannelAsync("medication-reminders", {
    name: "Medication reminders",
    importance: Notifications.AndroidImportance.DEFAULT,
    vibrationPattern: [0, 250, 250, 250],
    lightColor: "#2F6FED",
  });
  androidChannelReady = true;
}

export const NotificationService = {
  async getPreferences(): Promise<NotificationPreferences> {
    return loadNotificationPreferences();
  },

  async setPreferences(prefs: NotificationPreferences): Promise<NotificationPreferences> {
    await saveNotificationPreferences(prefs);
    return prefs;
  },

  async getPermissionState(): Promise<PermissionState> {
    const current = await Notifications.getPermissionsAsync();
    return {
      granted: current.granted,
      canAskAgain: current.canAskAgain,
      status: String(current.status),
      message: current.granted
        ? undefined
        : current.canAskAgain
          ? "CapTap needs notification permission to remind you about doses."
          : "Notifications are turned off. Enable them in system Settings → CapTap.",
    };
  },

  async requestPermissions(): Promise<PermissionState> {
    const current = await Notifications.getPermissionsAsync();
    if (current.granted) {
      return {
        granted: true,
        canAskAgain: current.canAskAgain,
        status: String(current.status),
      };
    }

    const requested = await Notifications.requestPermissionsAsync();
    return {
      granted: requested.granted,
      canAskAgain: requested.canAskAgain,
      status: String(requested.status),
      message: requested.granted
        ? undefined
        : "Notification permission denied. You can enable reminders later in Settings.",
    };
  },

  /**
   * Cancel every CapTap-managed local reminder, then schedule the next horizon.
   */
  async rescheduleAll(options: {
    medications: Medication[];
    loggedScheduleIdsByDay?: Record<string, Set<string>>;
    preferences?: NotificationPreferences;
  }): Promise<{ scheduled: number; skippedPermission: boolean }> {
    const preferences = options.preferences ?? (await loadNotificationPreferences());

    await this.cancelAllCapTapReminders();

    if (!preferences.enabled) {
      return { scheduled: 0, skippedPermission: false };
    }

    const permission = await this.getPermissionState();
    if (!permission.granted) {
      return { scheduled: 0, skippedPermission: true };
    }

    await ensureAndroidChannel();

    const plan = buildReminderPlan({
      medications: options.medications,
      preferences,
      loggedScheduleIdsByDay: options.loggedScheduleIdsByDay,
    });

    let scheduled = 0;
    for (const item of plan) {
      try {
        await Notifications.scheduleNotificationAsync({
          identifier: item.identifier,
          content: {
            title: "Medication reminder",
            body: item.body,
            data: {
              medicationId: item.medicationId,
              scheduleId: item.scheduleId,
              kind: "medication-reminder",
            },
            sound: true,
            ...(Platform.OS === "android" ? { channelId: "medication-reminders" } : {}),
          },
          trigger: {
            type: Notifications.SchedulableTriggerInputTypes.DATE,
            date: item.fireAt,
          },
        });
        scheduled += 1;
      } catch {
        // Continue scheduling remaining reminders; surface aggregate via return.
      }
    }

    return { scheduled, skippedPermission: false };
  },

  async cancelForSchedule(scheduleId: string, day: Date = new Date()): Promise<void> {
    const id = reminderIdentifier(scheduleId, day);
    try {
      await Notifications.cancelScheduledNotificationAsync(id);
    } catch {
      // Already cancelled or never scheduled.
    }
  },

  async cancelAllForMedication(medicationId: string, scheduleIds: string[]): Promise<void> {
    for (const scheduleId of scheduleIds) {
      // Cancel today + next week identifiers for this schedule.
      for (let i = 0; i < 7; i++) {
        const day = new Date();
        day.setDate(day.getDate() + i);
        await this.cancelForSchedule(scheduleId, day);
      }
    }
    void medicationId;
  },

  async cancelAllCapTapReminders(): Promise<void> {
    const pending = await Notifications.getAllScheduledNotificationsAsync();
    await Promise.all(
      pending
        .filter((n) => n.identifier.startsWith("captap-reminder:"))
        .map(async (n) => {
          try {
            await Notifications.cancelScheduledNotificationAsync(n.identifier);
          } catch {
            // ignore
          }
        }),
    );
  },

  /** Helper for tests / UI — today's day key. */
  todayKey(now: Date = new Date()): string {
    return formatDayKey(now);
  },
};
