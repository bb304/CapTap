# CapTap Mobile Architecture

Phase 6 established the Expo / React Native foundation. Phases 7–9 wired live auth,
dashboard, medication logging, and NFC confirm-to-log against the ASP.NET API.
Mock data has been removed; see `docs/mobile-backend-integration.md` and
`docs/nfc-integration.md`.

## Goals

CapTap should feel **calm, approachable, and trustworthy**, with large touch targets and low cognitive load for older adults and ADHD-friendly flows.

Tagline: *Tap. Confirm. Peace of mind.*

## Folder structure

```
mobile/
  src/
    app/                 # Expo Router routes (screens)
    screens/             # Reserved for shared presenters
    components/
      ui/                # Design system primitives
      medication/
      dashboard/
      common/
      forms/
    navigation/          # Route helpers (re-exports)
    context/             # Auth placeholder + React Query
    hooks/
    services/            # Mock service interfaces
    api/                 # Endpoint constants (future HTTP)
    constants/           # Mock data + routes
    theme/               # Colors, type, space, radius, shadows
    types/
    utils/
    assets/
    __tests__/
```

## Navigation

```
Launch → Welcome
            ├─ Login (mock) ──┐
            └─ Register (mock)┤
                              ▼
                     Main tabs
                     ├─ Dashboard
                     ├─ My Medications → Add / Details
                     └─ Settings
```

Auth is a **local placeholder** (`AuthContext`). Signing in only sets mock user state.

## Theme

Centralized in `src/theme`:

| Token | Role |
|-------|------|
| Soft blue `#3B82A0` | Primary actions |
| White / light gray | Surfaces |
| Success / warning / error | Status only |
| Body 18 / titles 28+ | Readable type |

NativeWind is configured (`global.css`, `tailwind.config.js`) for utility styling; shared components primarily use theme StyleSheets for consistency and testability.

## Reusable components

UI: `Button`, `Card`, `Input`, `SearchBar`, `Screen`, `SectionHeader`, `LoadingSpinner`, `EmptyState`, `Modal`, `Badge`, `Chip`

Domain: `MedicationCard`, `DoseCard`, `StatCard`, `FloatingActionButton`

States: `ErrorState`, `OfflinePlaceholder`, `NoResults`

## Accessibility

- Minimum **44×44** touch targets
- `accessibilityLabel` / `accessibilityRole` on interactive controls
- `maxFontSizeMultiplier` on text
- High-contrast ink on soft backgrounds
- Logical screen order: brand → copy → actions

## Technology choices

| Concern | Choice |
|---------|--------|
| Framework | Expo SDK 57 + TypeScript |
| Routing | Expo Router (`src/app`) |
| Server state | TanStack Query (installed; mock queries now) |
| Forms | React Hook Form + Zod |
| Icons | Lucide React Native |
| Motion | Reanimated (available; subtle use) |
| Secrets / offline | SecureStore + SQLite placeholders |

## API integration

Phase 6 established this foundation with mock data. **Phase 7 replaced the mock
layer with live backend integration** — see
[`docs/mobile-backend-integration.md`](./mobile-backend-integration.md) for the
Axios client, secure token lifecycle, automatic refresh, React Query strategy,
and error handling.

## Running

```bash
cd mobile
npm install
npm start
npm test
```
