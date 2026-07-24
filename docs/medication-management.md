# CapTap Medication Management

Phase 4 personal medication list. CapTap helps users answer *“Did I take my medication today?”* — it is **not** a medical provider and does not give clinical advice.

## User flow

```
Search (OpenFDA lookup)
    ↓
Select a matching result
    ↓
Customize dosage / form / instructions
    ↓
Save to My Medications
    ↓
List / update / archive as needed
```

Schedule configuration is Phase 5 (`docs/scheduling.md`). NFC pairing is Phase 9 (`docs/nfc-integration.md`). This phase stores the medication record the user confirms.

## Architecture

```
MedicationsController ([Authorize])
        ↓
MedicationService (ownership + validation + audit)
        ↓
IMedicationRepository (always filters by UserId)
        ↓
PostgreSQL Medications

Search path:
MedicationService → IFdaMedicationService → OpenFDA drug/label.json
```

## API endpoints

All require `Authorization: Bearer {accessToken}`.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/medications` | Active (non-archived) medications for the current user |
| GET | `/api/v1/medications/{id}` | Single medication; **404** if missing or not owned |
| POST | `/api/v1/medications` | Create |
| PATCH | `/api/v1/medications/{id}` | Update dosage / details (cannot change `UserId`) |
| POST | `/api/v1/medications/{id}/archive` | Soft archive (`IsArchived = true`) |
| GET | `/api/v1/medications/search?q={name}` | OpenFDA lookup (rate limited) |

### Create example

```json
{
  "name": "Metformin",
  "genericName": "Metformin Hydrochloride",
  "brandName": "Glucophage",
  "fdaIdentifier": "00093-7212",
  "dosageAmount": 500,
  "dosageUnit": "mg",
  "form": "Tablet",
  "instructions": "Take with food"
}
```

### Response example

```json
{
  "success": true,
  "data": {
    "id": "…",
    "name": "Metformin",
    "genericName": "Metformin Hydrochloride",
    "brandName": "Glucophage",
    "dosageAmount": 500,
    "dosageUnit": "mg",
    "form": "Tablet",
    "instructions": "Take with food",
    "isArchived": false
  }
}
```

## Security decisions

| Rule | Implementation |
|------|----------------|
| Auth required | `AuthorizedApiControllerBase` + JWT |
| Ownership | Every read/update/archive uses `UserId` **and** medication id |
| Cross-user access | Same **404** as missing — no ownership leak |
| Search abuse | `medication-search` rate limit: **30/min per user** |
| Audit | `MEDICATION_CREATED`, `MEDICATION_UPDATED`, `MEDICATION_ARCHIVED`, `MEDICATION_SEARCHED` |

## FDA integration

- External lookup only via OpenFDA Drug Label API (`https://api.fda.gov/drug/label.json`).
- Results are suggestions (`name`, `brand`, `identifier`); the **user** controls what is saved.
- `IHttpClientFactory` client `OpenFda` with a 10s timeout.
- Downtime or HTTP errors return a controlled message: *Medication search is temporarily unavailable* — CapTap does not crash.

## Validation

- Name required, max 255 characters
- Dosage amount **> 0**
- Dosage unit required

## Database

No new tables in Phase 4 — uses existing `Medications` entity/columns from Phase 2 (`UserId`, dosage fields, `IsArchived`, `FdaIdentifier`, etc.).
