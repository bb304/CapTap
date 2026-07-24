# CapTap AWS Deployment (Phase 12)

Target architecture (from CapTap TAD + master prompt):

```
Mobile (EAS builds)
        │ HTTPS
        ▼
Application Load Balancer (TLS)
        │
        ▼
EC2 (Docker: CapTap.Api)
        │
        ▼
RDS PostgreSQL (encrypted, private subnet)
```

Secrets: **AWS Secrets Manager** (never commit JWT / DB / SMTP credentials).  
Local development continues to use `.env` files.

## Prerequisites

- AWS account with VPC, EC2, RDS, ALB, Secrets Manager, CloudWatch permissions
- Domain + ACM certificate for `api.your-domain.com`
- GitHub Actions release image (GHCR) or ECR mirror — see `.github/workflows/release.yml`
- Strong `JWT_SECRET` (`openssl rand -base64 48`)

## 1. Network

1. Create a VPC with public + private subnets (at least 2 AZs).
2. Place **RDS** in private subnets; security group allows 5432 only from the API SG.
3. Place **EC2** in private or public (demo) subnets; security group allows 8080 only from the ALB SG.
4. ALB in public subnets; listener 443 → target group → EC2:8080.
5. Health checks:
   - Liveness / ALB: `GET /health/live`
   - Readiness (optional deeper): `GET /health/ready` (requires DB)

## 2. Database (RDS PostgreSQL 16)

1. Create PostgreSQL 16 instance (Multi-AZ for production demos that matter).
2. Enable encryption at rest; restrict public access.
3. Store connection string in Secrets Manager, e.g. secret `captap/production/database`:

```json
{
  "DATABASE_CONNECTION": "Host=....rds.amazonaws.com;Port=5432;Database=captapdb;Username=captap;Password=..."
}
```

4. Apply EF migrations from a trusted runner (CI job, bastion, or one-off ECS/EC2 task):

```bash
export DATABASE_CONNECTION="$(aws secretsmanager get-secret-value --secret-id captap/production/database --query SecretString --output text | jq -r .DATABASE_CONNECTION)"
export PATH="$HOME/.dotnet:$PATH"
dotnet ef database update \
  --project server/CapTap.Infrastructure \
  --startup-project server/CapTap.Api
```

Do **not** auto-migrate on every container start in production (protects data integrity).

## 3. Secrets Manager

Create `captap/production/app` (example keys):

| Key | Purpose |
|-----|---------|
| `JWT_SECRET` | Access-token signing (≥32 chars, not a placeholder) |
| `JWT_ISSUER` | Optional override |
| `JWT_AUDIENCE` | Optional override |
| `EMAIL_PROVIDER` | Must be `Smtp` in Production |
| `EMAIL_HOST` / `EMAIL_PORT` / `EMAIL_USERNAME` / `EMAIL_PASSWORD` / `EMAIL_FROM` | SMTP |
| `EMAIL_APP_BASE_URL` | Password-reset deep link base |
| `CORS_ALLOWED_ORIGINS` | Comma-separated browser origins (native apps usually empty) |

CapTap Production startup **rejects** weak JWT placeholders and non-SMTP email providers.

## 4. EC2 + Docker

Minimal demo host:

```bash
# Amazon Linux 2023 example
sudo dnf install -y docker
sudo systemctl enable --now docker
sudo usermod -aG docker ec2-user

# Pull image (GHCR example after Release workflow)
docker pull ghcr.io/<org>/captap-api:v1.0.0

# Inject secrets from Secrets Manager into the container env (prefer IAM role + aws cli)
export JWT_SECRET=...
export DATABASE_CONNECTION=...
# ...remaining env vars...

docker run -d --name captap-api --restart unless-stopped \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_CONNECTION \
  -e JWT_SECRET \
  -e EMAIL_PROVIDER=Smtp \
  -e EMAIL_HOST -e EMAIL_PORT -e EMAIL_USERNAME -e EMAIL_PASSWORD \
  -e EMAIL_FROM -e EMAIL_FROM_NAME=CapTap \
  -e EMAIL_APP_BASE_URL=https://app.your-domain.com \
  -e CORS_ALLOWED_ORIGINS \
  ghcr.io/<org>/captap-api:v1.0.0
```

For a closer local rehearsal:

```bash
docker compose --profile full up -d --build
```

## 5. TLS + mobile config

1. Attach ACM cert to ALB HTTPS listener.
2. Set mobile `EXPO_PUBLIC_API_URL=https://api.your-domain.com` via **EAS secrets / build profiles** (never commit personal LAN IPs).
3. Ship production builds with EAS (`eas.json` → `production` profile).

## 6. CI → deploy sketch

```
PR / push → .github/workflows/ci.yml
  • backend format + build + tests + coverage
  • mobile lint + format + tests + coverage
  • docker image build (no push)

tag v* / workflow_dispatch → .github/workflows/release.yml
  • push captap-api to GHCR

Manual / CD (next hardening step)
  • mirror GHCR → ECR (optional)
  • SSM / CodeDeploy / user-data pulls new tag
  • drain ALB, restart container, verify /health/ready
```

## 7. What CapTap deliberately does not automate yet

- Full Terraform/CloudFormation stack (kept as docs-first for MVP simplicity)
- Auto-scaling groups / ECS Fargate (reasonable next step once demo traffic warrants it)
- Remote push (APNs) — local reminders only

See also: `docs/monitoring.md`, `docs/production-hardening.md`, `infrastructure/README.md`.
