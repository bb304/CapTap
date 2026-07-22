# CapTap

CapTap is a secure medication adherence application that helps users track daily medications through reminders, medication logging, and NFC technology.

## Technology Stack

Frontend:
- React Native
- Expo
- TypeScript

Backend:
- ASP.NET Core (.NET 9)
- C#
- Entity Framework Core

Database:
- PostgreSQL

Infrastructure:
- Docker
- AWS

## Project Status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 0 — Environment & project setup | Complete | Repo layout, Expo app, ASP.NET solution, Docker Postgres, health/Swagger scaffolding |
| Phase 1 — Backend foundation | Complete | Clean Architecture, EF Core plumbing, DI, repositories, middleware, config, tests |
| Phase 2 — Domain & database | Complete | Core entities, Fluent API configs, `InitialCreate` migration, relationship tests |
| Phase 3 — Authentication | Complete | Register/login, JWT, refresh family rotation, SMTP email, authorized API scaffolds |
| Phase 4 — Medication management | Complete | Personal med CRUD, OpenFDA search, ownership isolation, archive |
| Phase 5 — Scheduling & adherence | Complete | Multi-time schedules, today/missed dashboard, status engine |
| Later phases | Not started | NFC logging, offline, deployment |

### Completed in Phase 5

- `MedicationSchedule` extended: `IsActive`, `EffectiveFrom`, `EffectiveTo`; frequency enum as strings
- Multiple daily times as separate schedule rows; duplicate time rejection
- `AdherenceService` computes Upcoming / Due / Taken / Missed (no auto missed logs)
- Endpoints: medication schedules CRUD + `GET /api/v1/dashboard/today` + `/missed`
- Audit: `SCHEDULE_CREATED` / `UPDATED` / `DELETED`
- Docs: `docs/scheduling.md`; scheduling & adherence unit tests

### Completed in Phase 4

- Medication endpoints under `/api/v1/medications` (list, get, create, update, archive, OpenFDA search)
- Ownership enforced on every query (`UserId` + id); cross-user access returns identical 404
- OpenFDA integration via `IFdaMedicationService` / `HttpClient` (graceful failure, 10s timeout)
- Search rate limit: 30 requests/minute per authenticated user
- Audit: `MEDICATION_CREATED`, `MEDICATION_UPDATED`, `MEDICATION_ARCHIVED`, `MEDICATION_SEARCHED`
- Docs: `docs/medication-management.md`; application medication unit tests

### Completed in Phase 3

- Auth endpoints under `/api/v1/auth/*` (register, login, refresh, logout, forgot/reset password)
- Argon2id password hashing; JWT access tokens (15m) + hashed refresh tokens with rotation (30d)
- Refresh **family reuse detection** (replay revokes the whole session family)
- Account lockout (5 failures / 15 minutes), IP rate limiting on login/register (10/min)
- `SmtpEmailService` (SMTP) + `Mock` for local; Production requires SMTP
- `JWT_SECRET` only via env (not committed); Production rejects weak placeholders
- `[Authorize]` + `CurrentUserId` for protected APIs
- Audit events, security headers, Production HTTPS redirection, Swagger Bearer auth
- Migrations: `AuthFoundation`, `RefreshTokenFamilies`; docs: `docs/authentication.md`
- Application auth unit tests + Argon2 password hashing test

### Completed in Phase 2

- Domain entities: `User`, `Medication`, `MedicationSchedule`, `MedicationLog`, `NfcTag`, `RefreshToken`, `AuditLog`
- Enums stored as strings: `FrequencyType`, `LoggingMethod`
- EF Fluent API configurations, indexes, and delete behaviors
- Migration: `InitialCreate` applied to local PostgreSQL
- Infrastructure tests for context init, user insert, medication/schedule relationships, NFC uniqueness
- Database design doc: `docs/database-design.md`

### Completed in Phase 1

