/** Medication logging mutations and history queries — online or queued offline. */
import { useMutation, useQuery, useQueryClient, onlineManager } from "@tanstack/react-query";
import { medicationLogApi } from "@/api/medicationLog";
import { ApiClientError } from "@/api/errors";
import type {
  CreateMedicationLogRequest,
  MedicationLogDto,
  PaginatedMedicationLogHistory,
} from "@/api/types";
import { queryKeys } from "@/constants/queryKeys";
import type { DashboardSummary } from "@/types/dashboard";
import type { Medication } from "@/types/medication";
import { cancelReminderAfterLog, rescheduleRemindersFromCache } from "@/hooks/useReminders";
import { LocalDatabase } from "@/services/localDatabase";
import { buildOptimisticLog, processOfflineQueue } from "@/services/syncEngine";

function applyOptimisticDashboard(
  previous: DashboardSummary | undefined,
  request: CreateMedicationLogRequest,
): DashboardSummary | undefined {
  if (!previous) return previous;
  return {
    ...previous,
    today: previous.today.map((dose) =>
      dose.scheduleId === request.scheduleId && dose.status !== "Taken"
        ? { ...dose, status: "Taken" as const }
        : dose,
    ),
    upcoming: previous.upcoming.filter((dose) => dose.scheduleId !== request.scheduleId),
    takenCount: Math.min(previous.totalMedicationsToday, previous.takenCount + 1),
    completionPercent:
      previous.totalMedicationsToday === 0
        ? 0
        : Math.round(
            (100 * Math.min(previous.totalMedicationsToday, previous.takenCount + 1)) /
              previous.totalMedicationsToday,
          ),
  };
}

function medicationNameFromCache(
  queryClient: ReturnType<typeof useQueryClient>,
  medicationId: string,
): string {
  const list = queryClient.getQueryData<Medication[]>(queryKeys.medications);
  const match = list?.find((m) => m.id === medicationId);
  if (match) return match.name;
  const single = queryClient.getQueryData<Medication>(queryKeys.medication(medicationId));
  return single?.name ?? "Medication";
}

async function createLogOnlineOrQueue(
  queryClient: ReturnType<typeof useQueryClient>,
  request: CreateMedicationLogRequest,
): Promise<MedicationLogDto> {
  const name = medicationNameFromCache(queryClient, request.medicationId);

  if (!onlineManager.isOnline()) {
    const item = await LocalDatabase.enqueueMedicationLog(request);
    return buildOptimisticLog(request, name, item.clientRequestId);
  }

  try {
    return await medicationLogApi.create(request);
  } catch (error) {
    if (error instanceof ApiClientError && (error.kind === "network" || error.kind === "timeout")) {
      const item = await LocalDatabase.enqueueMedicationLog(request);
      return buildOptimisticLog(request, name, item.clientRequestId);
    }
    throw error;
  }
}

export function useLogMedication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateMedicationLogRequest) =>
      createLogOnlineOrQueue(queryClient, request),
    networkMode: "always",
    onMutate: async (request) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.dashboard });
      const previous = queryClient.getQueryData<DashboardSummary>(queryKeys.dashboard);
      const next = applyOptimisticDashboard(previous, request);
      if (next) {
        queryClient.setQueryData(queryKeys.dashboard, next);
      }

      const history = queryClient.getQueryData<PaginatedMedicationLogHistory>(queryKeys.logHistory);
      if (history) {
        const optimistic = buildOptimisticLog(
          request,
          medicationNameFromCache(queryClient, request.medicationId),
          `opt_${Date.now()}`,
        );
        queryClient.setQueryData<PaginatedMedicationLogHistory>(queryKeys.logHistory, {
          ...history,
          items: [optimistic, ...history.items],
          totalCount: history.totalCount + 1,
        });
      }

      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.dashboard, context.previous);
      }
    },
    onSuccess: async (data, request) => {
      await cancelReminderAfterLog(request.scheduleId);
      queryClient.invalidateQueries({ queryKey: ["offline-queue", "count"] });
      // If this was a real server id, try draining any other pending items.
      if (!data.id.startsWith("local-") && onlineManager.isOnline()) {
        await processOfflineQueue(queryClient);
      }
    },
    onSettled: async () => {
      queryClient.invalidateQueries({ queryKey: ["offline-queue", "count"] });
      if (onlineManager.isOnline()) {
        queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
        queryClient.invalidateQueries({ queryKey: queryKeys.logHistory });
      }
      await rescheduleRemindersFromCache(queryClient);
    },
  });
}

export function useMedicationLogHistory(params?: {
  page?: number;
  pageSize?: number;
  medicationId?: string;
}) {
  return useQuery({
    queryKey: queryKeys.logHistory,
    queryFn: () => medicationLogApi.history(params),
    networkMode: "offlineFirst",
  });
}

export function usePendingSyncCount() {
  return useQuery({
    queryKey: ["offline-queue", "count"] as const,
    queryFn: () => LocalDatabase.countPending(),
    refetchInterval: 5_000,
    networkMode: "always",
  });
}
