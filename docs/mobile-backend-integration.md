# CapTap Mobile ↔ Backend Integration

Phase 7 replaces the Phase 6 mock layer with live communication against the
CapTap ASP.NET Core API. Auth, dashboard, medications, and scheduling now flow
through a single typed networking stack. Notifications and full offline mode
remain later phases; NFC assign/scan/confirm is Phase 9 (`docs/nfc-integration.md`).
are intentionally still out of scope.

## Goals

Users should never have to think about authentication, expired sessions,
networking, or loading management. Everything "just works":

- Tokens persist securely and restore on launch.
- Access tokens refresh transparently on `401`.
- Every screen shows consistent loading / empty / error states.
- All server state is cached and invalidated through TanStack Query.

## API architecture

```
UI screens (app/**)
      │  (never import axios or tokens directly)
      ▼
React Query hooks (hooks/**)        ← loading / error / cache / invalidation
      ▼
API modules (api/auth|medication|dashboard|schedule)  ← typed calls, unwrap ApiResponse
      ▼
Axios client (api/client.ts)        ← base URL, auth header, 401 refresh, error mapping
      ▼
CapTap API (/api/v1/**)
```

Rules:

- `api/*` modules contain **networking only** — no React, no UI, no hooks.
- Screens consume **hooks only** — they never touch Axios or tokens.
- Wire DTOs (`api/types.ts`) are mapped to UI domain types in `api/mappers.ts`,
  so components are insulated from contract changes.

### Files

| File | Responsibility |
|------|----------------|
| `api/client.ts` | The single Axios instance + interceptors + `unwrap`/`normalizeError` |
| `api/session.ts` | Token store (SecureStore), single-flight refresh, expiry events |
| `api/errors.ts` | `ApiClientError`, status→kind mapping, `toUserMessage`, retry policy |
| `api/types.ts` | Wire DTOs mirroring the backend (camelCase, string enums) |
| `api/mappers.ts` | DTO → domain (`Medication`, `TodayDose`, `DashboardSummary`) |
| `api/auth.ts` / `medication.ts` / `dashboard.ts` / `schedule.ts` | Endpoint calls |
| `api/endpoints.ts` | Centralized paths (never hardcode URLs) |
| `context/AuthContext.tsx` | App-facing auth state + actions |
| `context/QueryProvider.tsx` | TanStack Query configuration |
| `hooks/**` | `useLogin`, `useDashboard`, `useMedications`, `useSchedules`, … |

## Authentication flow

```
Login / Register screen
   → useLogin / useRegister (mutation)
   → AuthContext.signIn/signUp
   → authApi.login  (POST /api/v1/auth/login)
   → session.setTokens()  → SecureStore + memory
   → user decoded from JWT claims → status "authenticated"
   → router.replace(tabs)
```

- **Register** creates an active account (`POST /auth/register`) and then signs in
  immediately, so users never see an extra login step.
- **Forgot password** posts to `/auth/forgot-password` and always shows the same
  enumeration-safe confirmation.
- The signed-in user's `id` / `email` are read by decoding the JWT payload
  (`utils/jwt.ts`). This is display-only; the server re-validates every request.

## Token lifecycle

| Token | Lifetime | Storage |
|-------|----------|---------|
| Access | 15 min | Expo SecureStore + in-memory (for the interceptor) |
| Refresh | 30 days | Expo SecureStore only |

- Tokens are **never** stored in AsyncStorage and never exposed to UI components.
- On launch, `AuthContext` calls `session.restore()` to hydrate memory from
  SecureStore; the app shows a splash while `status === "restoring"`.
- `signOut` best-effort revokes the refresh token server-side
  (`POST /auth/logout`) and always clears local storage.

### Automatic refresh (401 recovery)

```
request → 401 (non-auth route, not already retried)
        → session.refresh()   (single-flight; POST /auth/refresh via a bare client)
        → success: store rotated pair, retry original request once
        → failure: session.expire() → clear tokens + notify AuthContext → Login
```

- Refresh is **de-duplicated**: concurrent 401s share one refresh request.
- Auth routes (`/auth/*`) never trigger refresh — a 401 there is a real
  credential failure surfaced to the user.
- The refresh call uses a separate bare Axios instance to avoid interceptor
  recursion.

## Axios interceptors

**Request**
- Attaches `Authorization: Bearer <accessToken>` from the session store.
- Development-only logging of method + URL. Headers and bodies are never logged
  (they can contain tokens or passwords).

**Response**
- Development-only logging of status + URL.
- 401 → transparent refresh + single retry (see above).
- All failures are normalized into `ApiClientError` via `normalizeError`.

