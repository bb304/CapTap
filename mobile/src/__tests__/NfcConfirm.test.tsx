import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import { medicationLogApi } from "@/api/medicationLog";
import { endpoints } from "@/api/endpoints";

const mockPost = jest.fn();

jest.mock("@/api/client", () => ({
  apiClient: {
    post: (...args: unknown[]) => mockPost(...args),
    get: jest.fn(),
  },
  unwrap: (response: { data: { data: unknown } }) => response.data.data,
}));

describe("NFC logging pipeline", () => {
  beforeEach(() => {
    mockPost.mockReset();
  });

  it("posts to the shared medication-logs endpoint with LoggingMethod Nfc", async () => {
    mockPost.mockResolvedValue({
      data: {
        success: true,
        data: {
          id: "log1",
          medicationId: "m1",
          medicationName: "Metformin",
          scheduleId: "s1",
          scheduledDoseTime: "2026-07-24T12:00:00Z",
          loggedAt: "2026-07-24T12:05:00Z",
          loggingMethod: "Nfc",
        },
      },
    });

    const result = await medicationLogApi.create({
      medicationId: "m1",
      scheduleId: "s1",
      scheduledDoseTime: "2026-07-24T12:00:00.000Z",
      loggingMethod: "Nfc",
    });

    expect(mockPost).toHaveBeenCalledWith(
      endpoints.medicationLogs.create,
      expect.objectContaining({ loggingMethod: "Nfc", medicationId: "m1" }),
    );
    expect(result.loggingMethod).toBe("Nfc");
  });
});
