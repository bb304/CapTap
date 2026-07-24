import { describe, expect, it } from "@jest/globals";
import { buildReminderPlan, reminderIdentifier } from "@/services/reminderScheduler";
import type { Medication } from "@/types/medication";
import { DEFAULT_NOTIFICATION_PREFERENCES } from "@/types/notifications";

const med = (overrides?: Partial<Medication>): Medication => ({
  id: "m1",
  name: "Metformin",
  dosageAmount: 500,
  dosageUnit: "mg",
  isArchived: false,
  schedules: [{ id: "s1", scheduledTime: "08:00:00", doseQuantity: 1 }],
  ...overrides,
});

describe("buildReminderPlan", () => {
  it("schedules a reminder one hour after the dose by default", () => {
    const now = new Date(2026, 6, 24, 7, 0, 0); // Jul 24 07:00 local
    const plan = buildReminderPlan({
      medications: [med()],
      preferences: DEFAULT_NOTIFICATION_PREFERENCES,
      now,
      horizonDays: 1,
    });

    expect(plan).toHaveLength(1);
    expect(plan[0].identifier).toBe(reminderIdentifier("s1", now));
    expect(plan[0].fireAt.getHours()).toBe(9);
    expect(plan[0].fireAt.getMinutes()).toBe(0);
  });

  it("skips reminders for doses already logged that day", () => {
    const now = new Date(2026, 6, 24, 7, 0, 0);
    const dayKey = "2026-07-24";
    const plan = buildReminderPlan({
      medications: [med()],
      now,
      horizonDays: 1,
      loggedScheduleIdsByDay: { [dayKey]: new Set(["s1"]) },
    });
    expect(plan).toHaveLength(0);
  });

  it("ignores archived medications", () => {
    const now = new Date(2026, 6, 24, 7, 0, 0);
    const plan = buildReminderPlan({
      medications: [med({ isArchived: true })],
      now,
      horizonDays: 1,
    });
    expect(plan).toHaveLength(0);
  });

  it("returns empty when notifications are disabled", () => {
    const now = new Date(2026, 6, 24, 7, 0, 0);
    const plan = buildReminderPlan({
      medications: [med()],
      preferences: { ...DEFAULT_NOTIFICATION_PREFERENCES, enabled: false },
      now,
      horizonDays: 1,
    });
    expect(plan).toHaveLength(0);
  });

  it("skips fire times that are already in the past", () => {
    const now = new Date(2026, 6, 24, 10, 0, 0); // after 09:00 reminder
    const plan = buildReminderPlan({
      medications: [med()],
      now,
      horizonDays: 1,
    });
    expect(plan).toHaveLength(0);
  });

  it("respects a custom offset", () => {
    const now = new Date(2026, 6, 24, 7, 0, 0);
    const plan = buildReminderPlan({
      medications: [med()],
      preferences: {
        ...DEFAULT_NOTIFICATION_PREFERENCES,
        reminderOffsetMinutes: 30,
      },
      now,
      horizonDays: 1,
    });
    expect(plan[0].fireAt.getHours()).toBe(8);
    expect(plan[0].fireAt.getMinutes()).toBe(30);
  });
});
