/**
 * In-memory stand-in for expo-sqlite so offline queue logic is unit-testable
 * without native modules.
 */
type Row = Record<string, unknown>;

const tables: Record<string, Row[]> = {
  offline_queue: [],
  query_cache: [],
  nfc_tag_cache: [],
  sync_meta: [],
};

function reset() {
  tables.offline_queue = [];
  tables.query_cache = [];
  tables.nfc_tag_cache = [];
  tables.sync_meta = [];
}

const db = {
  execAsync: jest.fn(async () => undefined),
  runAsync: jest.fn(async (sql: string, ...params: unknown[]) => {
    if (sql.includes("INSERT INTO offline_queue")) {
      tables.offline_queue.push({
        id: params[0],
        kind: params[1],
        payload_json: params[2],
        client_request_id: params[3],
        status: params[4],
        attempts: params[5],
        last_error: params[6],
        created_at: params[7],
        updated_at: params[8],
      });
      return;
    }
    if (sql.includes("UPDATE offline_queue")) {
      const row = tables.offline_queue.find((r) => r.id === params[4]);
      if (row) {
        row.status = params[0];
        if (params[1] != null) row.attempts = params[1];
        row.last_error = params[2];
        row.updated_at = params[3];
      }
      return;
    }
    if (sql.includes("DELETE FROM offline_queue")) {
      if (sql.includes("DELETE FROM offline_queue;") || params.length === 0) {
        // handled in exec for full wipe
      }
      tables.offline_queue = tables.offline_queue.filter((r) => r.id !== params[0]);
      return;
    }
    if (sql.includes("INSERT INTO query_cache")) {
      const existing = tables.query_cache.find((r) => r.cache_key === params[0]);
      if (existing) {
        existing.value_json = params[1];
        existing.updated_at = params[2];
      } else {
        tables.query_cache.push({
          cache_key: params[0],
          value_json: params[1],
          updated_at: params[2],
        });
      }
      return;
    }
    if (sql.includes("INSERT INTO nfc_tag_cache")) {
      const existing = tables.nfc_tag_cache.find((r) => r.tag_identifier === params[0]);
      if (existing) {
        existing.resolve_json = params[1];
        existing.updated_at = params[2];
      } else {
        tables.nfc_tag_cache.push({
          tag_identifier: params[0],
          resolve_json: params[1],
          updated_at: params[2],
        });
      }
      return;
    }
    if (sql.includes("INSERT INTO sync_meta")) {
      const existing = tables.sync_meta.find((r) => r.key === params[0]);
      if (existing) {
        existing.value = params[1];
        existing.updated_at = params[2];
      } else {
        tables.sync_meta.push({
          key: params[0],
          value: params[1],
          updated_at: params[2],
        });
      }
    }
  }),
  getAllAsync: jest.fn(async (sql: string) => {
    if (sql.includes("FROM offline_queue")) {
      return tables.offline_queue
        .filter((r) => r.status === "pending" || r.status === "failed")
        .sort((a, b) => String(a.created_at).localeCompare(String(b.created_at)));
    }
    return [];
  }),
  getFirstAsync: jest.fn(async (sql: string, ...params: unknown[]) => {
    if (sql.includes("COUNT(*)")) {
      const c = tables.offline_queue.filter((r) =>
        ["pending", "failed", "syncing"].includes(String(r.status)),
      ).length;
      return { c };
    }
    if (sql.includes("FROM query_cache")) {
      return tables.query_cache.find((r) => r.cache_key === params[0]) ?? null;
    }
    if (sql.includes("FROM nfc_tag_cache")) {
      return tables.nfc_tag_cache.find((r) => r.tag_identifier === params[0]) ?? null;
    }
    if (sql.includes("FROM sync_meta")) {
      return tables.sync_meta.find((r) => r.key === params[0]) ?? null;
    }
    return null;
  }),
};

export const __sqliteMock = { tables, reset, db };

export async function openDatabaseAsync() {
  return db;
}
