/** Medication API calls. No React, no UI — pure networking. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import type {
  ApiResponse,
  CreateMedicationRequest,
  MedicationDto,
  MedicationSearchResult,
  UpdateMedicationRequest,
} from "./types";

export const medicationApi = {
  async list(): Promise<MedicationDto[]> {
    const response = await apiClient.get<ApiResponse<MedicationDto[]>>(endpoints.medications.list);
    return unwrap(response);
  },

  async getById(id: string): Promise<MedicationDto> {
    const response = await apiClient.get<ApiResponse<MedicationDto>>(
      endpoints.medications.byId(id),
    );
    return unwrap(response);
  },

  async create(request: CreateMedicationRequest): Promise<MedicationDto> {
    const response = await apiClient.post<ApiResponse<MedicationDto>>(
      endpoints.medications.create,
      request,
    );
    return unwrap(response);
  },

  async update(id: string, request: UpdateMedicationRequest): Promise<MedicationDto> {
    const response = await apiClient.patch<ApiResponse<MedicationDto>>(
      endpoints.medications.update(id),
      request,
    );
    return unwrap(response);
  },

  async archive(id: string): Promise<void> {
    await apiClient.post(endpoints.medications.archive(id));
  },

  async search(query: string): Promise<MedicationSearchResult[]> {
    const response = await apiClient.get<ApiResponse<MedicationSearchResult[]>>(
      endpoints.medications.search,
      { params: { q: query } },
    );
    return unwrap(response);
  },
};
