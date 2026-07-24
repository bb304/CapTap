/** NFC API — assignment and resolution. Logging still uses medicationLogApi. */
import { onlineManager } from "@tanstack/react-query";
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import { ApiClientError } from "./errors";
import type {
  ApiResponse,
  AssignNfcTagRequest,
  NfcResolveDto,
  NfcTagDto,
  UnassignNfcTagRequest,
} from "./types";
import { LocalDatabase } from "@/services/localDatabase";

export const nfcApi = {
  async listTags(): Promise<NfcTagDto[]> {
    const response = await apiClient.get<ApiResponse<NfcTagDto[]>>(endpoints.nfc.tags);
    return unwrap(response);
  },

  async assign(request: AssignNfcTagRequest): Promise<NfcTagDto> {
    const response = await apiClient.post<ApiResponse<NfcTagDto>>(endpoints.nfc.assign, request);
    return unwrap(response);
  },

  async unassign(request: UnassignNfcTagRequest): Promise<NfcTagDto> {
    const response = await apiClient.post<ApiResponse<NfcTagDto>>(endpoints.nfc.unassign, request);
    return unwrap(response);
  },

  async resolve(tagIdentifier: string): Promise<NfcResolveDto> {
    const tryNetwork = async () => {
      const response = await apiClient.get<ApiResponse<NfcResolveDto>>(
        endpoints.nfc.resolve(encodeURIComponent(tagIdentifier)),
      );
      const resolved = unwrap(response);
      await LocalDatabase.setNfcTagCache(tagIdentifier, resolved);
      return resolved;
    };

    if (!onlineManager.isOnline()) {
      const cached = await LocalDatabase.getNfcTagCache<NfcResolveDto>(tagIdentifier);
      if (cached) {
        return {
          ...cached,
          // Offline confirm still posts through the shared log queue.
        };
      }
      throw new ApiClientError({
        kind: "network",
        message:
          "You're offline and this tag isn't cached yet. Connect once after assigning the tag, then try again.",
      });
    }

    try {
      return await tryNetwork();
    } catch (error) {
      if (
        error instanceof ApiClientError &&
        (error.kind === "network" || error.kind === "timeout")
      ) {
        const cached = await LocalDatabase.getNfcTagCache<NfcResolveDto>(tagIdentifier);
        if (cached) return cached;
      }
      throw error;
    }
  },
};
