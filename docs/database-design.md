# CapTap Database Design

Version: Phase 9  
Aligned with: CapTap DDD v1.0

## Overview

CapTap uses PostgreSQL with Entity Framework Core. The schema stores only what is required for medication tracking — not diagnoses, conditions, insurance, or other clinical data.

## Privacy Decisions

Stored:
- Account email + password hash
- Medication identity and dosage the user configured
- Schedules and dose logs
- NFC tag identifiers (opaque IDs only)
- Refresh token hashes
- Security audit events

Not stored:
- Diagnoses or medical conditions
- Insurance information
- Doctor / provider records
- Unnecessary demographic data
- Medication details on NFC tags (tags store only `TagIdentifier`)

## Entities

| Entity | Table | Purpose |
|--------|-------|---------|
| `User` | `Users` | Account credentials, status, IANA `TimeZoneId` |
| `Medication` | `Medications` | User-owned medication records |
| `MedicationSchedule` | `MedicationSchedules` | Daily dose times |
| `MedicationLog` | `MedicationLogs` | Taken-dose history (`LoggedAt`, `ScheduledDoseTime`, `LoggingMethod`) |
| `NfcTag` | `NfcTags` | Physical sticker → medication mapping (`UserId`, soft `IsAssigned`) |
| `RefreshToken` | `RefreshTokens` | Session refresh tokens (hashed) |
| `AuditLog` | `AuditLogs` | Security / compliance events (+ optional `Metadata`) |

All entities inherit `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`).

## Relationships

```
User 1 ─── * Medication
Medication 1 ─── * MedicationSchedule
Medication 1 ─── * MedicationLog
User 1 ─── * MedicationLog
User 1 ─── * NfcTag
Medication 1 ─── * NfcTag (at most one with IsAssigned = true)
User 1 ─── * RefreshToken
```

Delete behaviors:
- Medication → Schedules: **Cascade**
- Medication → NfcTags: **Restrict** (soft-unassign; never hard-delete tags)
- Medication → Logs: **Restrict** (history preserved)
- User → Medications / Logs / NfcTags: **Restrict**
- User → RefreshTokens: **Cascade**

## Enums (stored as strings)

`FrequencyType`: `OnceDaily`, `TwiceDaily`, `ThreeTimesDaily`  
`LoggingMethod`: `Manual`, `Nfc`

## Indexes

- `IX_Users_Email` (unique)
- `IX_Medications_UserId`
- `IX_Medications_FdaIdentifier`
- `IX_MedicationSchedules_MedicationId`
- `IX_MedicationLogs_UserId`
- `IX_MedicationLogs_LoggedAt`
- `IX_MedicationLogs_ScheduledDoseTime`
- `IX_MedicationLogs_MedicationId`
- `IX_MedicationLogs_User_Schedule_ScheduledDoseTime` (unique — one log per scheduled occurrence)
- `IX_NfcTags_TagIdentifier` (unique)
- `IX_NfcTags_MedicationId_Assigned` (unique filtered — one **assigned** tag per medication)
- `IX_NfcTags_UserId`
- `IX_RefreshTokens_UserId`
- `IX_RefreshTokens_TokenHash`
- `IX_AuditLogs_UserId`
- `IX_AuditLogs_CreatedAt`

## Migration Instructions

From `server/`:

```bash
export PATH="$HOME/.dotnet:$HOME/.dotnet/.dotnet/tools:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export DOCKER_HOST="unix://${HOME}/.colima/docker.sock"

docker compose up -d

dotnet ef migrations add MigrationName \
  --project CapTap.Infrastructure \
  --startup-project CapTap.Api \
  --output-dir Migrations

dotnet ef database update \
  --project CapTap.Infrastructure \
  --startup-project CapTap.Api
```

Current schema migrations (apply in order via `dotnet ef database update`):

- `InitialCreate` … through Phase 5 scheduling
- `MedicationLoggingFields` (Phase 8)
- `NfcAndUserTimeZone` (Phase 9)

## Design Notes

- Soft archive via `Medication.IsArchived` rather than hard delete for active meds.
- NFC tag identifiers are unique; at most one **assigned** sticker per medication (`IsAssigned` soft flag; rows are never hard-deleted).
- User `TimeZoneId` (IANA) drives local-day adherence; timestamps remain UTC.
- See `docs/nfc-integration.md` and `docs/medication-logging.md` for logging + NFC behavior.
