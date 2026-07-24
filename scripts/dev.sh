#!/usr/bin/env bash
# CapTap local environment helpers

set -euo pipefail

export PATH="${HOME}/.dotnet:${HOME}/.dotnet/.dotnet/tools:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
export DOCKER_HOST="${DOCKER_HOST:-unix://${HOME}/.colima/docker.sock}"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

case "${1:-}" in
  db-up)
    docker compose up -d postgres
    docker ps --filter name=captap-postgres
    ;;
  db-down)
    docker compose down
    ;;
  full-up)
    docker compose --profile full up -d --build
    ;;
  api)
    (cd server/CapTap.Api && dotnet run --launch-profile http)
    ;;
  mobile)
    (cd mobile && npm start)
    ;;
  health)
    curl -sS "http://localhost:5001/health" | python3 -m json.tool
    echo
    curl -sS "http://localhost:5001/health/live" | python3 -m json.tool
    echo
    curl -sS "http://localhost:5001/health/ready" | python3 -m json.tool
    ;;
  test-backend)
    dotnet test server/CapTap.sln --configuration Release
    ;;
  test-mobile)
    (cd mobile && npm test)
    ;;
  migrate)
    dotnet ef database update \
      --project server/CapTap.Infrastructure \
      --startup-project server/CapTap.Api
    ;;
  *)
    echo "Usage: $0 {db-up|db-down|full-up|api|mobile|health|migrate|test-backend|test-mobile}"
    exit 1
    ;;
esac
