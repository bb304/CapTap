/** Medication log API calls. Manual today; Nfc reuses the same endpoint later. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import type {
  ApiResponse,
  CreateMedicationLogRequest,
  MedicationLogDto,
  PaginatedMedicationLogHistory,
} from "./types";

export const medicationLogApi = {
  async create(request: CreateMedicationLogRequest): Promise<MedicationLogDto> {
    const response = await apiClient.post<ApiResponse<MedicationLogDto>>(
      endpoints.medicationLogs.create,
      request,
    );
    return unwrap(response);
  },

  async history(params?: {
    page?: number;
    pageSize?: number;
    medicationId?: string;
  }): Promise<PaginatedMedicationLogHistory> {
    const response = await apiClient.get<ApiResponse<PaginatedMedicationLogHistory>>(
      endpoints.medicationLogs.history,
      {
        params: {
          page: params?.page ?? 1,
          pageSize: params?.pageSize ?? 20,
          medicationId: params?.medicationId,
        },
      },
    );
    return unwrap(response);
  },
};
