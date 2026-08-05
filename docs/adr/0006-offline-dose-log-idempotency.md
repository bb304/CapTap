# ADR-0006: Offline dose-log idempotency via unique constraint and HTTP 409

- Status: Accepted
- Date: 2026-08-05

## Context

The mobile app queues dose logs in SQLite and replays them when connectivity returns. Retries and duplicate queue drains must not create double logs for the same scheduled occurrence.

## Decision

- Enforce uniqueness on `(UserId, ScheduleId, ScheduledDoseTime)` for medication logs in EF configuration / PostgreSQL.
- On conflict, the API returns **409 Conflict** (`ConflictException`).
- The mobile sync engine treats **409 as success** (already logged) so offline replay is idempotent.

## Consequences

- **Pros:** Safe FIFO replay; API remains source of truth; no client-generated idempotency keys required for MVP.
- **Cons:** Clients must map 409 correctly; unrelated conflicts must not be blindly ignored (only the “already logged” path).
- **Follow-up:** Covered in `docs/offline-architecture.md` and logging/offline tests.
