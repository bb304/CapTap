/** Medication logging mutations and history queries. */
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { medicationLogApi } from "@/api/medicationLog";
import type { CreateMedicationLogRequest } from "@/api/types";
import { queryKeys } from "@/constants/queryKeys";
import type { DashboardSummary } from "@/types/dashboard";

export function useLogMedication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateMedicationLogRequest) =>
      medicationLogApi.create(request),
    onMutate: async (request) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.dashboard });
      const previous = queryClient.getQueryData<DashboardSummary>(queryKeys.dashboard);

      if (previous) {
        queryClient.setQueryData<DashboardSummary>(queryKeys.dashboard, {
          ...previous,
          today: previous.today.map((dose) =>
            dose.scheduleId === request.scheduleId &&
            dose.status !== "Taken"
              ? { ...dose, status: "Taken" as const }
              : dose,
          ),
          upcoming: previous.upcoming.filter(
            (dose) => dose.scheduleId !== request.scheduleId,
          ),
          takenCount: Math.min(
            previous.totalMedicationsToday,
            previous.takenCount + 1,
          ),
          completionPercent:
            previous.totalMedicationsToday === 0
              ? 0
              : Math.round(
                  (100 *
                    Math.min(
                      previous.totalMedicationsToday,
                      previous.takenCount + 1,
                    )) /
                    previous.totalMedicationsToday,
                ),
        });
      }

      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.dashboard, context.previous);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      queryClient.invalidateQueries({ queryKey: queryKeys.logHistory });
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
  });
}
