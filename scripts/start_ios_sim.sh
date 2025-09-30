#!/usr/bin/env bash
set -euo pipefail

# Startup script to run the encrypted SQLite backend and open the app
# in the iOS Simulator (Safari) on macOS.
#
# Usage:
#   chmod +x scripts/start_ios_sim.sh
#   AIMATE_DB_PASSWORD='your-strong-passphrase' APP_URL='http://localhost:5173' ./scripts/start_ios_sim.sh
#
# Environment variables:
#   AIMATE_DB_PASSWORD  (required) SQLCipher password for the encrypted SQLite DB
#   APP_URL             (optional) URL to open in the Simulator (default: http://localhost:5173)
#   SIM_DEVICE          (optional) Exact simulator device name (e.g., "iPhone 15 Pro")

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
APP_URL="${APP_URL:-http://localhost:5173}"
SIM_DEVICE="${SIM_DEVICE:-}"

if [[ -z "${AIMATE_DB_PASSWORD:-}" ]]; then
  echo "[FATAL] AIMATE_DB_PASSWORD is not set. Export a strong passphrase before running." >&2
  echo "Example: export AIMATE_DB_PASSWORD='your-strong-passphrase'" >&2
  exit 1
fi

# Ensure Simulator app exists
if ! command -v xcrun >/dev/null 2>&1; then
  echo "[FATAL] xcrun not found. Please install Xcode command line tools: xcode-select --install" >&2
  exit 1
fi

# Start backend
pushd "$BACKEND_DIR" >/dev/null
  echo "[INFO] Restoring backend packages..."
  dotnet restore
  echo "[INFO] Starting backend on http://0.0.0.0:5280 ..."
  # Run in background
  AIMATE_DB_PASSWORD="$AIMATE_DB_PASSWORD" dotnet run &
  BACKEND_PID=$!
  echo "[INFO] Backend PID: $BACKEND_PID"
popd >/dev/null

# Cleanup on exit
cleanup() {
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
  printf "."
  sleep 1
  if [[ "$i" == 30 ]]; then
    echo "\n[WARN] Backend health not responding yet, continuing anyway..."
  fi
done

# Boot iOS Simulator
open -a Simulator || true
sleep 2

# Pick a device
if [[ -z "$SIM_DEVICE" ]]; then
  # Try common devices; fall back to the first available iPhone
  CANDIDATES=("iPhone 15 Pro" "iPhone 15" "iPhone 14 Pro" "iPhone 14" "iPhone SE (3rd generation)")
  for name in "${CANDIDATES[@]}"; do
    if xcrun simctl list devices available | grep -q "$name"; then
      SIM_DEVICE="$name"
      break
    fi
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
# Boot the device (idempotent)
xcrun simctl boot "$SIM_DEVICE" 2>/dev/null || true
sleep 2

# Open the URL in the Simulator's Safari
xcrun simctl openurl booted "$APP_URL" || {
  echo "[WARN] Failed to open URL in booted device. Attempting to launch Safari manually..." >&2
}

echo "[INFO] App URL opened in iOS Simulator: $APP_URL"

echo "[INFO] Press Ctrl+C to stop."
# Keep script running to keep backend alive
while true; do sleep 3600; done
