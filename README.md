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
| Phase 1 — Backend foundation | Complete | Clean Architecture, EF Core, DI, repositories, middleware, config, tests |
| Phase 2 — Authentication | Not started | Register/login, JWT, refresh tokens, email verification |
| Later phases | Not started | Medications, NFC, dashboard, offline, deployment |

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

### Not in Phase 1 (intentionally deferred)

- Authentication / JWT issuance
- Domain entities (User, Medication, etc.)
- Medication, NFC, dashboard, or other business APIs
- Rate limiting and FluentValidation (planned before/with auth)

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
