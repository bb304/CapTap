/** Schedule API calls. No React, no UI — pure networking. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import type {
  ApiResponse,
  CreateScheduleRequest,
  ScheduleDto,
  UpdateScheduleRequest,
} from "./types";

export const scheduleApi = {
  async list(medicationId: string): Promise<ScheduleDto[]> {
    const response = await apiClient.get<ApiResponse<ScheduleDto[]>>(
      endpoints.medications.schedules(medicationId),
    );
    return unwrap(response);
  },

  async create(
    medicationId: string,
    request: CreateScheduleRequest,
  ): Promise<ScheduleDto> {
    const response = await apiClient.post<ApiResponse<ScheduleDto>>(
      endpoints.medications.schedules(medicationId),
      request,
    );
    return unwrap(response);
  },

  async update(
    scheduleId: string,
    request: UpdateScheduleRequest,
  ): Promise<ScheduleDto> {
    const response = await apiClient.patch<ApiResponse<ScheduleDto>>(
      endpoints.schedules.update(scheduleId),
      request,
    );
    return unwrap(response);
  },

  async remove(scheduleId: string): Promise<void> {
    await apiClient.delete(endpoints.schedules.remove(scheduleId));
  },
};
