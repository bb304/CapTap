# CapTap sequence diagrams

Behavioral views of critical paths. Narrative detail lives in linked feature docs.

## 1. Login and refresh-token reuse detection

See also: [authentication.md](../authentication.md), [ADR-0002](../adr/0002-jwt-refresh-token-families.md).

```mermaid
sequenceDiagram
  actor User
  participant App as Mobile app
  participant API as CapTap.Api
  participant DB as PostgreSQL

  User->>App: Email + password
  App->>API: POST /api/v1/auth/login
  API->>DB: Verify Argon2id hash; reset lockout counters
  API->>DB: Store hashed refresh token (new FamilyId)
  API-->>App: accessToken + refreshToken (expiresIn 900)

  Note over App,API: Later — access JWT expired
  App->>API: POST /api/v1/auth/refresh (current refresh)
  API->>DB: Lookup by hash; revoke as rotated; issue new pair same FamilyId
  API-->>App: New access + refresh

  Note over App,API: Attacker replays old rotated refresh
  App->>API: POST /api/v1/auth/refresh (revoked token)
  API->>DB: Revoke entire family (reuse_detected)
  API->>DB: Audit REFRESH_REUSE_DETECTED
  API-->>App: 401 Unauthorized
```

## 2. Offline dose log replay (409 = success)

See also: [offline-architecture.md](../offline-architecture.md), [ADR-0006](../adr/0006-offline-dose-log-idempotency.md).

```mermaid
sequenceDiagram
  actor User
  participant App as Mobile app
  participant SQLite as Device SQLite
  participant API as CapTap.Api
  participant DB as PostgreSQL

  User->>App: Mark as Taken (offline)
  App->>SQLite: Enqueue CreateMedicationLogRequest
  App-->>User: Optimistic UI + pending banner

  Note over App,API: Network returns
  App->>SQLite: Dequeue FIFO
  App->>API: POST /api/v1/medication-logs
  alt First success
    API->>DB: Insert log (unique UserId+ScheduleId+ScheduledDoseTime)
    API-->>App: 200 OK
    App->>SQLite: Remove queue row
  else Already logged / retry
    API->>DB: Unique violation
    API-->>App: 409 Conflict
    App->>SQLite: Treat 409 as success; remove queue row
  end
```

## 3. Account soft-delete and anonymize

See also: [account-privacy.md](../account-privacy.md).

```mermaid
sequenceDiagram
  actor User
  participant App as Mobile app
  participant API as CapTap.Api
  participant DB as PostgreSQL

  User->>App: Delete account + password confirm
  App->>API: DELETE /api/v1/users/me
  API->>DB: Verify password
  API->>DB: Anonymize email/password; Status=Deleted; DeletedAt
  API->>DB: Revoke all refresh tokens (account_deleted)
  API->>DB: Soft-unassign NFC tags; archive medications
  Note over DB: Medication logs retained under anonymized user id
  API->>DB: Audit ACCOUNT_DELETED
  API-->>App: Success
  App->>App: Clear SecureStore, SQLite, React Query cache
```

## Related

- [System context](system-context.md)
- [Threat model](../security/threat-model.md)
