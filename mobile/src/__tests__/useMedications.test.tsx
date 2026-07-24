import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import React from "react";
import { renderHook, waitFor } from "@testing-library/react-native";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

jest.mock("@/api/medication", () => ({
  medicationApi: {
    list: jest.fn(),
    create: jest.fn(),
  },
}));

import { medicationApi } from "@/api/medication";
import { useMedications, useCreateMedication } from "@/hooks/useMedications";
import { ApiClientError } from "@/api/errors";
import type { MedicationDto } from "@/api/types";

const listMock = medicationApi.list as jest.MockedFunction<typeof medicationApi.list>;
const createMock = medicationApi.create as jest.MockedFunction<
  typeof medicationApi.create
>;

const dto: MedicationDto = {
  id: "m1",
  name: "Metformin",
  dosageAmount: 500,
  dosageUnit: "mg",
  isArchived: false,
};

function createWrapper() {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  };
}

beforeEach(() => {
  listMock.mockReset();
  createMock.mockReset();
});

describe("useMedications", () => {
  it("retrieves and maps medications", async () => {
    listMock.mockResolvedValue([dto]);

    const { result } = renderHook(() => useMedications(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toEqual([
      expect.objectContaining({ id: "m1", name: "Metformin", schedules: [] }),
    ]);
  });

  it("exposes a typed error on network failure", async () => {
    listMock.mockRejectedValue(
      new ApiClientError({ kind: "network", message: "Network request failed." }),
    );

    const { result } = renderHook(() => useMedications(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(result.current.error).toBeInstanceOf(ApiClientError);
  });
});

describe("useCreateMedication", () => {
  it("creates a medication", async () => {
    createMock.mockResolvedValue(dto);

    const { result } = renderHook(() => useCreateMedication(), {
      wrapper: createWrapper(),
    });

    const created = await result.current.mutateAsync({
      name: "Metformin",
      dosageAmount: 500,
      dosageUnit: "mg",
    });

    expect(created.id).toBe("m1");
    expect(createMock).toHaveBeenCalledTimes(1);
  });
});
