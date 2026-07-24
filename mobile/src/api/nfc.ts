/** NFC API — assignment and resolution. Logging still uses medicationLogApi. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import type {
  ApiResponse,
  AssignNfcTagRequest,
  NfcResolveDto,
  NfcTagDto,
  UnassignNfcTagRequest,
} from "./types";

export const nfcApi = {
  async listTags(): Promise<NfcTagDto[]> {
    const response = await apiClient.get<ApiResponse<NfcTagDto[]>>(endpoints.nfc.tags);
    return unwrap(response);
  },

  async assign(request: AssignNfcTagRequest): Promise<NfcTagDto> {
    const response = await apiClient.post<ApiResponse<NfcTagDto>>(
      endpoints.nfc.assign,
      request,
    );
    return unwrap(response);
  },

  async unassign(request: UnassignNfcTagRequest): Promise<NfcTagDto> {
    const response = await apiClient.post<ApiResponse<NfcTagDto>>(
      endpoints.nfc.unassign,
      request,
    );
    return unwrap(response);
  },

  async resolve(tagIdentifier: string): Promise<NfcResolveDto> {
    const response = await apiClient.get<ApiResponse<NfcResolveDto>>(
      endpoints.nfc.resolve(encodeURIComponent(tagIdentifier)),
    );
    return unwrap(response);
  },
};
