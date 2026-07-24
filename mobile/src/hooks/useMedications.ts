/** Medication queries and mutations, with cache invalidation on writes. */
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { medicationApi } from "@/api/medication";
import { mapMedication } from "@/api/mappers";
import type { CreateMedicationRequest, UpdateMedicationRequest } from "@/api/types";
import { queryKeys } from "@/constants/queryKeys";
import { NotificationService } from "@/services/NotificationService";
import { rescheduleRemindersFromCache } from "@/hooks/useReminders";
import type { Medication } from "@/types/medication";

export function useMedications() {
  return useQuery({
    queryKey: queryKeys.medications,
    networkMode: "offlineFirst",
    queryFn: async () => {
      const dtos = await medicationApi.list();
      return dtos.map((dto) => mapMedication(dto));
    },
  });
}

export function useMedication(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.medication(id ?? "unknown"),
    enabled: Boolean(id),
    networkMode: "offlineFirst",
    queryFn: async () => {
      const dto = await medicationApi.getById(id as string);
      return mapMedication(dto);
    },
  });
}

export function useCreateMedication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateMedicationRequest) => medicationApi.create(request),
    onSuccess: async () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.medications });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      await rescheduleRemindersFromCache(queryClient);
    },
  });
}

export function useUpdateMedication(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateMedicationRequest) => medicationApi.update(id, request),
    onSuccess: async () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.medications });
      queryClient.invalidateQueries({ queryKey: queryKeys.medication(id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      await rescheduleRemindersFromCache(queryClient);
    },
  });
}

export function useArchiveMedication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => medicationApi.archive(id),
    onSuccess: async (_data, id) => {
      const cached = queryClient.getQueryData<Medication>(queryKeys.medication(id));
      const scheduleIds =
        cached?.schedules.map((s) => s.id) ??
        queryClient
          .getQueryData<Medication[]>(queryKeys.medications)
          ?.find((m) => m.id === id)
          ?.schedules.map((s) => s.id) ??
        [];
      await NotificationService.cancelAllForMedication(id, scheduleIds);

      queryClient.invalidateQueries({ queryKey: queryKeys.medications });
      queryClient.invalidateQueries({ queryKey: queryKeys.medication(id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      await rescheduleRemindersFromCache(queryClient);
    },
  });
}

export function useMedicationSearch(query: string) {
  const trimmed = query.trim();
  return useQuery({
    queryKey: queryKeys.medicationSearch(trimmed.toLowerCase()),
    enabled: trimmed.length > 1,
    staleTime: 5 * 60_000,
    queryFn: () => medicationApi.search(trimmed),
  });
}
