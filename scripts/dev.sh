#!/usr/bin/env bash
# CapTap local environment helpers (Phase 0)

set -euo pipefail

export PATH="${HOME}/.dotnet:${HOME}/.dotnet/.dotnet/tools:${PATH}"
export DOTNET_ROOT="${HOME}/.dotnet"
export DOCKER_HOST="unix://${HOME}/.colima/docker.sock"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

case "${1:-}" in
  db-up)
    docker compose up -d
    docker ps --filter name=captap-postgres
    ;;
  db-down)
    docker compose down
    ;;
  api)
    (cd server/CapTap.Api && dotnet run)
    ;;
  mobile)
    (cd mobile && npm start)
    ;;
  health)
    curl -sS "http://localhost:5001/health" | python3 -m json.tool
    ;;
  *)
    echo "Usage: $0 {db-up|db-down|api|mobile|health}"
    exit 1
    ;;
esac
