# CapTap Account Privacy (Phase 14)

DDD data-retention flow for account deletion:

```
Account Deleted
      ↓
Soft delete (IsActive=false, Status=Deleted, DeletedAt)
      ↓
Anonymize personal data (email, password hash, timezone reset)
      ↓
Revoke sessions + unassign NFC + archive medications
      ↓
Retain medication logs under anonymized user id
```

## API

`DELETE /api/v1/users/me` (JWT + **current password** in body) → `204 No Content`

Request body:

```json
{ "password": "your-current-password" }
```

Effects:

| Area | Behavior |
|------|----------|
| Email | Replaced with `deleted-{userId}@anon.invalid` (original email free to re-register) |
| Password | Replaced with a fresh Argon2 hash of random material |
| Status | `Deleted`, `IsActive=false`, `DeletedAt=UTC` |
| Refresh tokens | All revoked (`account_deleted`) |
| NFC tags | Soft unassign (`IsAssigned=false`) |
| Medications | Archived |
| Medication logs | **Retained** (no hard delete) |
| Audit | `ACCOUNT_DELETED` |

Login / refresh after delete fail with the same generic unauthorized messages (no enumeration).

## Mobile

Settings → Privacy → enter current password → **Delete my account** (destructive confirm). On success: cancel local reminders, clear SQLite offline cache, clear React Query, sign out.

Sign-out and sign-in also clear local SQLite / React Query caches so accounts never share medication data on the same device.

## Accessibility (Phase 14)

Aligned with the master prompt / PRD:

- Large touch targets (`touchTarget`) on Settings rows
- `maxFontSizeMultiplier` raised on Privacy / About / Settings copy
- Clear language; Privacy and About are real screens (not placeholders)
- Delete uses system destructive alert + accessibility hints

## Related

- Domain: `UserStatus.Deleted`, `User.DeletedAt`
- Service: `UserProfileService.DeleteAccountAsync`
- Docs: `docs/authentication.md`, `docs/final-demo.md`
