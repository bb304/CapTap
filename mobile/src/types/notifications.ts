/** Notification preference types for CapTap reminders. */
export type NotificationPreferences = {
  /** Master switch for local medication reminders. */
  enabled: boolean;
  /**
   * Minutes after the scheduled dose time to fire the reminder.
   * Default 60 → 8:00 dose → 9:00 reminder.
   */
  reminderOffsetMinutes: number;
  /**
   * When enabled, reminders that would fire inside [quietHoursStart, quietHoursEnd)
   * are deferred to quietHoursEnd (supports overnight windows like 22:00–07:00).
   */
  quietHoursEnabled: boolean;
  quietHoursStart: string; // "HH:mm"
  quietHoursEnd: string; // "HH:mm"
};

export const DEFAULT_NOTIFICATION_PREFERENCES: NotificationPreferences = {
  enabled: true,
  reminderOffsetMinutes: 60,
  quietHoursEnabled: false,
  quietHoursStart: "22:00",
  quietHoursEnd: "07:00",
};

export type ReminderPlanItem = {
  /** Stable id for scheduleNotificationAsync / cancel. */
  identifier: string;
  medicationId: string;
  medicationName: string;
  scheduleId: string;
  scheduledDoseLocal: Date;
  fireAt: Date;
  body: string;
};