- Clean Architecture layers (`Api` → `Application` → `Domain`; `Infrastructure` → Application/Domain)
- Entity Framework Core + PostgreSQL (`ApplicationDbContext`, migrations pipeline)
- `BaseEntity` with UUID + audit timestamps
- Generic repository + unit of work abstractions
- Dependency injection extensions (`AddApplicationServices`, `AddInfrastructureServices`, `AddDatabase`, Swagger)
- Global exception middleware with safe `ApiResponse` errors
- Health endpoint with database check (`GET /health`)
- Swagger/OpenAPI (JWT security scheme prepared, not enforced)
- Configuration: `DatabaseSettings`, `JwtSettings`, `ApplicationSettings` + env overrides
- xUnit + FluentAssertions tests (Application + Infrastructure with Testcontainers)

### Intentionally deferred

- NFC / manual dose logging APIs (Phase 6) — adherence matches logs by `ScheduleId` when present
- User timezones (status currently uses UTC)
- Soft-delete / anonymization workflows

## Backend Architecture

```
API (CapTap.Api)
        ↓
Application (CapTap.Application)
        ↓
Domain (CapTap.Domain)

Infrastructure (CapTap.Infrastructure)
        ↓
Application + Domain
        ↓
PostgreSQL
```

- **Api** — HTTP, middleware, Swagger, composition root
- **Application** — interfaces, DTOs, use cases (future phases)
- **Domain** — entities and domain rules (future phases)
- **Infrastructure** — EF Core, repositories, external services
- **Shared** — API response wrappers and shared constants

## Getting Started

### Prerequisites

- Node.js LTS (22.13+ recommended for Expo)
- .NET 9 SDK (`~/.dotnet` or system install)
- Docker runtime (Colima + Docker CLI, or Docker Desktop)
- Git

### Local Docker runtime (Colima)

```bash
colima start
export DOCKER_HOST="unix://${HOME}/.colima/docker.sock"
```

### Running Backend

```bash
# Start PostgreSQL
docker compose up -d

# Optional: apply migrations once domain entities exist
dotnet ef database update \
  --project server/CapTap.Infrastructure \
  --startup-project server/CapTap.Api

# Run API
export PATH="$HOME/.dotnet:$HOME/.dotnet/.dotnet/tools:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet run --project server/CapTap.Api --launch-profile http
```

- Health: `http://localhost:5001/health`
- Swagger: `http://localhost:5001/swagger`

### Database Migration Instructions

From `server/`:

```bash
dotnet ef migrations add MigrationName \
  --project CapTap.Infrastructure \
  --startup-project CapTap.Api \
  --output-dir Migrations

dotnet ef database update \
  --project CapTap.Infrastructure \
  --startup-project CapTap.Api
```

### Run tests

```bash
export DOCKER_HOST="unix://${HOME}/.colima/docker.sock"  # required for Testcontainers with Colima
dotnet test server/CapTap.sln
```

### Run the mobile app

```bash
cd mobile
npm start
```

### Helper script

```bash
./scripts/dev.sh db-up
./scripts/dev.sh api
./scripts/dev.sh mobile
./scripts/dev.sh health
```

## Environment Variables

Copy examples before running:

- `server/CapTap.Api/.env.example` → `server/CapTap.Api/.env`
- `mobile/.env.example` → `mobile/.env`

Configuration sections (see `appsettings*.json`):

- `DatabaseSettings`
- `JwtSettings` (prepared for Phase 2; unused in Phase 1)
- `ApplicationSettings`

`.env` files are gitignored. Never commit secrets.

## Repository Structure

```
CapTap/
├── mobile/           # React Native (Expo) app
├── server/           # ASP.NET Core Clean Architecture
├── tests/            # xUnit test projects
├── infrastructure/   # Docker / AWS assets
├── docs/             # Product and engineering docs
├── scripts/          # Utility scripts
├── docker-compose.yml
└── .github/          # CI workflows
```
