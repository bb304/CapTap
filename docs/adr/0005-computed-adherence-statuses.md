# ADR-0005: Computed adherence statuses (no persisted Missed rows)

- Status: Accepted
- Date: 2026-08-05

## Context

Dashboard UX needs statuses: Upcoming, Due, Taken, Missed. Persisting “Missed” rows on a schedule would invent historical facts incorrectly when clocks, time zones, or schedule edits change.

## Decision

Compute adherence status at read time in `AdherenceService` from schedules, user IANA time zone, current time, and existing `MedicationLog` rows. Do **not** insert Missed log rows. Only Taken doses are persisted (manual or NFC) via `MedicationLog`.

## Consequences

- **Pros:** History stays factual; DST and schedule edits remain coherent; simpler privacy story (no fabricated clinical events).
- **Cons:** Dashboard queries do more computation; status can change as time passes without a write.
- **Follow-up:** See `docs/scheduling.md` and adherence unit tests.
