/**
 * Wipe device-local user data (SQLite caches + React Query).
 * Call on sign-out / before sign-in so accounts never share PHI on one device.
 */
import type { QueryClient } from "@tanstack/react-query";
import { LocalDatabase } from "@/services/localDatabase";
import { createSqlitePersister } from "@/services/queryPersister";

export async function clearLocalUserState(queryClient: QueryClient): Promise<void> {
  queryClient.clear();
  await LocalDatabase.clearAllUserData();
  try {
    await createSqlitePersister().removeClient();
  } catch {
    // Persister wipe is best-effort; SQLite clear above already drops query_cache.
  }
}
