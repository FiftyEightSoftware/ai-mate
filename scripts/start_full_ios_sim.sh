#!/usr/bin/env bash
set -euo pipefail

# Start backend and frontend, then open the app URL in iOS Simulator Safari (macOS).
#
# Usage:
#   chmod +x scripts/start_full_ios_sim.sh
#   AIMATE_DB_PASSWORD='your-strong-passphrase' APP_URL='http://localhost:5173' ./scripts/start_full_ios_sim.sh
#
# Environment variables:
#   AIMATE_DB_PASSWORD    (optional) SQLCipher password for the encrypted SQLite DB (requires SQLCipher bundle)
#   APP_URL               (optional) URL to open in the Simulator (default derives from FRONTEND_PORT)
#   FRONTEND_DIR          (optional) Path to frontend dir (default: ai_mate_blazor)
#   FRONTEND_CMD          (optional) Command to start frontend (default binds to FRONTEND_PORT)
#   FRONTEND_PORT         (optional) Frontend port (default: 5173)
#   INVOICE_SEED_MIN/MAX  (optional) Invoice seeding bounds (defaults: 250/450)
#   JOB_SEED_MIN/MAX      (optional) Job seeding bounds (defaults: 80/200)
#   RESEED                (optional) If set to 1, clears and reseeds after backend is healthy (default: 1)
#   SIM_DEVICE            (optional) Exact simulator device name (e.g., "iPhone 15 Pro")

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="${FRONTEND_DIR:-$ROOT_DIR/ai_mate_blazor}"
# Configurable frontend port; derive APP_URL from it by default
FRONTEND_PORT="${FRONTEND_PORT:-5173}"
FRONTEND_CMD=${FRONTEND_CMD:-dotnet run --urls "http://localhost:${FRONTEND_PORT}"}
APP_URL="${APP_URL:-http://localhost:${FRONTEND_PORT}}"
SIM_DEVICE="${SIM_DEVICE:-}"
RESEED="${RESEED:-1}"

# AIMATE_DB_PASSWORD is optional. If set, the backend will use encrypted DB (requires SQLCipher).

if ! command -v xcrun >/dev/null 2>&1; then
  echo "[FATAL] xcrun not found. Please install Xcode command line tools: xcode-select --install" >&2
  exit 1
fi

# Start backend
pushd "$BACKEND_DIR" >/dev/null
  echo "[INFO] Restoring backend packages..."
  dotnet restore
  echo "[INFO] Starting backend on http://0.0.0.0:5280 ..."
  # Pass seeding bounds and CORS origins to backend
  FRONTEND_ORIGINS="http://localhost:${FRONTEND_PORT},http://127.0.0.1:${FRONTEND_PORT}" \
  INVOICE_SEED_MIN="${INVOICE_SEED_MIN:-250}" INVOICE_SEED_MAX="${INVOICE_SEED_MAX:-450}" \
  JOB_SEED_MIN="${JOB_SEED_MIN:-80}" JOB_SEED_MAX="${JOB_SEED_MAX:-200}" \
  ${AIMATE_DB_PASSWORD:+AIMATE_DB_PASSWORD="$AIMATE_DB_PASSWORD"} dotnet run --urls "http://0.0.0.0:5280" &
  BACKEND_PID=$!
  echo "[INFO] Backend PID: $BACKEND_PID"
popd >/dev/null

# Start frontend
pushd "$FRONTEND_DIR" >/dev/null
  echo "[INFO] Starting frontend: $FRONTEND_CMD (in $FRONTEND_DIR) ..."
  bash -lc "$FRONTEND_CMD" &
  FRONTEND_PID=$!
  echo "[INFO] Frontend PID: $FRONTEND_PID"
popd >/dev/null

# Cleanup on exit
cleanup() {
  echo "[INFO] Stopping frontend (PID: $FRONTEND_PID) ..."
  if ps -p "$FRONTEND_PID" >/dev/null 2>&1; then
    kill "$FRONTEND_PID" || true
    wait "$FRONTEND_PID" || true
  fi
  echo "[INFO] Stopping backend (PID: $BACKEND_PID) ..."
  if ps -p "$BACKEND_PID" >/dev/null 2>&1; then
    kill "$BACKEND_PID" || true
    wait "$BACKEND_PID" || true
  fi
}
trap cleanup EXIT INT TERM

# Wait for backend health
HEALTH_URL="http://localhost:5280/api/health"
printf "[INFO] Waiting for backend health %s" "$HEALTH_URL"
for i in {1..30}; do
  if curl -sf "$HEALTH_URL" >/dev/null 2>&1; then
    echo " ... OK"
    break
  fi
  printf "."; sleep 1
  if [[ "$i" == 30 ]]; then echo "\n[WARN] Backend health not responding yet, continuing..."; fi
done

# Optional reseed to guarantee a fresh realistic dataset
if [[ "$RESEED" == "1" ]]; then
  echo "[INFO] Triggering reseed ..."
  if ! curl -sf -X POST "http://localhost:5280/api/dev/reseed" >/dev/null 2>&1; then
    echo "[WARN] Reseed request failed; proceeding with existing data" >&2
  else
    echo "[INFO] Reseed complete"
  fi
fi

# Wait for frontend to be reachable
printf "[INFO] Waiting for frontend %s" "$APP_URL"
for i in {1..60}; do
  if curl -sf "$APP_URL" >/dev/null 2>&1; then
    echo " ... OK"
    break
  fi
  printf "."; sleep 1
  if [[ "$i" == 60 ]]; then echo "\n[WARN] Frontend not responding yet, continuing..."; fi
done

# Boot iOS Simulator
open -a Simulator || true
sleep 2

# Pick a device
if [[ -z "$SIM_DEVICE" ]]; then
  CANDIDATES=("iPhone 15 Pro" "iPhone 15" "iPhone 14 Pro" "iPhone 14" "iPhone SE (3rd generation)")
  for name in "${CANDIDATES[@]}"; do
    if xcrun simctl list devices available | grep -q "$name"; then SIM_DEVICE="$name"; break; fi
  done
  if [[ -z "$SIM_DEVICE" ]]; then
    SIM_DEVICE=$(xcrun simctl list devices available | grep -m1 -E "iPhone" | sed -E 's/^\s*([^\(]+) \(([^\)]+)\).*/\1/' | head -n1)
  fi
fi

if [[ -z "$SIM_DEVICE" ]]; then
  echo "[FATAL] Could not find an available iPhone simulator device." >&2
  exit 1
fi

echo "[INFO] Using Simulator device: $SIM_DEVICE"
xcrun simctl boot "$SIM_DEVICE" 2>/dev/null || true
sleep 2

# Open URL
xcrun simctl openurl booted "$APP_URL" || echo "[WARN] Failed to open URL in Simulator" >&2

echo "[INFO] App URL opened in iOS Simulator: $APP_URL"
echo "[INFO] Press Ctrl+C to stop."
# Keep script running to keep both processes alive
while true; do sleep 3600; done
