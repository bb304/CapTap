/**
 * Offline sync engine — replays SQLite queue when NetInfo reports online.
 * Conflict policy: treat 409 / "already logged" as success (idempotent),
 * drop 404 archived/missing meds as unrecoverable conflict, retry transients.
 */
import { onlineManager } from "@tanstack/react-query";
import type { QueryClient } from "@tanstack/react-query";
import { medicationLogApi } from "@/api/medicationLog";
import { ApiClientError, isRetryableError } from "@/api/errors";
import type { CreateMedicationLogRequest, MedicationLogDto } from "@/api/types";
import { LocalDatabase, type OfflineQueueItem } from "@/services/localDatabase";
import { queryKeys } from "@/constants/queryKeys";

export type SyncConflict = {
  queueId: string;
  reason: string;
  recoverable: boolean;
};

export type SyncResult = {
  synced: number;
  conflicts: SyncConflict[];
  failed: number;
};

const MAX_ATTEMPTS = 5;
let syncing = false;
let listenersAttached = false;

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function backoffMs(attempts: number): number {
  return Math.min(1000 * 2 ** Math.max(0, attempts - 1), 30_000);
}

function isDuplicateConflict(error: unknown): boolean {
  if (!(error instanceof ApiClientError)) return false;
  if (error.kind === "conflict") return true;
  return /already been logged|already logged|duplicate/i.test(error.message);
}

function isUnrecoverable(error: unknown): boolean {
  if (!(error instanceof ApiClientError)) return false;
  return error.kind === "notFound" || error.kind === "validation";
}

async function replayMedicationLog(
  item: OfflineQueueItem,
): Promise<"synced" | "conflict" | "retry" | "drop"> {
  const payload = JSON.parse(item.payloadJson) as CreateMedicationLogRequest;

  try {
    await LocalDatabase.markQueueStatus(item.id, "syncing", {
      attempts: item.attempts + 1,
    });
    await medicationLogApi.create(payload);
    await LocalDatabase.removeQueueItem(item.id);
    return "synced";
  } catch (error) {
    if (isDuplicateConflict(error)) {
      // Already on server — treat as success (no duplicate).
      await LocalDatabase.removeQueueItem(item.id);
      return "conflict";
    }

    if (isUnrecoverable(error)) {
      await LocalDatabase.markQueueStatus(item.id, "conflict", {
        attempts: item.attempts + 1,
        lastError: error instanceof Error ? error.message : "Unrecoverable sync error",
      });
      return "drop";
    }

    const attempts = item.attempts + 1;
    if (isRetryableError(error) && attempts < MAX_ATTEMPTS) {
      await LocalDatabase.markQueueStatus(item.id, "failed", {
        attempts,
        lastError: error instanceof Error ? error.message : "Transient failure",
      });
      await sleep(backoffMs(attempts));
      return "retry";
    }

    await LocalDatabase.markQueueStatus(item.id, "failed", {
      attempts,
      lastError: error instanceof Error ? error.message : "Sync failed",
    });
    return "retry";
  }
}

export async function processOfflineQueue(queryClient?: QueryClient): Promise<SyncResult> {
  if (syncing) {
    return { synced: 0, conflicts: [], failed: 0 };
  }
  if (!onlineManager.isOnline()) {
    return { synced: 0, conflicts: [], failed: 0 };
  }

  syncing = true;
  const result: SyncResult = { synced: 0, conflicts: [], failed: 0 };

  try {
    await LocalDatabase.ready();
    let pending = await LocalDatabase.listPendingQueue();

    while (pending.length > 0 && onlineManager.isOnline()) {
      const item = pending[0];
      if (item.kind !== "medication_log") {
        await LocalDatabase.removeQueueItem(item.id);
        pending = await LocalDatabase.listPendingQueue();
        continue;
      }

      const outcome = await replayMedicationLog(item);
      if (outcome === "synced") {
        result.synced += 1;
      } else if (outcome === "conflict") {
        result.conflicts.push({
          queueId: item.id,
          reason: "Dose already logged on the server — kept a single record.",
          recoverable: true,
        });
      } else if (outcome === "drop") {
        result.conflicts.push({
          queueId: item.id,
          reason:
            "Could not sync this log (medication missing or invalid). It was kept for diagnostics.",
          recoverable: false,
        });
        result.failed += 1;
        // Stop replaying the same head item forever.
        break;
      } else {
        result.failed += 1;
        // Transient — stop this pass; next reconnect retries.
        break;
      }

      pending = await LocalDatabase.listPendingQueue();
    }

    await LocalDatabase.setMeta("lastSyncAt", new Date().toISOString());

    if (queryClient && (result.synced > 0 || result.conflicts.length > 0)) {
      await queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      await queryClient.invalidateQueries({ queryKey: queryKeys.logHistory });
      await queryClient.invalidateQueries({ queryKey: ["offline-queue", "count"] });
    }
  } finally {
    syncing = false;
  }

  return result;
}

/** Attach a one-time online listener that drains the queue. */
export function attachSyncEngine(queryClient: QueryClient): () => void {
  if (listenersAttached) {
    return () => undefined;
  }
  listenersAttached = true;

  const unsubscribe = onlineManager.subscribe((online) => {
    if (online) {
      void processOfflineQueue(queryClient);
    }
  });

  // Kick once on attach if already online.
  if (onlineManager.isOnline()) {
    void processOfflineQueue(queryClient);
  }

  return () => {
    unsubscribe();
    listenersAttached = false;
  };
}

/** Optimistic local log DTO used while pending sync. */
export function buildOptimisticLog(
  request: CreateMedicationLogRequest,
  medicationName: string,
  clientId: string,
): MedicationLogDto {
  return {
    id: `local-${clientId}`,
    medicationId: request.medicationId,
    medicationName,
    scheduleId: request.scheduleId,
    scheduledDoseTime: request.scheduledDoseTime,
    loggedAt: new Date().toISOString(),
    loggingMethod: request.loggingMethod,
    notes: request.notes,
  };
}
