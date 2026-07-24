/** User profile / timezone API. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import type { ApiResponse, UpdateTimeZoneRequest, UserProfileDto } from "./types";

export const userApi = {
  async me(): Promise<UserProfileDto> {
    const response = await apiClient.get<ApiResponse<UserProfileDto>>(endpoints.users.me);
    return unwrap(response);
  },

  async updateTimeZone(request: UpdateTimeZoneRequest): Promise<UserProfileDto> {
    const response = await apiClient.put<ApiResponse<UserProfileDto>>(
      endpoints.users.timezone,
      request,
    );
    return unwrap(response);
  },
};
