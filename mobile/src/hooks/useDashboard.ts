/** Dashboard data: today's doses, missed doses, and server-computed stats. */
import { useQuery } from "@tanstack/react-query";
import { dashboardApi } from "@/api/dashboard";
import { mapDashboardToday } from "@/api/mappers";
import { queryKeys } from "@/constants/queryKeys";

export function useDashboard() {
  return useQuery({
    queryKey: queryKeys.dashboard,
    networkMode: "offlineFirst",
    queryFn: async () => {
      const [today, missed] = await Promise.all([dashboardApi.today(), dashboardApi.missed()]);
      return mapDashboardToday(today, missed);
    },
  });
}
