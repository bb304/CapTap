/**
 * Pure reminder planning — no native modules.
 * Schedules fire `reminderOffsetMinutes` after each scheduled dose wall-clock time.
 * When quiet hours are enabled, fire times that fall inside the window are deferred
 * to quietHoursEnd (same morning or next day for overnight windows).
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

/** Parse "HH:mm" or "HH:mm:ss" into minutes from local midnight. */
export function parseHmToMinutes(value: string): number {
  const [h, m] = value.split(":").map(Number);
  return (h ?? 0) * 60 + (m ?? 0);
}

/**
 * Overnight windows (start > end, e.g. 22:00–07:00) treat the quiet span as
 * wrapping midnight. Same-day windows (start < end) are a single daytime block.
 */
export function isWithinQuietHours(
  fireAt: Date,
  quietHoursStart: string,
  quietHoursEnd: string,
): boolean {
  const minutes = fireAt.getHours() * 60 + fireAt.getMinutes();
  const start = parseHmToMinutes(quietHoursStart);
  const end = parseHmToMinutes(quietHoursEnd);

  if (start === end) {
    return false;
  }

  if (start < end) {
    return minutes >= start && minutes < end;
  }

  // Overnight: e.g. 22:00–07:00 → quiet if >= 22:00 OR < 07:00
  return minutes >= start || minutes < end;
}

/**
 * If `fireAt` is inside quiet hours, move it to quietHoursEnd on the appropriate day.
 * Otherwise return the original instant.
 */
export function deferPastQuietHours(
  fireAt: Date,
  quietHoursStart: string,
  quietHoursEnd: string,
): Date {
  if (!isWithinQuietHours(fireAt, quietHoursStart, quietHoursEnd)) {
    return fireAt;
  }

  const end = parseHmToMinutes(quietHoursEnd);
  const endHours = Math.floor(end / 60);
  const endMinutes = end % 60;
  const start = parseHmToMinutes(quietHoursStart);
  const minutes = fireAt.getHours() * 60 + fireAt.getMinutes();

  const deferred = new Date(fireAt);

  if (start < end) {
    // Same-day quiet block → end later the same calendar day.
    deferred.setHours(endHours, endMinutes, 0, 0);
    return deferred;
  }

  // Overnight: early-morning quiet (before end) → same day end;
  // evening quiet (at/after start) → next day end.
  if (minutes < end) {
    deferred.setHours(endHours, endMinutes, 0, 0);
  } else {
    deferred.setDate(deferred.getDate() + 1);
    deferred.setHours(endHours, endMinutes, 0, 0);
  }

  return deferred;
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
        let fireAt = new Date(doseLocal.getTime() + prefs.reminderOffsetMinutes * 60_000);

        if (prefs.quietHoursEnabled) {
          fireAt = deferPastQuietHours(fireAt, prefs.quietHoursStart, prefs.quietHoursEnd);
        }

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
