/**
 * CapTap local SQLite — sync queue + query cache.
 * Not a second source of truth: the API remains authoritative after sync.
 */
import * as SQLite from "expo-sqlite";

const DB_NAME = "captap-offline.db";

export type QueueStatus = "pending" | "syncing" | "failed" | "conflict";

export type OfflineQueueItem = {
  id: string;
  kind: "medication_log";
  payloadJson: string;
  clientRequestId: string;
  status: QueueStatus;
  attempts: number;
  lastError: string | null;
  createdAt: string;
  updatedAt: string;
};

export type SyncMeta = {
  key: string;
  value: string;
  updatedAt: string;
};

let dbPromise: Promise<SQLite.SQLiteDatabase> | null = null;

async function getDb(): Promise<SQLite.SQLiteDatabase> {
  if (!dbPromise) {
    dbPromise = (async () => {
      const db = await SQLite.openDatabaseAsync(DB_NAME);
      await db.execAsync(`
        PRAGMA journal_mode = WAL;
        CREATE TABLE IF NOT EXISTS offline_queue (
          id TEXT PRIMARY KEY NOT NULL,
          kind TEXT NOT NULL,
          payload_json TEXT NOT NULL,
          client_request_id TEXT NOT NULL UNIQUE,
          status TEXT NOT NULL,
          attempts INTEGER NOT NULL DEFAULT 0,
          last_error TEXT,
          created_at TEXT NOT NULL,
          updated_at TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_offline_queue_status_created
          ON offline_queue (status, created_at);
        CREATE TABLE IF NOT EXISTS query_cache (
          cache_key TEXT PRIMARY KEY NOT NULL,
          value_json TEXT NOT NULL,
          updated_at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS nfc_tag_cache (
          tag_identifier TEXT PRIMARY KEY NOT NULL,
          resolve_json TEXT NOT NULL,
          updated_at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS sync_meta (
          key TEXT PRIMARY KEY NOT NULL,
          value TEXT NOT NULL,
          updated_at TEXT NOT NULL
        );
      `);
      return db;
    })();
  }
  return dbPromise;
}

function nowIso(): string {
  return new Date().toISOString();
}

function newId(): string {
  return `q_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 10)}`;
}

