# CapTap threat model (STRIDE-lite)

Lightweight threat model for CapTap’s medication adherence MVP. Goal: operators and reviewers can see what we protect, what we accept, and what is out of scope.

**Last updated:** 2026-08-05  
**Related:** [authentication.md](../authentication.md), [account-privacy.md](../account-privacy.md), [monitoring.md](../monitoring.md), [ADRs](../adr/README.md)

## Assets

| Asset | Why it matters |
|-------|----------------|
| Account credentials | Account takeover → full med/history access |
| Access & refresh tokens | Session hijacking |
| Medication names, doses, schedules, logs | Sensitive personal health-adjacent data |
| NFC tag ↔ medication mapping | Reveals which med a physical sticker represents |
| Audit events | Security investigation trail |

## Trust boundaries

1. Mobile client and internet — **untrusted**
2. CapTap.Api after authn/authz — **trusted application**
3. PostgreSQL — **trusted data store** (must not be public)
4. OpenFDA / SMTP — **external**; no CapTap PHI sent to OpenFDA beyond search queries the user initiates

## STRIDE-oriented controls

| Category | Threat | CapTap control |
|----------|--------|----------------|
| **Spoofing** | Stolen password | Argon2id; lockout after 5 failures (15 minutes); generic login errors |
| **Spoofing** | Stolen access JWT | 15-minute lifetime; bearer validation; `OnTokenValidated` rejects inactive/deleted users |
| **Spoofing** | Stolen refresh token / replay after rotation | Hash at rest; rotation; **family revoke** on reuse (`REFRESH_REUSE_DETECTED`) |
| **Tampering** | Client sends another user’s `userId` | Ignore client user ids; scope by `CurrentUserId` from claims |
| **Tampering** | Duplicate offline dose posts | Unique `(UserId, ScheduleId, ScheduledDoseTime)`; HTTP 409 |
| **Repudiation** | Disputed security events | `AuditLog` for register/login/reuse/reset/delete (not a full legal audit product) |
| **Information disclosure** | Account enumeration | Generic register / forgot-password messages |
| **Information disclosure** | Tokens or meds in logs | Timing middleware logs method/path/status/duration only; no bodies/Authorization; client errors avoid PHI message text |
| **Information disclosure** | NFC sticker leak | Tag stores opaque identifier only — never medication name on tag |
| **Information disclosure** | DB backup of refresh tokens | SHA-256 hashes only |
| **Denial of service** | Auth / search flooding | Rate limits: auth 10/min/IP; medication search 30/min/user |
| **Elevation of privilege** | Call medication/NFC APIs unauthenticated | `[Authorize]` + `AuthorizedApiControllerBase` |
| **Elevation of privilege** | Use JWT after account delete | Soft-delete + anonymize; token validation rejects deleted users; refresh revoked |

## Data minimization

**Stored:** email, password hash, user-configured meds/schedules/logs, opaque NFC ids, token hashes, security audit metadata.

**Not stored:** diagnoses, conditions, insurance, provider records, unnecessary demographics.

## Out of scope (MVP)

- Formal penetration test / SOC2
- Hardware security modules for JWT signing
- End-to-end encryption of medication payloads at rest beyond DB/disk encryption in hosting
- Guaranteed anonymization against a determined adversary with full DB + NFC physical access
- Push-notification provider security (local reminders are primary)

## Residual risks (accepted for MVP)

| Risk | Mitigation / acceptance |
|------|-------------------------|
| Device theft with unlocked phone | OS lock + SecureStore; user responsibility |
| Short window of stolen access JWT | 15m expiry; refresh family revoke on detected reuse |
| OpenFDA availability | Search degrades gracefully; user can enter med details manually |
| Operator mis-logging in future OTel | Document scrubbing rules in monitoring / future observability ADR |

## Review checklist

When changing auth, logging, or NFC:

1. Does any new log/trace field include medication names, doses, or tokens?
2. Are new mutations scoped by `CurrentUserId`?
3. Do new tokens follow hash-at-rest + rotation rules?
4. Add or update an ADR if the control model changes.
