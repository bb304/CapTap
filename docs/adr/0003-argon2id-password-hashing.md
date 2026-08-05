# ADR-0003: Argon2id password hashing (not ASP.NET Identity)

- Status: Accepted
- Date: 2026-08-05

## Context

CapTap needs strong password hashing and a custom user/session model (refresh families, soft-delete anonymization, audit events). ASP.NET Identity brings a full user-store and cookie-oriented defaults that do not match the mobile JWT design.

## Decision

Hash passwords with **Argon2id** via `Konscious.Security.Cryptography.Argon2` in `PasswordService`. Do not adopt ASP.NET Identity. Persist a custom format (`argon2id$salt$hash`) and verify in fixed fashion through the application password service.

## Consequences

- **Pros:** Memory-hard hashing aligned with modern guidance; full control over user lifecycle and tokens; no Identity schema coupling.
- **Cons:** We maintain lockout, reset, and verification flows ourselves (already implemented in `AuthService`).
- **Follow-up:** Keep parameters reviewed if hardware or threat model changes; Infrastructure tests cover hashing behavior.
