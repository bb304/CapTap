# CapTap Medication Logging & Adherence

Phase 8 completes the core user journey: **"Did I take my medication today?"**

Users can manually mark a dose as taken. NFC reuses the **same** logging endpoint (`LoggingMethod = Nfc`) — see `docs/nfc-integration.md`.

## Medication logging architecture

```
Dashboard (Mark as Taken)
        ↓
POST /api/v1/medication-logs
        ↓
MedicationLogService
  • ownership check
  • today's active schedule match
  • duplicate prevention
  • LoggedAt = now (confirmation time)
  • ScheduledDoseTime = expected occurrence
        ↓
MedicationLogs (+ audit MEDICATION_LOGGED)
        ↓
Invalidate dashboard / history queries
        ↓
Dose status → Taken
```

### Domain model (`MedicationLog`)

| Field | Meaning |
|-------|---------|
| `MedicationId` / `UserId` | Ownership |
| `ScheduleId` | Links to the schedule row that produced the dose |
| `ScheduledDoseTime` | UTC datetime of the expected dose (calendar day + clock time) |
| `LoggedAt` | UTC datetime when the user confirmed the dose |
| `LoggingMethod` | `Manual` or `Nfc` (string in DB) |
| `Notes` | Optional free text (max 500) |

Enums stored as strings. Duplicate protection: unique index on `(UserId, ScheduleId, ScheduledDoseTime)`.

## Dose logging endpoint

`POST /api/v1/medication-logs`

```json
{
  "medicationId": "...",
  "scheduledDoseTime": "2026-07-24T08:00:00Z",
  "loggingMethod": "Manual",
  "scheduleId": "...",
  "notes": null
}
```

Rules:

1. User must own the medication (otherwise identical **404**).
2. Dose must belong to **today's** active schedule (user's local calendar day via `Users.TimeZoneId`).
3. Matching schedule is resolved by `scheduleId` or by medication + clock time.
4. Duplicate logs for the same scheduled occurrence are rejected (**409 Conflict**).
5. Existing logs are never overwritten.
6. `LoggingMethod.Nfc` is used by the Phase 9 NFC confirmation flow on this same endpoint.

## History API

`GET /api/v1/medication-logs/history?page=1&pageSize=20&medicationId=`

- Newest first (`LoggedAt` descending)
- Pagination (`page`, `pageSize` clamped 1–100)
- Optional medication filter
- Returns medication name, scheduled/logged times, method

## Streak calculation rules

`IAdherenceStreakService` / `GET /api/v1/dashboard/streak`

```
For each day in lookback (365 days):
  project expected doses (schedules + logs)
  if no schedules → skip (neither completes nor breaks)
  if today → ignore Upcoming (future) doses
  day complete ⇔ every relevant dose is Taken

Current streak:
  if today incomplete → 0
  if today in progress → count consecutive complete days ending yesterday
  if today complete → include today, walk backwards

Longest streak:
  max consecutive complete days in the lookback window
```

Missed days reset the current streak. Future doses do not break today's streak.

## Dashboard statistics

`GET /api/v1/dashboard/today` now returns server-computed stats:

```json
{
  "doses": [ /* TodayDoseDto[] */ ],
  "completionPercent": 50,
  "currentStreakDays": 3,
  "longestStreakDays": 12,
  "totalMedicationsToday": 4,
  "takenCount": 2,
  "missedCount": 0
}
```

`GET /api/v1/dashboard/missed` unchanged.  
`GET /api/v1/dashboard/streak` exposes streak alone for dedicated clients.

## Medication list N+1 fix

**Chosen approach:** include active schedules on `GET /api/v1/medications` and `GET /api/v1/medications/{id}` (`schedules` array on `MedicationResponseDto`).

**Why:** list and detail views always need schedule times for cards. Embedding one query with `Include` avoids N client round-trips per medication. Dedicated schedule endpoints remain for create/update/delete.

## Authentication improvements

Proactive access-token refresh (`mobile/src/api/tokenRefresh.ts`):

1. After login / restore / refresh, read JWT `exp`.
2. Schedule a refresh **60 seconds** before expiry.
3. Cap `setTimeout` delay to the 32-bit maximum to avoid overflow.
4. Reactive Axios `401` refresh remains the fallback.

## Environment configuration

### Local — simulator / web

```bash
# mobile/.env.development
EXPO_PUBLIC_API_URL=http://localhost:5001
```

### Local — physical device (same Wi-Fi)

```bash
# Find your Mac LAN IP (e.g. System Settings → Network, or `ipconfig getifaddr en0`)
EXPO_PUBLIC_API_URL=http://192.168.1.100:5001
```

Restart Expo after changing the URL (`npx expo start -c`).

### Production

```bash
# mobile/.env.production
EXPO_PUBLIC_API_URL=https://api.captap.app
```

- HTTPS only
- No hardcoded LAN IPs
- Never put secrets in `EXPO_PUBLIC_*` variables

## Offline architecture prep (Phase 11)

TanStack Query is configured with:

- `refetchOnReconnect: true`
- `networkMode: "online"`
- `onlineManager` imported and documented as the NetInfo wiring point

**Do not install NetInfo yet.** When Phase 11 starts:

```ts
import NetInfo from "@react-native-community/netinfo";
onlineManager.setEventListener((setOnline) =>
  NetInfo.addEventListener((state) => {
    setOnline(Boolean(state.isConnected && state.isInternetReachable !== false));
  }),
);
```

## Audit events

| Action | When |
|--------|------|
| `MEDICATION_LOGGED` | Successful dose log |
| `MEDICATION_LOG_EDITED` | Reserved for future edit support |
| `MEDICATION_LOG_DELETED` | Reserved for future delete support |

Events record user id, entity id, timestamp, and IP via `IAuditService`.

## Sequence diagrams

### Manual logging

```
User → Dashboard "Mark as Taken"
    → Optimistic UI (status Taken)
    → POST /medication-logs { Manual }
    → MedicationLogService validates + persists
    → Audit MEDICATION_LOGGED
    → Invalidate dashboard + history
    → Confirm Taken (or rollback on error)
```

### Dashboard refresh

```
Pull-to-refresh / focus / after log
    → GET /dashboard/today  (doses + stats)
    → GET /dashboard/missed
    → mapDashboardToday → UI
```

### Streak calculation

```
GET /dashboard/streak (or embedded in /today)
    → For each day: project doses → complete?
    → Walk backwards for current streak
    → Scan window for longest streak
```

### NFC logging (Phase 9)

Implemented — see `docs/nfc-integration.md`.

```
NFC tap → resolve tag → medicationId + schedule
    → Confirmation UI
    → POST /medication-logs { loggingMethod: "Nfc", ... }
    → Same validation, duplicate rules, dashboard invalidation
```

Adherence "today" and streaks use the user's IANA time zone (UTC storage).

## Mobile integration summary

- `DoseCard` shows **Mark as Taken** for Due / Upcoming doses
- `useLogMedication` optimistic update + cache invalidation
- History screen at `/history` (Settings → Dose history)
- Dashboard shows current / longest streak and completion from the server
- Schedules arrive with the medication list (no N+1)

## Migration

```bash
cd server
dotnet ef database update \
  --project CapTap.Infrastructure \
  --startup-project CapTap.Api
```

Migration `MedicationLoggingFields`: renames `TakenAt` → `LoggedAt`, adds `ScheduledDoseTime` + `Notes`, unique duplicate index.
