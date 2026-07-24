# CapTap User-Testing Readiness Audit

Date: 2026-07-24  
Sources: CapTap Roadmap, PRD, Master Prompt, TAD, DDD, API Spec, `docs/*.md`, and current code.

## Verdict

**Ready for user testing with caveats.**

Core demo path works in software: register → add Metformin → schedule → manual log → reminders → offline sync → delete account. Full NFC bottle-tap needs an EAS development build + NFC hardware. Live AWS deploy and store accessibility sign-off are not required for local user tests.

## Roadmap coverage (Phases 0–14)

| Phase | Status | Notes |
|-------|--------|-------|
| 0–2 Foundation / domain | Complete | |
| 3 Auth | Complete* | Verify-email + reset-password screens/API now present; login does **not** gate on `EmailVerified` (eases Mock-email demos) |
| 4–5 Meds / schedules | Complete | Permanent med DELETE omitted (archive only) — intentional |
| 6–7 Mobile + API | Complete | |
| 8 Logging | Complete | Log PATCH/DELETE deferred (README); history + Mark as Taken present |
| 9 NFC | Complete in software | Hardware E2E manual |
| 10 Reminders | Complete | Local only (push forbidden by roadmap) |
| 11 Offline | Complete | |
| 12 Production readiness | Complete for demo | AWS = docs; CI present |
| 13 Demo readiness | Complete | Quiet hours + Android-first path |
| 14 Account privacy | Complete | Soft-delete + anonymize |

\*Closed in this audit pass: `POST /auth/verify-email`, mobile verify + reset screens, deep-link-friendly `EMAIL_APP_BASE_URL=captap://`.

## PRD MVP features

| Feature | Ready? |
|---------|--------|
| Accounts (register/login/logout/reset/verify) | Yes (verify optional for login) |
| Medication search/add/edit/archive | Yes |
| Scheduling | Yes |
| NFC logging + confirm | Yes (needs device build) |
| Manual logging | Yes |
| Today dashboard | Yes |
| History + missed | Yes |
| Basic analytics (streaks/counts) | Yes (via dashboard, not `/analytics/adherence`) |
| Accessibility baseline | Mostly (device VoiceOver/TalkBack still manual) |

### Explicitly out of scope (not gaps)

AI, caregivers, Apple Watch, doctor portals, medical advice, remote push, Redis (TAD diagram aspirational).

## Intentional API Spec drifts (acceptable)

- Paths reshaped (`/medication-logs`, `/nfc/assign`, plural `/schedules`)
- No `PATCH /users/me` displayName (optional in PRD)
- No `GET /analytics/adherence` (covered by dashboard/streak)
- No log edit/delete (deferred)

## User-test blockers (ops, not missing product)

1. **EAS Android APK** (recommended): `cd mobile && npm run build:dev:android`
2. **LAN API**: Postgres + migrate + API; `EXPO_PUBLIC_API_URL=http://<LAN_IP>:5001`
3. **NFC hardware** for bottle demo; without it, use Manual Mark as Taken
4. **Mock email**: verification/reset tokens appear in API logs in Development — open `captap://verify-email?token=…` / `captap://reset-password?token=…`
5. Airplane-mode E2E still needs a human checklist pass (`docs/final-demo.md`)

## Tester script

See `docs/final-demo.md`. Minimum happy path without NFC:

1. Register / login  
2. Search Metformin → 500 mg → 8:00 / 20:00  
3. Mark as Taken → dashboard Taken + history  
4. Airplane Mode → log again → pending banner → reconnect sync  
5. Settings → Reminders / Privacy  

## Remaining non-blockers

- Screenshots folder empty  
- Live AWS not provisioned  
- VoiceOver/TalkBack store audit  
- Log PATCH (spec listed; product deferred)  
- EmailVerified not required at login (document for production hardening later)