## Error handling

`ApiClientError` carries a coarse `kind` derived from the HTTP status:

| Kind | Trigger | User copy (via `toUserMessage`) |
|------|---------|----------------------------------|
| `network` | no response | "No internet connection…" |
| `timeout` | `ECONNABORTED` | "That took too long…" |
| `unauthorized` | 401 | session-expired / backend message |
| `forbidden` | 403 | "You don't have access to that." |
| `notFound` | 404 | "We couldn't find what you were looking for." |
| `validation` | 400 | backend message (safe, actionable) |
| `conflict` | 409 | backend message |
| `rateLimited` | 429 | "Too many attempts…" |
| `server` | 5xx | generic "something went wrong on our end" |

- Backend messages are surfaced only for safe cases (validation / auth); server
  internals are never leaked.
- Screens render errors consistently via `ErrorState` (full screen) and
  `FormError` (inline on forms), both fed by `toUserMessage`.

## React Query strategy

Configured in `context/QueryProvider.tsx`:

- `staleTime` 30s, `gcTime` 5 min — warm data across tab switches.
- Retries only **transient** failures (network / timeout / 5xx), max 2, with
  exponential backoff. 4xx errors never retry.
- Mutations do not retry.
- `refetchOnReconnect` is on; `focusManager` is wired to React Native
  `AppState`, so returning to the foreground triggers a background refetch (the
  mobile analog of window focus).

### Query keys & invalidation

| Data | Key | Invalidated by |
|------|-----|----------------|
| Dashboard | `["dashboard"]` | create/update/archive medication, schedule writes |
| Medication list | `["medications"]` | create / update / archive |
| Medication detail | `["medications", id]` | update / archive |
| Schedules | `["schedules", medicationId]` | schedule create / update / delete |
| Search | `["medication-search", q]` | n/a (5 min stale) |

## Loading states

- **Skeletons** (`Skeleton`, `SkeletonCard`) on dashboard, medication list, and
  detail while first-loading.
- **Button spinners** via the shared `Button` `loading` prop on every submit.
- **Pull-to-refresh** on dashboard and medication list (`RefreshControl` through
  the `Screen` component).
- **Empty states** for no medications / no doses / no search results.

## Environment configuration

- `EXPO_PUBLIC_API_URL` is the only required value; read centrally in
  `constants/env.ts` (never `process.env` elsewhere).
- Loaded per mode from `.env.development` / `.env.production` (Expo inlines only
  `EXPO_PUBLIC_*`). `.env.example` documents the variable.
- Production must be HTTPS. For a physical device in development, set the URL to
  your machine's LAN IP (e.g. `http://192.168.1.20:5001`); `localhost` only works
  on simulators / web.

## Dashboard derivation (backend gap)

The API exposes `GET /dashboard/today` and `GET /dashboard/missed` returning
dose lists. The mobile summary is derived client-side in `buildDashboardSummary`:

- **Upcoming** = today's doses with status `Upcoming`.
- **Completion %** = `Taken / total` of today's doses.
- **Longest streak** and **current streak** are provided by
  `GET /api/v1/dashboard/today` / `/streak` (Phase 8). See
  [`docs/medication-logging.md`](./medication-logging.md).

## Testing

`npm test` (jest-expo). API/interceptor tests use `axios-mock-adapter`; hook and
context tests use a `QueryClient` wrapper with an in-memory SecureStore mock.

| Area | Covered |
|------|---------|
| Session store | persist / restore / clear, single-flight refresh, expiry notify |
| Auth API | successful login, failed login (typed error), network failure, no-refresh on auth routes |
| 401 recovery | refresh + retry with rotated token; unauthorized when refresh fails |
| Auth context | session restoration, successful login, failed login |
| Medications | retrieval + mapping, create, network-failure error |
| Dashboard | load + derived completion / upcoming / missed |
| Components | Button, Input, MedicationCard, DoseCard, EmptyState, LoadingSpinner, navigation |

## Security checklist

- Tokens in SecureStore only; never AsyncStorage; never rendered in UI.
- Authorization handled centrally in one interceptor.
- No headers, bodies, tokens, or passwords are logged (dev logging is
  method + URL only).
- HTTPS in production (`.env.production`).
- Input validation is server-authoritative; client Zod schemas are UX only.
- Generic error copy; backend messages surfaced only for safe cases.

## Future API integration

1. Quiet-hours enforcement using stored preferences.
2. Remote push / APNs if local reminders are insufficient.
3. Broader settings surfaces (theme, privacy copy).
