import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import React from "react";
import { renderHook, waitFor, act } from "@testing-library/react-native";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

jest.mock("@/api/dashboard", () => ({
  dashboardApi: {
    today: jest.fn(),
    missed: jest.fn(),
  },
}));

import { dashboardApi } from "@/api/dashboard";
import { useDashboard } from "@/hooks/useDashboard";
import type { DashboardTodayResponse, TodayDoseDto } from "@/api/types";

const todayMock = dashboardApi.today as jest.MockedFunction<typeof dashboardApi.today>;
const missedMock = dashboardApi.missed as jest.MockedFunction<typeof dashboardApi.missed>;

function dose(overrides: Partial<TodayDoseDto>): TodayDoseDto {
  return {
    medicationId: "m1",
    medicationName: "Metformin",
    scheduledTime: "08:00:00",
    doseQuantity: 1,
    status: "Upcoming",
    scheduleId: "s1",
    ...overrides,
  };
}

function createWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  };
}

beforeEach(() => {
  todayMock.mockReset();
  missedMock.mockReset();
});

describe("useDashboard", () => {
  it("loads server-computed stats and maps doses", async () => {
    const payload: DashboardTodayResponse = {
      doses: [
        dose({ scheduleId: "s1", status: "Taken" }),
        dose({ scheduleId: "s2", status: "Taken" }),
        dose({ scheduleId: "s3", status: "Upcoming" }),
        dose({ scheduleId: "s4", status: "Due" }),
      ],
      completionPercent: 50,
      currentStreakDays: 3,
      longestStreakDays: 12,
      totalMedicationsToday: 4,
      takenCount: 2,
      missedCount: 0,
    };
    todayMock.mockResolvedValue(payload);
    missedMock.mockResolvedValue([dose({ scheduleId: "s5", status: "Missed" })]);

    const { result } = renderHook(() => useDashboard(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    const data = result.current.data!;
    expect(data.completionPercent).toBe(50);
    expect(data.currentStreakDays).toBe(3);
    expect(data.longestStreakDays).toBe(12);
    expect(data.today).toHaveLength(4);
    expect(data.upcoming).toHaveLength(1);
    expect(data.missed).toHaveLength(1);
  });
});
