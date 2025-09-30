# AI-Mate HTMX PWA Skeleton

<!-- CI Status Badge -->
<!-- Replace OWNER/REPO with your GitHub org/repo -->
![CI](https://github.com/OWNER/REPO/actions/workflows/dotnet-tests.yml/badge.svg)

## Test Coverage

<!-- After GitHub Pages is enabled and workflow runs, coverage badges and report will be available at these URLs. Replace OWNER/REPO accordingly. -->
![Line Coverage](https://OWNER.github.io/REPO/coverage-report/Badges/line.svg)
![Branch Coverage](https://OWNER.github.io/REPO/coverage-report/Badges/branch.svg)

Full coverage report: https://OWNER.github.io/REPO/coverage-report/

A lightweight, mobile-first Progressive Web App scaffold using HTMX and Vite (as a static dev server and bundler). It provides basic screens and transitions only.

## Features
- Home, Jobs, Quotes, Invoices, Expenses, Clients, Assistant, Settings screens
- Bottom navigation and simple fade transitions
- HTMX partial loading with URL history
- PWA: manifest + service worker for offline shell

## Project Structure
- `index.html` – HTMX app shell and bottom nav
- `pages/` – HTML partials for each screen
- `src/styles.css` – mobile-first styles
- `manifest.webmanifest` – PWA manifest
- `sw.js` – service worker (cache shell + pages)

Note: There are some unused React files (`src/main.jsx`, etc.) from an earlier scaffold. The app does not use them.

## Getting Started

1. Install dependencies
```bash
npm install
```

2. Start dev server
```bash
npm run dev
```
Vite will print a local URL (default http://localhost:5173). Open it in a mobile-sized viewport.

3. Build for production
```bash
npm run build
npm run preview
```

## PWA Notes
- The manifest is referenced from `/manifest.webmanifest` and icons should be placed under `/icons/`.
- The service worker is at `/sw.js` and registered via `/src/swRegistration.js`.
- For full installability, add 192x192 and 512x512 PNG icons at:
  - `icons/icon-192.png`
  - `icons/icon-512.png`
  - `icons/maskable-icon-192.png`
  - `icons/maskable-icon-512.png`

## Next Steps
- Hook up real data sources for each screen
- Implement voice assistant functionality
- Add offline data persistence (e.g., IndexedDB)
- Add theming and auth

## Deploy to Azure (Backend + Frontend)

This repo includes infrastructure to deploy:

- Backend (`backend/` .NET API + SQLite) to Azure Container Apps (ACA) with a persistent Azure Files volume.
- Frontend (`ai_mate_blazor/` Blazor WASM) to Azure Static Web Apps (SWA) with a proxy to the backend.

### 1) Provision Azure resources (one‑time)

Use Azure CLI to create a Resource Group, Storage Account + File Share (for DB persistence), and a Container Apps Environment. Replace placeholders as needed.

```bash
az login
az account set --subscription "<SUBSCRIPTION_ID>"
az group create -n rg-ai-mate -l westeurope

# Storage (Azure Files) for DB persistence
az storage account create -g rg-ai-mate -n aimatestorage$RANDOM -l westeurope --sku Standard_LRS
SA=$(az storage account list -g rg-ai-mate --query "[0].name" -o tsv)
SAKEY=$(az storage account keys list -g rg-ai-mate -n $SA --query "[0].value" -o tsv)
az storage share-rm create --resource-group rg-ai-mate --storage-account $SA --name aimate-db --quota 5

# Container Apps env
az containerapp env create -g rg-ai-mate -n cae-aimate -l westeurope
```

### 2) Build and publish backend image to GHCR

Push to `main` to build/push the backend Docker image (see `.github/workflows/backend.yml`). The image will be available at `ghcr.io/<owner>/<repo>/backend:latest`.

### 3) Deploy backend to Container Apps

Run the manual workflow "backend-aca-deploy" (Actions → backend-aca-deploy → Run workflow) with inputs:

- `image_ref`: `ghcr.io/<owner>/<repo>/backend:latest`
- `resource_group`: `rg-ai-mate`
- `containerapp_name`: e.g., `api-aimate`
- `env_FRONTEND_ORIGINS`: `https://<your-frontend-domain>`
- (optional) seed ranges: `env_INVOICE_SEED_MIN/MAX`, `env_JOB_SEED_MIN/MAX`

Required GitHub repository secrets for ACA deploy:

- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (OIDC service principal for Azure Login)
- `AIMATE_DB_PASSWORD` (strong passphrase for encrypted DB; optional but recommended)

Notes:

- The app writes data to `/app/data`. Mount an Azure Files volume when first creating the Container App (portal/CLI) so data persists. The workflow updates image/env; initial volume mount is a one-time portal/CLI step.
- Seeding runs automatically on first boot, controlled by env vars: `INVOICE_SEED_MIN/MAX` (defaults 250/450) and `JOB_SEED_MIN/MAX` (defaults 80/200).

### 4) Deploy frontend to Static Web Apps

Add repository secrets:

- `AZURE_STATIC_WEB_APPS_API_TOKEN` (from SWA resource → Deployment tokens)
- `BACKEND_URL` (Container App FQDN such as `https://api-aimate.<region>.azurecontainerapps.io`)

Push to `main` (or manually run) the workflow `.github/workflows/frontend_swa.yml`. It will:

- Publish Blazor WASM to `publish_blazor/wwwroot`
- Inject SWA proxy config at `ai_mate_blazor/wwwroot/staticwebapp.config.json` by replacing `__BACKEND_URL__`
- Upload to SWA

SWA will serve the frontend and proxy `/api/*` to your backend.

### 5) Verify

- Open the SWA URL (e.g., `https://<yourapp>.azurestaticapps.net`).
- Dashboard should show realistic totals and charts.
- Backend health: `https://<backend-fqdn>/api/health`.

### Optional operations

- Reseed (dev only): `POST https://<backend-fqdn>/api/dev/reseed`
- Adjust seed volume without code changes via ACA env vars: `INVOICE_SEED_MIN/MAX`, `JOB_SEED_MIN/MAX`.
- CORS origins are controlled via `FRONTEND_ORIGINS` env in the backend.

## CI and Coverage

- The workflow at `.github/workflows/dotnet-tests.yml` runs tests for `ai_mate_blazor.Tests` and `api.Tests`, collects coverage, generates HTML/Text/JSON badges, and enforces thresholds (95% line, 85% branch) globally and per-assembly (`ai_mate_blazor`, `api`).
- On push to `main`/`master`, the `coverage-report/` is published to GitHub Pages for stable badge/report URLs.
- To enable Pages: Settings → Pages → Source: GitHub Actions.

---

## Blazor WASM App (ai_mate_blazor)

We also include a Blazor WebAssembly PWA wrapper located at `ai_mate_blazor/` with the same screens, branding, and PWA support.

### Run locally

```bash
dotnet run --project ai_mate_blazor
```

The dev server will print a local URL (e.g., `http://localhost:5210`).

Or start both (backend if present + frontend) with the helper script:

```bash
./scripts/start.sh
# override ports
BACKEND_PORT=5280 FRONTEND_PORT=5211 ./scripts/start.sh
```

### Test on iOS Simulator

1. Launch the iOS Simulator:
   ```bash
   open -a Simulator
   ```
2. Boot a device (if none is booted):
   ```bash
   xcrun simctl boot "iPhone 16 Pro"
   ```
3. Open the app URL in the simulator Safari:
   ```bash
   xcrun simctl openurl booted http://localhost:<FRONTEND_PORT>
   ```
4. Add to Home Screen to test PWA install: Share → Add to Home Screen. Relaunch from the home screen to verify standalone mode, copper status bar, and that the install tip no longer shows.

Notes:
- The iOS install banner only appears on iOS Safari when not running in standalone mode and hides permanently after dismissal or installation.
- Safe-area insets are handled for the toolbar and bottom navigation.
