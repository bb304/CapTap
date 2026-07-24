# CapTap

**Tap. Confirm. Peace of mind.**

CapTap is a secure medication adherence application that helps people track daily medications through reminders, manual and NFC logging, and offline-first sync.

## Technology choices

| Layer | Choice | Why |
|-------|--------|-----|
| Mobile | React Native + Expo | Cross-platform, EAS device builds for NFC / notifications |
| Backend | ASP.NET Core (.NET 9) | Strong typing, middleware, auth ecosystem |
| Architecture | Clean Architecture | Controllers never touch the database; DTOs at the edge |
| ORM / DB | EF Core + PostgreSQL | Relational integrity for schedules, logs, NFC ownership |
| Local device | Expo SQLite + TanStack Query | Offline queue + cache — API remains source of truth |
| Infra | Docker, GitHub Actions, AWS (EC2 + RDS) | Demo-ready deploy path without overbuilt IaC |

## Project status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 0 — Environment & project setup | Complete | Repo layout, Expo app, ASP.NET solution, Docker Postgres |
| Phase 1 — Backend foundation | Complete | Clean Architecture, EF, DI, middleware, health, tests |
| Phase 2 — Domain & database | Complete | Core entities, Fluent API, `InitialCreate` |
| Phase 3 — Authentication | Complete | Register/login, JWT, refresh rotation, SMTP |
| Phase 4 — Medication management | Complete | Personal med CRUD, OpenFDA search, ownership isolation |
| Phase 5 — Scheduling & adherence | Complete | Multi-time schedules, today/missed dashboard |
| Phase 6 — Mobile foundation | Complete | Expo Router, theme, UI kit |
| Phase 7 — Mobile ↔ backend integration | Complete | Axios, SecureStore, live screens |
| Phase 8 — Medication logging & adherence | Complete | Manual Mark as Taken, streaks, history |
| Phase 9 — NFC convenience logging | Complete | Assign/resolve, confirm via shared log endpoint |
| Phase 10 — Device validation & reminders | Complete | Local reminders, EAS profiles, device checklists |
| Phase 11 — Offline-first sync | Complete | SQLite queue, auto-sync, cached NFC resolve |
| Phase 12 — Production readiness | Complete | CI/CD, Docker image, AWS/monitoring docs, hardening |

### Completed in Phase 12

- GitHub Actions CI: backend format/build/test/coverage + mobile lint/format/test/coverage + Docker image build
- Release workflow publishes `captap-api` to GHCR on version tags
- Multi-stage API Dockerfile (non-root) + Compose `full` profile
- Health split: `/health/live`, `/health/ready`, `/health`
- Production CORS lockdown, request timing logs (no PHI bodies), safer client-error logging
- Docs: AWS deploy, monitoring, production hardening; device EAS + airplane-mode E2E checklists

### Completed in Phase 11

- SQLite sync queue for Manual/NFC logs; optimistic UI + pending banner
- Auto replay on reconnect (FIFO, backoff, 409 = idempotent success)
- TanStack Query persistence + NFC tag cache for offline resolve
- Docs: `docs/offline-architecture.md`

## Architecture

```
React Native (Expo)
        │ HTTPS / LAN
        ▼
ASP.NET Core API (CapTap.Api)
        │
        ├── Application (use cases, DTOs, validation)
        ├── Domain (entities, rules)
        └── Infrastructure (EF Core, SMTP, OpenFDA)
                │
                ▼
         PostgreSQL
```

Offline (device):

```
UI → SQLite offline_queue + query_cache → syncEngine → POST /api/v1/medication-logs
```

```
API (CapTap.Api)
        ↓
Application (CapTap.Application)
        ↓
Domain (CapTap.Domain)

Infrastructure (CapTap.Infrastructure)
        ↓
PostgreSQL
```

## Getting started

### Prerequisites

- Node.js LTS (22.13+ recommended for Expo)
- .NET 9 SDK
- Docker runtime (Colima + Docker CLI, or Docker Desktop)
- Git
- Expo account (for development builds / NFC)

### Local Docker runtime (Colima)

```bash
colima start
export DOCKER_HOST="unix://${HOME}/.colima/docker.sock"
```

### Running backend

```bash
docker compose up -d postgres

dotnet ef database update \
  --project server/CapTap.Infrastructure \
  --startup-project server/CapTap.Api

export PATH="$HOME/.dotnet:$HOME/.dotnet/.dotnet/tools:$PATH"
dotnet run --project server/CapTap.Api --launch-profile http
```

