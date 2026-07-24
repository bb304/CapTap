# CapTap Authentication

Phase 3 authentication foundation for CapTap. Protects user identity and sessions so medication data can be scoped to the authenticated owner.

## Architecture

```
Mobile App
    ↓
Login / Register API (/api/v1/auth/*)
    ↓
JWT Access Token (15 minutes) + Refresh Token (30 days)
    ↓
Protected APIs (Authorization: Bearer + CurrentUserId scoping)
    ↓
Refresh Token Rotation + Family Reuse Detection
```

### Layers

| Layer | Responsibility |
|-------|----------------|
| `AuthController` | HTTP only — validate transport, call `IAuthService` |
| `AuthorizedApiControllerBase` | `[Authorize]` + `CurrentUserId` for medication/NFC APIs |
| `AuthService` | Registration, login, refresh rotation/reuse detection, logout, password reset |
| `TokenService` | JWT access tokens + cryptographically secure refresh tokens (hashed at rest) |
| `PasswordService` | Argon2id hashing / verification |
| `SmtpEmailService` / `MockEmailService` | Real SMTP or local mock via `EMAIL_PROVIDER` |
| `ICurrentUserService` | Reads `userId` / email claims for user-scoped queries |
| Repositories | Users, refresh tokens, password-reset tokens via EF Core |

## Registration flow

1. Client posts email + password + confirm password.
2. FluentValidation enforces email format and password complexity.
3. Email is normalized to lowercase.
4. Duplicate emails return a generic **Account creation failed.** message (no existence leak).
5. Password is hashed with Argon2id.
6. User is persisted with `Status=Active`, `FailedLoginAttempts=0`.
7. Email verification token is generated, **hashed**, stored, and emailed via `IEmailService`.
8. Audit event: `REGISTER_SUCCESS`.

## Login flow

1. Client posts email + password.
2. Unknown / inactive accounts fail with **Invalid email or password.**
3. Locked accounts (`LockedUntil` in the future or suspended status) are rejected.
4. Failed passwords increment `FailedLoginAttempts`. After **5** failures the account locks for **15 minutes**.
5. Success resets failure counters, issues access + refresh tokens in a new **token family**, stores **hashed** refresh token, audits `LOGIN_SUCCESS`.

## JWT lifecycle

| Token | Lifetime | Storage | Claims |
|-------|----------|---------|--------|
| Access | 15 minutes (`expiresIn`: 900 seconds) | Client only | `userId`, `email`, `tokenId` / `jti` |
| Refresh | 30 days | DB as SHA-256 hash only | N/A (opaque random); grouped by `FamilyId` |

### Secrets

- **Never** store production `JWT_SECRET` in source control.
- `appsettings*.json` keep `JwtSettings:Secret` empty; load via `JWT_SECRET` env / `.env` (gitignored).
- Minimum length: 32 characters.
- Production rejects known weak placeholders (`dev-only-...`, `your_secret`, etc.).
- Generate: `openssl rand -base64 48`

Other settings:

- `Issuer`: `CapTap`
- `Audience`: `CapTapMobile`
- `AccessTokenExpirationMinutes`: `15`
- `RefreshTokenExpirationDays`: `30`

## Refresh token rotation & reuse detection

1. Client sends current refresh token to `POST /api/v1/auth/refresh`.
2. Server looks up by hash.
3. If the token was **already revoked** (replay / theft), CapTap revokes the **entire family** (`REFRESH_REUSE_DETECTED`) and rejects.
4. Otherwise the presented token is revoked (`rotated`), and a new pair is issued with the **same `FamilyId`**.
5. Logout revokes the whole family (`logout`).

## Protected medication / NFC APIs

Controllers inherit `AuthorizedApiControllerBase`. Medication CRUD is Phase 4 (`docs/medication-management.md`). NFC assign/resolve/logging is Phase 9 (`docs/nfc-integration.md`):

- `GET /api/v1/nfc/tags` — assigned tags for `CurrentUserId`
- `POST /api/v1/nfc/assign` / `unassign`
- `GET /api/v1/nfc/{tagIdentifier}` — resolve for confirm-to-log

**Rule:** every medication/NFC query and mutation must filter by `CurrentUserId` from claims. Never trust a client-supplied user id.

## Password reset

1. `forgot-password` always returns a generic success message.
2. If the account exists, a hashed reset token is stored and emailed.
3. `reset-password` validates the token, hashes the new password, marks the token used, audits `PASSWORD_RESET_SUCCESS`.
4. Never reveals whether an email is registered.

## Email

| Provider | When |
|----------|------|
| `Mock` | Local Development (default) — logs send intent; tokens only in Dev logs |
| `Smtp` | Staging/Production — SMTP via `EMAIL_*` env vars |

Production **requires** `EMAIL_PROVIDER=Smtp` with `EMAIL_HOST` and `EMAIL_FROM` configured.

## Security decisions

| Control | Implementation |
|---------|----------------|
| Password hashing | Argon2id (Konscious) |
| Refresh at rest | SHA-256 hash only |
| Refresh reuse | Family revocation |
| Account lockout | 5 failures → 15 minute lock |
| Rate limiting | Fixed window: 10 req/min/IP on login & register |
| HTTPS | Forced in Production |
| Security headers | `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy` |
| User scoping | `[Authorize]` + `ICurrentUserService` |
| Audit trail | Includes `REFRESH_REUSE_DETECTED` |
| Error responses | Generic client messages; no stack traces or auth internals |

## API endpoints

| Method | Path | Notes |
|--------|------|-------|
| POST | `/api/v1/auth/register` | Rate limited |
| POST | `/api/v1/auth/login` | Rate limited; returns tokens |
| POST | `/api/v1/auth/refresh` | Rotation + reuse detection |
| POST | `/api/v1/auth/logout` | Revokes token family |
| POST | `/api/v1/auth/forgot-password` | Enumeration-safe |
| POST | `/api/v1/auth/reset-password` | Token + new password |
| GET | `/api/v1/medications` | Authorized (Phase 4+) |
| GET | `/api/v1/nfc/tags` | Authorized (Phase 9) |
| PUT | `/api/v1/users/me/timezone` | Authorized (Phase 9) |

### Auth response shape

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 900
}
```

Wrapped in CapTap `ApiResponse` (`success` / `data` / `error`).

## Swagger

Bearer JWT security scheme is configured. Authorize with `Bearer {accessToken}` for protected routes.
