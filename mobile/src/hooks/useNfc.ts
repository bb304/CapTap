/** NFC React Query hooks. */
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { nfcApi } from "@/api/nfc";
import type { AssignNfcTagRequest, UnassignNfcTagRequest } from "@/api/types";
import { queryKeys } from "@/constants/queryKeys";

export function useNfcTags() {
  return useQuery({
    queryKey: queryKeys.nfcTags,
    queryFn: () => nfcApi.listTags(),
  });
}

export function useAssignNfcTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: AssignNfcTagRequest) => nfcApi.assign(request),
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
  });
}
