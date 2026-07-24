/**
 * SQLite-backed persister for TanStack Query cache.
 * Caches dashboard, medications, schedules, and recent history for offline reads.
 */
import type { PersistedClient, Persister } from "@tanstack/react-query-persist-client";
import { LocalDatabase } from "@/services/localDatabase";

const PERSIST_KEY = "tanstack-query-cache-v1";

export function createSqlitePersister(): Persister {
  return {
    persistClient: async (client: PersistedClient) => {
      await LocalDatabase.ready();
      await LocalDatabase.setQueryCache(PERSIST_KEY, client);
    },
    restoreClient: async () => {
      await LocalDatabase.ready();
      return LocalDatabase.getQueryCache<PersistedClient>(PERSIST_KEY);
    },
    removeClient: async () => {
      await LocalDatabase.ready();
      await LocalDatabase.setQueryCache(PERSIST_KEY, null);
    },
  };
}

/** Keys we intentionally keep warm offline. */
export function shouldDehydrateQuery(query: { queryKey: readonly unknown[] }): boolean {
  const root = String(query.queryKey[0] ?? "");
  return (
    root === "dashboard" ||
    root === "medications" ||
    root === "schedules" ||
    root === "medication-logs" ||
    root === "nfc"
  );
}
