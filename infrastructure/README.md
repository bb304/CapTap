# CapTap infrastructure

Phase 12 keeps infrastructure **docs-first** (master prompt: simplicity over premature IaC).

| Path | Purpose |
|------|---------|
| `../docker-compose.yml` | Local Postgres (+ optional `full` profile API container) |
| `../server/CapTap.Api/Dockerfile` | Production-style API image |
| `../.github/workflows/ci.yml` | PR/main CI |
| `../.github/workflows/release.yml` | Tag / manual GHCR publish |
| `../docs/aws-deployment.md` | EC2 + RDS + ALB + Secrets Manager |
| `../docs/monitoring.md` | Health, CloudWatch, logging rules |
| `aws/ec2-user-data.example.sh` | Example host bootstrap (edit before use) |

## Local full stack

```bash
# From repo root
cp .env.example .env   # edit JWT_SECRET for anything beyond toy use
docker compose --profile full up -d --build
curl -s http://localhost:5001/health/live
curl -s http://localhost:5001/health/ready
```

Apply migrations before expecting ready=healthy against an empty volume:

```bash
export DATABASE_CONNECTION='Host=localhost;Port=5432;Database=captapdb;Username=captap;Password=captappassword'
dotnet ef database update \
  --project server/CapTap.Infrastructure \
  --startup-project server/CapTap.Api
```

## Next IaC step (optional)

When the demo graduates past a single EC2 host, prefer:

1. Terraform modules for VPC / RDS / ALB / ASG or ECS Fargate
2. ECR instead of (or in addition to) GHCR
3. IAM role for the instance (no long-lived AWS keys on disk)

Do not store AWS credentials in this repository.
