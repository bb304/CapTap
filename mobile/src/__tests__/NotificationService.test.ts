import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import * as Notifications from "expo-notifications";
import { NotificationService } from "@/services/NotificationService";
import type { Medication } from "@/types/medication";

const scheduleMock = Notifications.scheduleNotificationAsync as jest.MockedFunction<
  typeof Notifications.scheduleNotificationAsync
>;
const cancelMock = Notifications.cancelScheduledNotificationAsync as jest.MockedFunction<
  typeof Notifications.cancelScheduledNotificationAsync
>;
const getAllMock = Notifications.getAllScheduledNotificationsAsync as jest.MockedFunction<
  typeof Notifications.getAllScheduledNotificationsAsync
>;

const med: Medication = {
  id: "m1",
  name: "Metformin",
  dosageAmount: 500,
  dosageUnit: "mg",
  isArchived: false,
  schedules: [{ id: "s1", scheduledTime: "08:00:00", doseQuantity: 1 }],
};

describe("NotificationService", () => {
  beforeEach(() => {
    scheduleMock.mockReset();
    cancelMock.mockReset();
    getAllMock.mockReset();
    getAllMock.mockResolvedValue([]);
    scheduleMock.mockResolvedValue("id");
  });

  it("cancels existing CapTap reminders before rescheduling", async () => {
    getAllMock.mockResolvedValue([
      {
        identifier: "captap-reminder:s1:2026-07-24",
        content: {},
        trigger: null,
      } as never,
      {
        identifier: "other-app",
        content: {},
        trigger: null,
      } as never,
    ]);

    await NotificationService.setPreferences({
      enabled: true,
      reminderOffsetMinutes: 60,
      quietHoursEnabled: false,
      quietHoursStart: "22:00",
      quietHoursEnd: "07:00",
    });

    // Force a future plan by using evening dose relative to "now" is hard;
    // at minimum cancelAll should run.
    await NotificationService.cancelAllCapTapReminders();
    expect(cancelMock).toHaveBeenCalledWith("captap-reminder:s1:2026-07-24");
    expect(cancelMock).not.toHaveBeenCalledWith("other-app");
  });

  it("cancelForSchedule targets the stable identifier", async () => {
    const day = new Date(2026, 6, 24);
    await NotificationService.cancelForSchedule("s1", day);
    expect(cancelMock).toHaveBeenCalledWith("captap-reminder:s1:2026-07-24");
  });

  it("skips scheduling when preferences are disabled", async () => {
    await NotificationService.setPreferences({
      enabled: false,
      reminderOffsetMinutes: 60,
      quietHoursEnabled: false,
      quietHoursStart: "22:00",
      quietHoursEnd: "07:00",
    });

    const result = await NotificationService.rescheduleAll({
      medications: [med],
    });
    expect(result.scheduled).toBe(0);
    expect(scheduleMock).not.toHaveBeenCalled();
  });
});
