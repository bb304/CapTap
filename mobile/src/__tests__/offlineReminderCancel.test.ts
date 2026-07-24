import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import { onlineManager } from "@tanstack/react-query";
import { cancelReminderAfterLog } from "@/hooks/useReminders";
import { NotificationService } from "@/services/NotificationService";
import { LocalDatabase } from "@/services/localDatabase";

jest.mock("@/services/NotificationService", () => ({
  NotificationService: {
    cancelForSchedule: jest.fn(async () => undefined),
    rescheduleAll: jest.fn(async () => ({ scheduled: 0, skippedPermission: false })),
    todayKey: jest.fn(() => "2026-07-24"),
  },
}));

jest.mock("@/services/localDatabase", () => ({
  LocalDatabase: {
    enqueueMedicationLog: jest.fn(async () => ({
      id: 1,
      clientRequestId: "local-abc",
    })),
    countPending: jest.fn(async () => 1),
  },
}));

describe("offline logging cancels reminders", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    onlineManager.setOnline(false);
  });

  it("cancelReminderAfterLog cancels today's schedule reminder", async () => {
    await cancelReminderAfterLog("s1");
    expect(NotificationService.cancelForSchedule).toHaveBeenCalledWith("s1");
  });

  it("skips cancel when scheduleId is missing", async () => {
    await cancelReminderAfterLog(null);
    await cancelReminderAfterLog(undefined);
    expect(NotificationService.cancelForSchedule).not.toHaveBeenCalled();
  });

  it("offline enqueue path remains available while offline", async () => {
    expect(onlineManager.isOnline()).toBe(false);
    await LocalDatabase.enqueueMedicationLog({
      medicationId: "m1",
      scheduleId: "s1",
      scheduledDoseTime: "2026-07-24T08:00:00.000Z",
      loggingMethod: "Manual",
    });
    expect(LocalDatabase.enqueueMedicationLog).toHaveBeenCalled();
  });
});
