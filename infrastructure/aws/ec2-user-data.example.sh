#!/usr/bin/env bash
# Example EC2 user-data / bootstrap for CapTap API (Amazon Linux 2023).
# Replace placeholders. Prefer IAM instance role + Secrets Manager over hardcoding.

set -euo pipefail

dnf install -y docker
systemctl enable --now docker

IMAGE="${CAPTAP_IMAGE:-ghcr.io/YOUR_ORG/captap-api:latest}"
docker pull "$IMAGE" || true

# Load secrets via AWS CLI (instance role required), e.g.:
# eval "$(aws secretsmanager get-secret-value --secret-id captap/production/app --query SecretString --output text | jq -r 'to_entries|map("export \(.key)=\(.value|@sh)")|.[]')"

docker rm -f captap-api 2>/dev/null || true
docker run -d --name captap-api --restart unless-stopped \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_CONNECTION \
  -e JWT_SECRET \
  -e EMAIL_PROVIDER=Smtp \
  -e EMAIL_HOST \
  -e EMAIL_PORT \
  -e EMAIL_USERNAME \
  -e EMAIL_PASSWORD \
  -e EMAIL_FROM \
  -e EMAIL_FROM_NAME=CapTap \
  -e EMAIL_APP_BASE_URL \
  -e CORS_ALLOWED_ORIGINS \
  "$IMAGE"