export const LocalDatabase = {
  async ready(): Promise<boolean> {
    await getDb();
    return true;
  },

  async enqueueMedicationLog(
    payload: unknown,
    clientRequestId?: string,
  ): Promise<OfflineQueueItem> {
    const db = await getDb();
    const id = newId();
    const requestId = clientRequestId ?? id;
    const createdAt = nowIso();
    const item: OfflineQueueItem = {
      id,
      kind: "medication_log",
      payloadJson: JSON.stringify(payload),
      clientRequestId: requestId,
      status: "pending",
      attempts: 0,
      lastError: null,
      createdAt,
      updatedAt: createdAt,
    };

    await db.runAsync(
      `INSERT INTO offline_queue
        (id, kind, payload_json, client_request_id, status, attempts, last_error, created_at, updated_at)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      item.id,
      item.kind,
      item.payloadJson,
      item.clientRequestId,
      item.status,
      item.attempts,
      item.lastError,
      item.createdAt,
      item.updatedAt,
    );

    return item;
  },

  async listPendingQueue(): Promise<OfflineQueueItem[]> {
    const db = await getDb();
    const rows = await db.getAllAsync<{
      id: string;
      kind: string;
      payload_json: string;
      client_request_id: string;
      status: string;
      attempts: number;
      last_error: string | null;
      created_at: string;
      updated_at: string;
    }>(
      `SELECT * FROM offline_queue
       WHERE status IN ('pending', 'failed')
       ORDER BY created_at ASC`,
    );

    return rows.map((row) => ({
      id: row.id,
      kind: row.kind as OfflineQueueItem["kind"],
      payloadJson: row.payload_json,
      clientRequestId: row.client_request_id,
      status: row.status as QueueStatus,
      attempts: row.attempts,
      lastError: row.last_error,
      createdAt: row.created_at,
      updatedAt: row.updated_at,
    }));
  },

  async markQueueStatus(
    id: string,
    status: QueueStatus,
    extras?: { attempts?: number; lastError?: string | null },
  ): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      `UPDATE offline_queue
       SET status = ?, attempts = COALESCE(?, attempts), last_error = ?, updated_at = ?
       WHERE id = ?`,
      status,
      extras?.attempts ?? null,
      extras?.lastError ?? null,
      nowIso(),
      id,
    );
  },

  async removeQueueItem(id: string): Promise<void> {
    const db = await getDb();
    await db.runAsync(`DELETE FROM offline_queue WHERE id = ?`, id);
  },

  async countPending(): Promise<number> {
    const db = await getDb();
    const row = await db.getFirstAsync<{ c: number }>(
      `SELECT COUNT(*) AS c FROM offline_queue WHERE status IN ('pending', 'failed', 'syncing')`,
    );
    return row?.c ?? 0;
  },

  async setQueryCache(cacheKey: string, value: unknown): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      `INSERT INTO query_cache (cache_key, value_json, updated_at)
       VALUES (?, ?, ?)
       ON CONFLICT(cache_key) DO UPDATE SET
         value_json = excluded.value_json,
         updated_at = excluded.updated_at`,
      cacheKey,
      JSON.stringify(value),
      nowIso(),
    );
  },

  async getQueryCache<T>(cacheKey: string): Promise<T | null> {
    const db = await getDb();
    const row = await db.getFirstAsync<{ value_json: string }>(
      `SELECT value_json FROM query_cache WHERE cache_key = ?`,
      cacheKey,
    );
    if (!row) return null;
    try {
      return JSON.parse(row.value_json) as T;
    } catch {
      return null;
    }
  },

  async setNfcTagCache(tagIdentifier: string, resolvePayload: unknown): Promise<void> {
    const db = await getDb();
    const normalized = tagIdentifier.trim().toUpperCase();
    await db.runAsync(
      `INSERT INTO nfc_tag_cache (tag_identifier, resolve_json, updated_at)
       VALUES (?, ?, ?)
       ON CONFLICT(tag_identifier) DO UPDATE SET
         resolve_json = excluded.resolve_json,
         updated_at = excluded.updated_at`,
      normalized,
      JSON.stringify(resolvePayload),
      nowIso(),
    );
  },

  async getNfcTagCache<T>(tagIdentifier: string): Promise<T | null> {
    const db = await getDb();
    const normalized = tagIdentifier.trim().toUpperCase();
    const row = await db.getFirstAsync<{ resolve_json: string }>(
      `SELECT resolve_json FROM nfc_tag_cache WHERE tag_identifier = ?`,
      normalized,
    );
    if (!row) return null;
    try {
      return JSON.parse(row.resolve_json) as T;
    } catch {
      return null;
    }
  },

  async setMeta(key: string, value: string): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      `INSERT INTO sync_meta (key, value, updated_at)
       VALUES (?, ?, ?)
       ON CONFLICT(key) DO UPDATE SET value = excluded.value, updated_at = excluded.updated_at`,
      key,
      value,
      nowIso(),
    );
  },

  async getMeta(key: string): Promise<string | null> {
    const db = await getDb();
    const row = await db.getFirstAsync<{ value: string }>(
      `SELECT value FROM sync_meta WHERE key = ?`,
      key,
    );
    return row?.value ?? null;
  },

  /** Test helper — wipe tables. */
  async __resetForTests(): Promise<void> {
    const db = await getDb();
    await db.execAsync(`
      DELETE FROM offline_queue;
      DELETE FROM query_cache;
      DELETE FROM nfc_tag_cache;
      DELETE FROM sync_meta;
    `);
  },
};
