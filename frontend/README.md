# Workspace: three editions

A collaborative workspace and project management frontend in three designs that share one engine.

```powershell
npm install
npm run dev
```

Open http://localhost:5173 and pick an edition.

| Edition | URL | Navigation | Look |
|---|---|---|---|
| **Atelier** | `/atelier` | Collapsible sand sidebar | Soft cards, serif headlines, terracotta; split-screen sign-in |
| **Almanac** | `/almanac` | Masthead with top tabs | White paper, hairline rules, sage; broadsheet dashboard, table-style project list |
| **Studio** | `/studio` | Floating icon dock (bottom dock on phones) | Pill controls, large radii, sans type, amber; floating-card sign-in, springy motion |

All three have every feature. Use the palette button in the header to switch while staying on the same page.

## Demo mode

- Sign-in is pre-filled; any email with a 6+ character password works.
- 2FA and reset codes: any 6 digits except `000000`. Recovery code format: `abcde-12345`.
- Data is mocked in the browser and kept per tab (`sessionStorage`), so reloads keep your edits. Close the tab to reset.
- Chat replies are simulated.

## Connecting the backend

1. Copy `.env.example` to `.env.local`, set `VITE_USE_MOCK=false` and, if needed, `VITE_API_URL`.
2. For local development, add a `server.proxy` entry for `/api` in `vite.config.ts`.

- `src/api/services.ts`: every REST call the UI makes.
- `src/api/types.ts`: the expected response shapes.
- `src/api/mock/server.ts`: a route table showing the behaviour the UI expects (validation, status codes).

These paths and shapes are assumptions, since no backend spec was provided. Adjust them to match the real API.
The real mode has not been tested against a server. It assumes a bearer token (kept in `localStorage`;
consider an httpOnly cookie for production), JSON errors shaped `{ "message": string }`, multipart file
uploads, and Server-Sent Events for chat. Chat attachments currently send metadata and a local preview URL
only, so they need an upload endpoint. Permission checks in the UI are cosmetic: the server must enforce them.

## Layout

```
src/
  api/            REST client, services, types, mock server
  features/       one folder per area (auth, projects, chat, calendar, files, ...)
  components/ui/  buttons, forms, modals, drawers, skeletons
  components/layout/  the three shells and shared header pieces
  editions.tsx    per-edition motion settings
  styles/         base.css (shared) and editions.css (per-edition look)
```
