# CapTap Scheduling & Adherence

Phase 5 core engine: answer *“Did I take my medication today?”* by projecting expected doses from schedules and comparing them to logs.

CapTap is not a clinical system. Schedules and statuses organize adherence tracking only.

## Schedule model

Each **scheduled clock time** is its own `MedicationSchedules` row (no time arrays).

| Field | Notes |
|-------|--------|
| `MedicationId` | Owner medication (scoped via medication `UserId`) |
| `Frequency` | `OnceDaily` / `TwiceDaily` / `ThreeTimesDaily` (string in DB) |
| `ScheduledTime` | `TimeOnly` (`time` column) |
| `DoseQuantity` | Must be &gt; 0 |
| `IsActive` | Soft-delete sets false |
| `EffectiveFrom` / `EffectiveTo` | Inclusive date window (UTC dates) |

**Example — Metformin twice daily**

| Row | Time |
|-----|------|
| 1 | 08:00 |
| 2 | 20:00 |

Duplicate active `(MedicationId, ScheduledTime)` pairs are rejected.

## Adherence engine

`AdherenceService` is **read-only**: it never writes missed logs. Status is computed when the dashboard is loaded.

```
Active schedules for date
        ↓
Logs for that user/date (matched by ScheduleId)
        ↓
CalculateStatus(scheduledTime, doseDate, now, hasLog)
        ↓
TodayDoseDto[]
```

### Business rules

| Status | Rule |
|--------|------|
| **Taken** | A medication log exists for that schedule on that calendar day |
| **Upcoming** | Same day, current time **before** scheduled time, no log |
| **Due** | Same day, scheduled time has passed, no log |
| **Missed** | Calendar day has ended, no log (computed dynamically; no auto-insert) |

Sort for `/dashboard/today`: **Due → Upcoming → Taken → Missed**, then by time.

## Time zone basis (Phase 9+)

Schedule wall-clock times are evaluated in the user's IANA zone (`Users.TimeZoneId`).
Persisted timestamps remain UTC. See `docs/nfc-integration.md`.

## API endpoints

All require Bearer JWT. Ownership enforced (same 404 if missing or not owned).

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/v1/medications/{id}/schedules` | List schedules |
| POST | `/api/v1/medications/{id}/schedules` | Create |
| PATCH | `/api/v1/schedules/{scheduleId}` | Update |
| DELETE | `/api/v1/schedules/{scheduleId}` | Soft-delete (`IsActive=false`) |
| GET | `/api/v1/dashboard/today` | Today's doses + status |
| GET | `/api/v1/dashboard/missed` | Missed doses from the last 7 completed days |

### Create schedule example

```json
{
  "frequency": "TwiceDaily",
  "scheduledTime": "08:00:00",
  "doseQuantity": 1
}
```

### Today response example

```json
{
  "success": true,
  "data": [
    {
      "medicationId": "…",
      "medicationName": "Metformin",
      "scheduledTime": "08:00:00",
      "doseQuantity": 1,
      "status": "Taken",
      "logId": "…",
      "scheduleId": "…"
    },
    {
      "medicationName": "Vitamin D",
      "scheduledTime": "20:00:00",
      "doseQuantity": 1,
      "status": "Upcoming",
      "logId": null,
      "scheduleId": "…"
    }
  ]
}
```

## Sequence diagrams

### Creating a schedule

```
Mobile App → POST /medications/{id}/schedules
    → ScheduleService (validate, ownership, duplicate check)
    → MedicationSchedules insert
    → Audit SCHEDULE_CREATED
    → ScheduleResponseDto
```

### Loading today's medications

```
Mobile App → GET /dashboard/today
    → AdherenceService.GetTodayDosesAsync
    → Load active schedules for UTC today
    → Load today's logs
    → CalculateStatus per schedule (no writes)
    → Sorted TodayDoseDto[]
```

### Determining adherence status

```
hasLog? → Taken
doseDate < today? → Missed
doseDate > today? → Upcoming
now < scheduledTime? → Upcoming
else → Due
```

## Audit events

- `SCHEDULE_CREATED`
- `SCHEDULE_UPDATED`
- `SCHEDULE_DELETED`

## Database

Migration `ScheduleAdherenceFields` adds `IsActive`, `EffectiveFrom`, `EffectiveTo` to `MedicationSchedules`.
