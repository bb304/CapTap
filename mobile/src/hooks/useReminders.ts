/**
 * Orchestrates CapTap local reminders against medications + today's logs.
 */
import { useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { NotificationService } from "@/services/NotificationService";
import type { NotificationPreferences } from "@/types/notifications";
import type { Medication } from "@/types/medication";
import type { DashboardSummary } from "@/types/dashboard";
import { queryKeys } from "@/constants/queryKeys";

function loggedIdsFromDashboard(
  dashboard: DashboardSummary | undefined,
): Record<string, Set<string>> {
  const todayKey = NotificationService.todayKey();
  const taken = new Set<string>();
  if (dashboard) {
    for (const dose of dashboard.today) {
      if (dose.status === "Taken" && dose.scheduleId) {
        taken.add(dose.scheduleId);
      }
    }
  }
  return { [todayKey]: taken };
}

export async function rescheduleRemindersFromCache(
  queryClient: ReturnType<typeof useQueryClient>,
  preferences?: NotificationPreferences,
): Promise<{ scheduled: number; skippedPermission: boolean }> {
  const medications = queryClient.getQueryData<Medication[]>(queryKeys.medications) ?? [];
  const dashboard = queryClient.getQueryData<DashboardSummary>(queryKeys.dashboard);

  return NotificationService.rescheduleAll({
    medications,
    loggedScheduleIdsByDay: loggedIdsFromDashboard(dashboard),
    preferences,
  });
}

export async function cancelReminderAfterLog(scheduleId?: string | null) {
  if (!scheduleId) return;
  await NotificationService.cancelForSchedule(scheduleId);
}

/** Keep reminders aligned when the signed-in user has medication data loaded. */
export function useReminderSync(enabled: boolean) {
  const queryClient = useQueryClient();

  const sync = useCallback(async () => {
    if (!enabled) return;
    try {
      await rescheduleRemindersFromCache(queryClient);
    } catch {
      // Non-fatal — reminders retry on next foreground / mutation.
    }
  }, [enabled, queryClient]);

  useEffect(() => {
    if (!enabled) return;
    void sync();
  }, [enabled, sync]);

  return { sync };
}
