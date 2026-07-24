/** Schedule queries and mutations for a medication. */
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { scheduleApi } from "@/api/schedule";
import { mapSchedule } from "@/api/mappers";
import type {
  CreateScheduleRequest,
  UpdateScheduleRequest,
} from "@/api/types";
import { queryKeys } from "@/constants/queryKeys";

export function useSchedules(medicationId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.schedules(medicationId ?? "unknown"),
    enabled: Boolean(medicationId),
    queryFn: async () => {
      const dtos = await scheduleApi.list(medicationId as string);
      return dtos.map(mapSchedule);
    },
  });
}

function useScheduleInvalidation(medicationId: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({
      queryKey: queryKeys.schedules(medicationId),
    });
    queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
  };
}

export function useCreateSchedule(medicationId: string) {
  const invalidate = useScheduleInvalidation(medicationId);
  return useMutation({
    mutationFn: (request: CreateScheduleRequest) =>
      scheduleApi.create(medicationId, request),
    onSuccess: invalidate,
  });
}

export function useUpdateSchedule(medicationId: string) {
  const invalidate = useScheduleInvalidation(medicationId);
  return useMutation({
    mutationFn: (vars: { scheduleId: string; request: UpdateScheduleRequest }) =>
      scheduleApi.update(vars.scheduleId, vars.request),
    onSuccess: invalidate,
  });
}

export function useDeleteSchedule(medicationId: string) {
  const invalidate = useScheduleInvalidation(medicationId);
  return useMutation({
    mutationFn: (scheduleId: string) => scheduleApi.remove(scheduleId),
    onSuccess: invalidate,
  });
}
