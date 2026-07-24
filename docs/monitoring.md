# CapTap Monitoring (Phase 12)

Goal: operators can detect outages and auth failures without logging medication contents, passwords, or tokens (master prompt privacy rules).

## What to monitor

| Signal | Source | Alert idea |
|--------|--------|------------|
| API process up | ALB / Docker → `GET /health/live` | Target unhealthy > 2 min |
| DB connectivity | `GET /health/ready` | Ready failing while live OK → DB/SG issue |
| 5xx rate | ALB access logs / app logs | Spike vs baseline |
| 429 rate | Auth / search rate limits | Possible abuse |
| Auth failures | Audit / structured warnings | Lockouts / credential stuffing |
| RDS CPU / storage / connections | CloudWatch RDS | Capacity |
| EC2 CPU / disk | CloudWatch EC2 | Host pressure |
| Container restarts | Docker / systemd | Crash loop |

## Health endpoints

| Path | Meaning |
|------|---------|
| `/health/live` | Process is serving (no DB dependency) |
| `/health/ready` | Database check included |
| `/health` | Aggregate (compat); prefer ready for deep checks |

Point ALB health checks at **`/health/live`** so a brief DB blip does not replace every instance. Use `/health/ready` for deploy gates and synthetic checks.

## Logging rules

Log:

- Authentication / security events (via existing audit service)
- Unhandled server errors (exception + error code; never stack traces to clients)
- Request method, path, status, duration (`RequestTimingMiddleware`)

Do **not** log:

- Passwords, JWTs, refresh tokens, Authorization headers
- Request/response bodies that include medication names or doses
- NFC tag ↔ medication mappings in bulk dumps

Client errors are logged as `Handled application error {ErrorCode}` without exception message text (messages can contain medication context).

## CloudWatch (recommended baseline)

1. ALB → enable access logs to S3; metric alarms on `HTTPCode_Target_5XX_Count`, `UnHealthyHostCount`.
2. EC2 → CloudWatch agent: collect docker/app stdout; filter on `Unhandled server error`.
3. RDS → default Enhanced Monitoring optional; alarm free storage + CPU.
4. Optional: metric filter on `status":"unhealthy"` from readiness probes.

## Synthetic checks

External uptime check every 1–5 minutes:

```bash
curl -fsS https://api.your-domain.com/health/live
curl -fsS https://api.your-domain.com/health/ready
```

## Mobile / EAS

- Crash analytics (Sentry, etc.) is optional and out of MVP scope.
- Prefer EAS build + TestFlight/Play internal tracks for release monitoring.
- Offline queue depth is user-local (SQLite); no server metric — surface via in-app pending banner.

## Incident first response

1. Check `/health/live` vs `/health/ready`.
2. If only ready fails → RDS security group, secret rotation, connection limits.
3. If live fails → EC2/docker/image; roll back previous GHCR tag.
4. If 401/429 storms → rate limits working; review auth audit events; rotate JWT only with a planned client re-login.