- Liveness: `http://localhost:5001/health/live`
- Readiness: `http://localhost:5001/health/ready`
- Swagger (Development): `http://localhost:5001/swagger`

Optional API container:

```bash
docker compose --profile full up -d --build
```

### Run tests

```bash
# Backend (Testcontainers needs Docker — GitHub Actions runners work out of the box.
# On Colima, if Infrastructure tests fail mounting docker.sock, Application tests
# still validate business logic; CI remains the integration gate.)
export DOCKER_HOST="unix://${HOME}/.colima/docker.sock"
dotnet test server/CapTap.sln

# Mobile
cd mobile && npm test
```

### Run the mobile app

```bash
cd mobile
npm start
```

NFC + reliable local notifications require an **EAS development build** (not Expo Go alone):

```bash
cd mobile
npx eas login
npm run eas:init          # paste projectId into app.json
npm run build:dev:ios     # or build:dev:android
```

### Helper script

```bash
./scripts/dev.sh db-up
./scripts/dev.sh migrate
./scripts/dev.sh api
./scripts/dev.sh mobile
./scripts/dev.sh health
./scripts/dev.sh full-up
```

## CI / CD

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `.github/workflows/ci.yml` | PR / push to `main`/`develop` | Format, build, test, coverage, Docker build |
| `.github/workflows/release.yml` | `v*` tags or manual | Push API image to GHCR |

## Security decisions

- Assume hostile input; validate DTOs; ownership-scoped queries
- JWT + hashed refresh tokens with family reuse detection
- Production rejects weak JWT secrets and Mock email
- Secrets via `.env` locally and **AWS Secrets Manager** in production
- NFC tags never store medication names or user PHI
- API errors never expose stack traces or SQL
- Production CORS defaults to deny browser origins unless `CORS_ALLOWED_ORIGINS` is set

## Environment variables

Copy examples before running:

- `server/CapTap.Api/.env.example` → `server/CapTap.Api/.env`
- `mobile/.env.example` → local env / EAS secrets
- Root `.env.example` for Compose-oriented vars

Never commit real secrets, LAN IPs, or production JWT values.

## API documentation

- Interactive: Swagger UI in Development (`/swagger`)
- Narrative: `docs/authentication.md`, `docs/medication-management.md`, `docs/scheduling.md`, `docs/medication-logging.md`, `docs/nfc-integration.md`
- Product/API specs (docx): under `docs/`

## Screenshots

Capture on a development build for demos (Welcome, Dashboard, NFC confirm, Offline pending banner, Settings → Reminders). Store under `docs/screenshots/` when available (not required for CI).

## Documentation index

| Doc | Topic |
|-----|--------|
| `docs/authentication.md` | Auth, JWT, lockout |
| `docs/medication-management.md` | Medication CRUD + OpenFDA |
| `docs/scheduling.md` | Schedules + adherence statuses |
| `docs/medication-logging.md` | Manual logging, streaks, history |
| `docs/nfc-integration.md` | NFC assign/scan/confirm + time zones |
| `docs/production-validation.md` | EAS, device NFC, reminders, offline E2E |
| `docs/offline-architecture.md` | SQLite queue, sync, conflicts |
| `docs/aws-deployment.md` | EC2, RDS, ALB, Secrets Manager |
| `docs/monitoring.md` | Health, CloudWatch, log rules |
| `docs/production-hardening.md` | Security / perf / a11y / maintainability review |
| `docs/mobile-architecture.md` | Expo app structure |
| `docs/mobile-backend-integration.md` | Axios, session, hooks |
| `docs/database-design.md` | Schema overview |
| `infrastructure/README.md` | Docker / AWS bootstrap notes |

## Repository structure

```
CapTap/
├── mobile/              # React Native (Expo) app
├── server/              # ASP.NET Core Clean Architecture
├── tests/               # xUnit test projects
├── infrastructure/      # AWS bootstrap examples
├── docs/                # Product and engineering docs
├── scripts/             # Utility scripts
├── docker-compose.yml   # Postgres (+ optional API profile)
└── .github/workflows/   # CI + release
```

## Future roadmap

- Quiet-hours enforcement for reminders
- Soft-delete / anonymization workflows
- Remote push (APNs) if local reminders are insufficient
- Full Terraform/ECS when traffic outgrows single-EC2 demos
- Store accessibility audit (VoiceOver / TalkBack)

## Intentionally deferred

- Soft-delete / anonymization workflows
- Medication log edit/delete APIs (audit event names reserved)
- Quiet-hours enforcement (preference stored; behavior later)
- Remote push / APNs (local reminders only)
- Full IaC (docs-first AWS path shipped in Phase 12)
