# CapTap Production Hardening Review (Phase 12)

Checklist derived from the CapTap AI Coding Agent Master Prompt + TAD. Items marked **Done** are implemented in-repo; **Manual** requires operator / device action.

## Security

| Item | Status | Notes |
|------|--------|-------|
| No secrets in source | Done | `.env` gitignored; Production uses env / Secrets Manager |
| Strong JWT in Production | Done | Rejects short / placeholder secrets |
| SMTP required in Production | Done | Mock email blocked |
| Argon2id + refresh family rotation | Done | Phase 3 |
| Rate limits (auth, search) | Done | |
| Ownership isolation on meds/logs/NFC | Done | Identical 404 across users |
| Safe API errors (no stack / SQL) | Done | `ExceptionMiddleware` |
| Security headers | Done | nosniff, DENY frame, CSP, referrer |
| Production CORS locked down | Done | `CORS_ALLOWED_ORIGINS`; default deny for browsers |
| NFC tags store no PHI | Done | UID / URI only |
| Non-root API container | Done | Dockerfile `captap` user |
| Dependency vulnerability scan in CI | Done | Informational `dotnet list package --vulnerable` |

## Performance

| Item | Status | Notes |
|------|--------|-------|
| Request timing logs | Done | Method/path/status/duration only |
| Health split live/ready | Done | Avoid DB-coupled liveness |
| Adherence day-bounded | Done | Documented extension points |
| Offline queue FIFO + backoff | Done | Phase 11 |
| Premature micro-optimizations avoided | Done | Master prompt simplicity |

## Accessibility (mobile)

| Item | Status | Notes |
|------|--------|-------|
| 44pt touch targets (UI kit) | Done | Phase 6 baseline |
| Clear error / empty / offline states | Done | Phases 7–11 |
| System notification permission UX | Done | Settings → Reminders |
| Full VoiceOver / TalkBack audit | Manual | Run before store submission |

## Maintainability

| Item | Status | Notes |
|------|--------|-------|
| Clean Architecture layers | Done | |
| CI: build, test, lint, format, coverage, Docker | Done | `.github/workflows/ci.yml` |
| Release image workflow | Done | `.github/workflows/release.yml` |
| AWS + monitoring docs | Done | `docs/aws-deployment.md`, `docs/monitoring.md` |
| EditorConfig | Done | Repo root |
| Architecture / API docs | Done | `docs/*` |

## Device validation still required (cannot be automated here)

1. `eas login` → `npm run eas:init` → paste real `projectId` into `mobile/app.json`
2. Install a development build (not Expo Go) for NFC + reliable notifications
3. Airplane-mode offline E2E (manual + cached NFC → reconnect sync)
4. Confirm reminders cancel when logging offline

See `docs/production-validation.md` and `docs/offline-architecture.md`.
