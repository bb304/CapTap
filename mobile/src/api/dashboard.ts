/** Dashboard / adherence API calls. No React, no UI — pure networking. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import type {
  AdherenceStreakDto,
  ApiResponse,
  DashboardTodayResponse,
  TodayDoseDto,
} from "./types";

export const dashboardApi = {
  async today(): Promise<DashboardTodayResponse> {
    const response = await apiClient.get<ApiResponse<DashboardTodayResponse>>(
      endpoints.dashboard.today,
    );
    return unwrap(response);
  },

  async missed(): Promise<TodayDoseDto[]> {
    const response = await apiClient.get<ApiResponse<TodayDoseDto[]>>(
      endpoints.dashboard.missed,
    );
    return unwrap(response);
  },

  async streak(): Promise<AdherenceStreakDto> {
    const response = await apiClient.get<ApiResponse<AdherenceStreakDto>>(
      endpoints.dashboard.streak,
    );
    return unwrap(response);
  },
};
