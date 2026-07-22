# CapTap

CapTap is a secure medication adherence application that helps users track daily medications through reminders, medication logging, and NFC technology.

## Technology Stack

Frontend:
- React Native
- Expo
- TypeScript

Backend:
- ASP.NET Core
- C#

Database:
- PostgreSQL

Infrastructure:
- Docker
- AWS

## Project Status

Currently in development.

## Getting Started

### Prerequisites

- Node.js LTS (22.13+ recommended for Expo)
- .NET 9 SDK (`~/.dotnet` or system install)
- Docker runtime (Colima + Docker CLI, or Docker Desktop)
- Git

### Local Docker runtime (Colima)

If you are not using Docker Desktop:

```bash
colima start
export DOCKER_HOST="unix://${HOME}/.colima/docker.sock"
```

### Start PostgreSQL

```bash
docker compose up -d
```

### Run the API

```bash
export PATH="$HOME/.dotnet:$HOME/.dotnet/.dotnet/tools:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet run --project server/CapTap.Api --launch-profile http
```

- Health: `http://localhost:5001/health`
- Swagger: `http://localhost:5001/swagger`

### Run the mobile app

```bash
cd mobile
npm start
```

The mobile home screen calls `/health` using `EXPO_PUBLIC_API_URL` (see `mobile/.env.example`).

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
- Root `.env.example` documents shared keys

`.env` files are gitignored.

## Repository Structure

```
CapTap/
├── mobile/           # React Native (Expo) app
├── server/           # ASP.NET Core Clean Architecture
├── infrastructure/   # Docker / AWS assets
├── docs/             # Product and engineering docs
├── scripts/          # Utility scripts
├── docker-compose.yml
└── .github/          # CI workflows
```

## Phase Status

Phase 0 (environment + project foundation) complete.
Phase 1 (backend foundation) not started.