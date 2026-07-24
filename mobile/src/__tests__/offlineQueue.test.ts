import { describe, expect, it, beforeEach, jest } from "@jest/globals";
import { onlineManager } from "@tanstack/react-query";
import { LocalDatabase } from "@/services/localDatabase";
import { processOfflineQueue } from "@/services/syncEngine";
import { medicationLogApi } from "@/api/medicationLog";
import { ApiClientError } from "@/api/errors";
import { __sqliteMock } from "@/__mocks__/expo-sqlite";

jest.mock("@/api/medicationLog", () => ({
  medicationLogApi: {
    create: jest.fn(),
    history: jest.fn(),
  },
}));

const createMock = medicationLogApi.create as jest.MockedFunction<typeof medicationLogApi.create>;

describe("offline queue + sync", () => {
  beforeEach(async () => {
    __sqliteMock.reset();
    createMock.mockReset();
    onlineManager.setOnline(true);
    await LocalDatabase.ready();
  });

  it("enqueues medication logs and lists them FIFO", async () => {
    await LocalDatabase.enqueueMedicationLog({
      medicationId: "m1",
      scheduledDoseTime: "2026-07-24T12:00:00.000Z",
      loggingMethod: "Manual",
      scheduleId: "s1",
    });
    await LocalDatabase.enqueueMedicationLog({
      medicationId: "m1",
      scheduledDoseTime: "2026-07-24T20:00:00.000Z",
      loggingMethod: "Nfc",
      scheduleId: "s2",
    });

    const pending = await LocalDatabase.listPendingQueue();
    expect(pending).toHaveLength(2);
    expect(pending[0].kind).toBe("medication_log");
    expect(JSON.parse(pending[0].payloadJson).scheduleId).toBe("s1");
  });

  it("replays the queue and removes synced items", async () => {
    await LocalDatabase.enqueueMedicationLog({
      medicationId: "m1",
      scheduledDoseTime: "2026-07-24T12:00:00.000Z",
      loggingMethod: "Manual",
      scheduleId: "s1",
    });
    createMock.mockResolvedValue({
      id: "server-1",
      medicationId: "m1",
      medicationName: "Metformin",
      scheduleId: "s1",
      scheduledDoseTime: "2026-07-24T12:00:00.000Z",
      loggedAt: "2026-07-24T12:05:00.000Z",
      loggingMethod: "Manual",
    });

    const result = await processOfflineQueue();
    expect(result.synced).toBe(1);
    expect(await LocalDatabase.listPendingQueue()).toHaveLength(0);
    expect(createMock).toHaveBeenCalledTimes(1);
  });

  it("treats duplicate conflicts as successful sync (no duplicates)", async () => {
    await LocalDatabase.enqueueMedicationLog({
      medicationId: "m1",
      scheduledDoseTime: "2026-07-24T12:00:00.000Z",
      loggingMethod: "Nfc",
      scheduleId: "s1",
    });
    createMock.mockRejectedValue(
      new ApiClientError({
        kind: "conflict",
        message: "This dose has already been logged.",
        status: 409,
      }),
    );

    const result = await processOfflineQueue();
    expect(result.synced).toBe(0);
    expect(result.conflicts).toHaveLength(1);
    expect(result.conflicts[0].recoverable).toBe(true);
    expect(await LocalDatabase.listPendingQueue()).toHaveLength(0);
  });

  it("marks unrecoverable 404 conflicts without deleting diagnostics", async () => {
    await LocalDatabase.enqueueMedicationLog({
      medicationId: "gone",
      scheduledDoseTime: "2026-07-24T12:00:00.000Z",
      loggingMethod: "Manual",
      scheduleId: "s1",
    });
    createMock.mockRejectedValue(
      new ApiClientError({
        kind: "notFound",
        message: "Medication was not found.",
        status: 404,
      }),
    );

    const result = await processOfflineQueue();
    expect(result.conflicts[0].recoverable).toBe(false);
    const remaining = __sqliteMock.tables.offline_queue;
    expect(remaining).toHaveLength(1);
    expect(remaining[0].status).toBe("conflict");
  });

  it("caches NFC resolve payloads for offline scans", async () => {
    await LocalDatabase.setNfcTagCache("04:AA", {
      medicationId: "m1",
      medicationName: "Metformin",
      tagIdentifier: "04:AA",
    });
    const cached = await LocalDatabase.getNfcTagCache<{ medicationName: string }>("04:aa");
    expect(cached?.medicationName).toBe("Metformin");
  });
});
