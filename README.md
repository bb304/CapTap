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

Phase 0 complete. Phase 1 (backend foundation) complete. Authentication and domain features are not implemented yet.

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
