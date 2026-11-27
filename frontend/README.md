# Atelier workspace

A collaborative workspace and project management frontend: a collapsible sand sidebar, soft cards,
serif headlines and a terracotta accent.

```powershell
npm install
npm run dev
```

Open http://localhost:5173.

## Demo mode

- Sign-in is pre-filled; any email with a 5+ character password works.
- 2FA codes: any 6 digits except `000000`. Recovery code format: `ABCD-2345`.
- Data is mocked in the browser and kept per tab (`sessionStorage`), so reloads keep your edits. Close the tab to reset.
- Chat replies are simulated.

## Connecting the backend

1. Copy `.env.example` to `.env.local` and set `VITE_USE_MOCK=false`.
2. Start the backend with its `http` launch profile (port 5243). `vite.config.ts` proxies `/api` to it.
3. For password-reset emails, set `FrontendUrl` in the backend settings to this app's address, e.g. `http://localhost:5173`.

**Auth, session and profile** are connected and follow the backend's controllers and DTOs. With the real API,
signed-in users see only the account pages; the workspace modules still use assumed paths and shapes and run in demo mode only.

- `src/api/authClient.ts`: the axios instance, token refresh and logout.
- `src/api/session.ts`: token storage (`localStorage`, shared across tabs).
- `src/api/auth.ts`, `src/api/users.ts`, `src/api/authTypes.ts`: the connected endpoints and their DTOs.
- `src/api/mock/auth.ts`: their demo-mode stand-in.
- `src/api/services.ts`, `src/api/types.ts`: the remaining, not yet connected modules.

Tokens come back in the response body, so they are readable by scripts on this origin. Permission checks in
the UI are cosmetic: the server must enforce them. Chat uses Server-Sent Events in the not-yet-connected code,
while the backend exposes SignalR.

## Layout

```
src/
  api/            REST client, services, types, mock server
  features/       one folder per area (auth, projects, chat, calendar, files, ...)
  components/ui/  buttons, forms, modals, drawers, skeletons
  components/layout/  the app shell and shared header pieces
  lib/motion.ts   shared motion settings
  styles/         base.css (design tokens and component classes)
```
