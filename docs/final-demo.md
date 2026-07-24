# CapTap Final Demo Guide (Phase 13)

Roadmap “Final Demo Flow” plus the Android-first device path (no Apple password required).

## Recommended device path (no Apple ID password)

Prefer an **NFC Android phone** for the full demo:

```bash
cd mobile
npx eas login
npm run build:dev:android
```

EAS produces an **APK** (`development` profile). Install it on the phone, then:

```bash
# Find your Mac LAN IP
ipconfig getifaddr en0

# Start API (separate terminal)
./scripts/dev.sh db-up
./scripts/dev.sh migrate
./scripts/dev.sh api

# Metro against the device
cd mobile
EXPO_PUBLIC_API_URL=http://<YOUR_LAN_IP>:5001 npx expo start --dev-client
```

Open the CapTap development build (not Expo Go), scan the QR / connect to Metro.

### iPhone without typing Apple password

Use an **App Store Connect API key** with `eas credentials` (not your Apple ID password).  
Simulator builds (`development-simulator`) are UI-only — **NFC will not work**.

## Scripted demo flow

1. **Open CapTap** (dev client) → Welcome → Create account.
2. **Search** OpenFDA: `Metformin`.
3. **Add** 500 mg, schedules **8:00 AM** and **8:00 PM**.
4. **Assign NFC** sticker from medication details (online once so `nfc_tag_cache` seeds).
5. **Tap bottle** → Confirm dose → Dashboard shows Taken + “Logged via NFC”.
6. **Airplane Mode** → Mark as Taken / cached NFC confirm → pending banner; reminder for that schedule cancels locally.
7. **Reconnect** → queue drains; history matches server.
8. **Settings → Reminders** → show offset + quiet hours (overnight deferral).

## Architecture talking points

| Topic | Point to |
|-------|----------|
| Clean Architecture | `server/` Api → Application → Domain / Infrastructure |
| Auth & security | Argon2id, JWT + refresh family reuse, rate limits, no PHI on NFC tags |
| Offline | SQLite queue + TanStack persistence — API remains source of truth |
| DevOps | GitHub Actions CI, Docker API image, AWS docs under `docs/aws-deployment.md` |
| Health | `/health/live` vs `/health/ready` |

## Airplane-mode E2E checklist

| Step | Pass? |
|------|-------|
| Warm cache online (dashboard + NFC assign) | ☐ |
| Airplane Mode ON | ☐ |
| Manual log → Taken + pending + **reminder cancelled** | ☐ |
| Cached NFC confirm → queued + **reminder cancelled** | ☐ |
| Airplane Mode OFF → auto sync, banner clears | ☐ |
| Quiet hours on → evening fire deferred to morning (unit-tested; optional live wait) | ☐ |

## Related docs

- `docs/production-validation.md`
- `docs/offline-architecture.md`
- `docs/aws-deployment.md`
- `docs/production-hardening.md`
