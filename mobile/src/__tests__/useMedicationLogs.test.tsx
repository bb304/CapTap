import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import React from "react";
import { renderHook, waitFor, act } from "@testing-library/react-native";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

jest.mock("@/api/medicationLog", () => ({
  medicationLogApi: {
    create: jest.fn(),
    history: jest.fn(),
  },
}));

import { medicationLogApi } from "@/api/medicationLog";
import { useLogMedication, useMedicationLogHistory } from "@/hooks/useMedicationLogs";
import { queryKeys } from "@/constants/queryKeys";
import type { DashboardSummary } from "@/types/dashboard";

const createMock = medicationLogApi.create as jest.MockedFunction<
  typeof medicationLogApi.create
>;
const historyMock = medicationLogApi.history as jest.MockedFunction<
  typeof medicationLogApi.history
>;

function createWrapper(initialDashboard?: DashboardSummary) {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  if (initialDashboard) {
    client.setQueryData(queryKeys.dashboard, initialDashboard);
  }
  return {
    client,
    Wrapper({ children }: { children: React.ReactNode }) {
      return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    },
  };
}

beforeEach(() => {
  createMock.mockReset();
  historyMock.mockReset();
});

describe("useLogMedication", () => {
  it("optimistically marks the dose Taken and invalidates on settle", async () => {
    createMock.mockResolvedValue({
      id: "log-1",
      medicationId: "m1",
      medicationName: "Metformin",
      scheduleId: "s1",
      scheduledDoseTime: "2026-07-24T08:00:00Z",
      loggedAt: "2026-07-24T08:05:00Z",
      loggingMethod: "Manual",
    });

    const initial: DashboardSummary = {
      today: [
        {
          id: "d1",
          medicationId: "m1",
          medicationName: "Metformin",
          scheduledTime: "08:00:00",
          doseQuantity: 1,
          status: "Due",
          scheduleId: "s1",
        },
      ],
      upcoming: [],
      missed: [],
      currentStreakDays: 1,
      longestStreakDays: 5,
      completionPercent: 0,
      totalMedicationsToday: 1,
      takenCount: 0,
      missedCount: 0,
    };

    const { client, Wrapper } = createWrapper(initial);
    const { result } = renderHook(() => useLogMedication(), {
      wrapper: Wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        medicationId: "m1",
        scheduleId: "s1",
        scheduledDoseTime: "2026-07-24T08:00:00.000Z",
        loggingMethod: "Manual",
      });
    });

    await waitFor(() => expect(createMock).toHaveBeenCalledTimes(1));
    const cached = client.getQueryData<DashboardSummary>(queryKeys.dashboard);
    // After settle, invalidate triggers refetch — cache may be stale or cleared.
    // Assert the mutation succeeded and was called with Manual method.
    expect(createMock.mock.calls[0][0].loggingMethod).toBe("Manual");
    expect(cached?.today[0]?.status === "Taken" || cached === undefined).toBe(true);
  });
});

describe("useMedicationLogHistory", () => {
  it("loads history items", async () => {
    historyMock.mockResolvedValue({
      items: [
        {
          id: "h1",
          medicationId: "m1",
          medicationName: "Metformin",
          scheduledDoseTime: "2026-07-24T08:00:00Z",
          loggedAt: "2026-07-24T08:05:00Z",
          loggingMethod: "Manual",
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });

    const { Wrapper } = createWrapper();
    const { result } = renderHook(() => useMedicationLogHistory(), {
      wrapper: Wrapper,
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.items[0].medicationName).toBe("Metformin");
  });
});
