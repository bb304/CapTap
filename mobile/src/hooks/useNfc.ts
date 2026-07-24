/** NFC React Query hooks. */
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { nfcApi } from "@/api/nfc";
import type { AssignNfcTagRequest, UnassignNfcTagRequest } from "@/api/types";
import { queryKeys } from "@/constants/queryKeys";
import { LocalDatabase } from "@/services/localDatabase";
import type { Medication } from "@/types/medication";
import type { DashboardSummary } from "@/types/dashboard";

export function useNfcTags() {
  return useQuery({
    queryKey: queryKeys.nfcTags,
    networkMode: "offlineFirst",
    queryFn: () => nfcApi.listTags(),
  });
}

export function useAssignNfcTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: AssignNfcTagRequest) => nfcApi.assign(request),
    onSuccess: async (tag, request) => {
      const medication =
        queryClient.getQueryData<Medication>(queryKeys.medication(request.medicationId)) ??
        queryClient
          .getQueryData<Medication[]>(queryKeys.medications)
          ?.find((m) => m.id === request.medicationId);
      const dashboard = queryClient.getQueryData<DashboardSummary>(queryKeys.dashboard);
      const dose = dashboard?.today.find((d) => d.medicationId === request.medicationId);

      await LocalDatabase.setNfcTagCache(tag.tagIdentifier, {
        medicationId: tag.medicationId,
        medicationName: tag.medicationName || medication?.name || "Medication",
        dosageAmount: medication?.dosageAmount ?? 0,
        dosageUnit: medication?.dosageUnit ?? "",
        form: medication?.form ?? null,
        scheduleId: dose?.scheduleId ?? medication?.schedules[0]?.id ?? null,
        scheduledTime: dose?.scheduledTime ?? medication?.schedules[0]?.scheduledTime ?? null,
        scheduledDoseTime: dose?.scheduledDoseTime ?? null,
        status: dose?.status ?? null,
        alreadyLogged: dose?.status === "Taken",
        tagIdentifier: tag.tagIdentifier,
      });
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.nfcTags });
      queryClient.invalidateQueries({ queryKey: queryKeys.medications });
    },
  });
}

export function useUnassignNfcTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UnassignNfcTagRequest) => nfcApi.unassign(request),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.nfcTags });
      queryClient.invalidateQueries({ queryKey: queryKeys.medications });
    },
  });
}

export function useResolveNfcTag() {
  return useMutation({
    mutationFn: (tagIdentifier: string) => nfcApi.resolve(tagIdentifier),
    networkMode: "always",
  });
}
