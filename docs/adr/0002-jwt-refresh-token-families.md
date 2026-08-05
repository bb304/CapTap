# ADR-0002: JWT access tokens with refresh token families

- Status: Accepted
- Date: 2026-08-05

## Context

The Expo mobile client needs lasting sessions without storing long-lived bearer tokens that grant API access if stolen. Refresh tokens stored in a database can be stolen from the DB or replayed after rotation.

## Decision

- Issue short-lived JWT **access** tokens (15 minutes) with claims `userId`, `email`, and `jti`; audience `CapTapMobile`.
- Issue opaque **refresh** tokens (30 days). Store only a SHA-256 hash in PostgreSQL.
- Group refresh tokens by `FamilyId`. On each successful refresh, revoke the presented token (`rotated`) and issue a new pair in the same family.
- If a **already-revoked** refresh token is presented, treat it as reuse/theft: revoke the entire family (`reuse_detected`), audit `REFRESH_REUSE_DETECTED`, and reject.
- Logout and password reset revoke the relevant family or all user refresh tokens.
- JWT bearer `OnTokenValidated` re-checks that the user is still active and not soft-deleted.

## Consequences

- **Pros:** Limits blast radius of stolen access tokens; DB leak does not yield usable refresh plaintext; detects token theft after rotation.
- **Cons:** Clients must implement single-flight refresh on 401; clock skew and family revoke UX require clear session expiry handling.
- **Follow-up:** Documented in `docs/authentication.md`; covered by `AuthServiceTests`.
