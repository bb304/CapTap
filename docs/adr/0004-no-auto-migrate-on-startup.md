# ADR-0004: No EF Core auto-migrate on application startup

- Status: Accepted
- Date: 2026-08-05

## Context

Running `Database.Migrate()` on every API process start is convenient for demos but dangerous in shared staging/production: multiple instances can race, failed half-migrations are hard to reason about, and rollbacks are unclear.

## Decision

Apply EF Core migrations **explicitly** via:

```bash
dotnet ef database update \
  --project server/CapTap.Infrastructure \
  --startup-project server/CapTap.Api
```

(or an equivalent one-shot CI/job). The API container does **not** auto-migrate on boot. Local helpers (`./scripts/dev.sh migrate`) remain the supported path for development.

## Consequences

- **Pros:** Controlled schema changes; safer multi-instance deploys; matches production hardening docs.
- **Cons:** Operators must remember to migrate before expecting `/health/ready` on a fresh database.
- **Follow-up:** When cloud deploy exists, run migrations as a dedicated job before shifting traffic to a new revision.
