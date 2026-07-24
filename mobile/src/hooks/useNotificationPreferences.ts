/** Notification preference query + mutation. */
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { NotificationService } from "@/services/NotificationService";
import type { NotificationPreferences } from "@/types/notifications";
import { queryKeys } from "@/constants/queryKeys";
import { rescheduleRemindersFromCache } from "./useReminders";

export function useNotificationPreferences() {
  return useQuery({
    queryKey: queryKeys.notificationPreferences,
    queryFn: () => NotificationService.getPreferences(),
  });
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (prefs: NotificationPreferences) => {
      const saved = await NotificationService.setPreferences(prefs);
      if (prefs.enabled) {
        const permission = await NotificationService.requestPermissions();
        if (!permission.granted) {
          // Keep prefs, but caller should surface guidance.
          return { prefs: saved, permission };
        }
      }
      await rescheduleRemindersFromCache(queryClient, saved);
      const permission = await NotificationService.getPermissionState();
      return { prefs: saved, permission };
    },
    onSuccess: (result) => {
      queryClient.setQueryData(queryKeys.notificationPreferences, result.prefs);
    },
  });
}
