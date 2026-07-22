# CapTap Database Design

Version: Phase 2  
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
| `User` | `Users` | Account credentials and status |
| `Medication` | `Medications` | User-owned medication records |
| `MedicationSchedule` | `MedicationSchedules` | Daily dose times |
| `MedicationLog` | `MedicationLogs` | Taken-dose history |
| `NfcTag` | `NfcTags` | Physical sticker → medication mapping |
| `RefreshToken` | `RefreshTokens` | Session refresh tokens (hashed) |
| `AuditLog` | `AuditLogs` | Security / compliance events |

All entities inherit `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`).

## Relationships

```
User 1 ─── * Medication
Medication 1 ─── * MedicationSchedule
Medication 1 ─── * MedicationLog
User 1 ─── * MedicationLog
Medication 1 ─── 0..1 NfcTag
User 1 ─── * RefreshToken
```

Delete behaviors:
- Medication → Schedules: **Cascade**
- Medication → NfcTag: **Cascade**
- Medication → Logs: **Restrict** (history preserved)
- User → Medications / Logs: **Restrict**
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
- `IX_MedicationLogs_TakenAt`
- `IX_MedicationLogs_MedicationId`
- `IX_NfcTags_TagIdentifier` (unique)
- `IX_NfcTags_MedicationId` (unique — one tag per medication)
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

Current schema migration: **`InitialCreate`**

## Design Notes

- Soft archive via `Medication.IsArchived` rather than hard delete for active meds.
- NFC uniqueness is enforced in the database so the same sticker cannot map to two medications.
- Auth token / email-verification tables from later auth work are intentionally deferred to Phase 3.
