/**
 * Pure reminder planning — no native modules.
 * Schedules fire `reminderOffsetMinutes` after each scheduled dose wall-clock time.
 */
import { toApiTime } from "@/utils/format";
import type { Medication } from "@/types/medication";
import type { NotificationPreferences, ReminderPlanItem } from "@/types/notifications";
import { DEFAULT_NOTIFICATION_PREFERENCES } from "@/types/notifications";

export function reminderIdentifier(scheduleId: string, localDay: Date): string {
  const y = localDay.getFullYear();
  const m = String(localDay.getMonth() + 1).padStart(2, "0");
  const d = String(localDay.getDate()).padStart(2, "0");
  return `captap-reminder:${scheduleId}:${y}-${m}-${d}`;
}

export function parseScheduledTime(scheduledTime: string): {
  hours: number;
  minutes: number;
  seconds: number;
} {
  const [h, m, s] = toApiTime(scheduledTime).split(":").map(Number);
  return { hours: h ?? 0, minutes: m ?? 0, seconds: s ?? 0 };
}

function startOfLocalDay(day: Date): Date {
  return new Date(day.getFullYear(), day.getMonth(), day.getDate());
}

function addDays(day: Date, days: number): Date {
  const next = startOfLocalDay(day);
  next.setDate(next.getDate() + days);
  return next;
}

/**
 * Build reminder plans for active medications over the next `horizonDays` local days.
 * Skips doses whose scheduleId is in `loggedScheduleIdsByDay` for that local day key.
 */
export function buildReminderPlan(options: {
  medications: Medication[];
  preferences?: NotificationPreferences;
  now?: Date;
  horizonDays?: number;
  /** Map of `YYYY-MM-DD` → set of scheduleIds already logged that day. */
  loggedScheduleIdsByDay?: Record<string, Set<string>>;
}): ReminderPlanItem[] {
  const prefs = options.preferences ?? DEFAULT_NOTIFICATION_PREFERENCES;
  if (!prefs.enabled) {
    return [];
  }

  const now = options.now ?? new Date();
  const horizon = Math.max(1, options.horizonDays ?? 7);
  const logged = options.loggedScheduleIdsByDay ?? {};
  const items: ReminderPlanItem[] = [];

  const active = options.medications.filter((m) => !m.isArchived);

  for (let offset = 0; offset < horizon; offset++) {
    const day = addDays(now, offset);
    const dayKey = formatDayKey(day);
    const loggedToday = logged[dayKey] ?? new Set<string>();

    for (const medication of active) {
      for (const schedule of medication.schedules) {
        if (loggedToday.has(schedule.id)) {
          continue;
        }

        const { hours, minutes, seconds } = parseScheduledTime(schedule.scheduledTime);
        const doseLocal = new Date(
          day.getFullYear(),
          day.getMonth(),
          day.getDate(),
          hours,
          minutes,
          seconds,
        );
        const fireAt = new Date(doseLocal.getTime() + prefs.reminderOffsetMinutes * 60_000);

        // Skip reminders that would fire in the past.
        if (fireAt.getTime() <= now.getTime()) {
          continue;
        }

        items.push({
          identifier: reminderIdentifier(schedule.id, day),
          medicationId: medication.id,
          medicationName: medication.name,
          scheduleId: schedule.id,
          scheduledDoseLocal: doseLocal,
          fireAt,
          body: `Have you taken ${medication.name}? Tap CapTap to confirm.`,
        });
      }
    }
  }

  // Dedupe by identifier (last write wins).
  const byId = new Map<string, ReminderPlanItem>();
  for (const item of items) {
    byId.set(item.identifier, item);
  }
  return [...byId.values()].sort((a, b) => a.fireAt.getTime() - b.fireAt.getTime());
}

export function formatDayKey(day: Date): string {
  const y = day.getFullYear();
  const m = String(day.getMonth() + 1).padStart(2, "0");
  const d = String(day.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}
