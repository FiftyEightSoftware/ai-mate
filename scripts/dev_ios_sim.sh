#!/usr/bin/env bash
set -euo pipefail

# Dev helper: auto-detect & start backend + frontend, discover the app URL,
# then open it in the iOS Simulator Safari on macOS.
#
# Usage:
#   chmod +x scripts/dev_ios_sim.sh
#   AIMATE_DB_PASSWORD='your-strong-passphrase' ./scripts/dev_ios_sim.sh
#
# Optional env vars:
#   FRONTEND_DIR  Path to frontend (default: ai_mate_blazor)
#   FRONTEND_CMD  Command to start frontend (default: auto-detect)
#   SIM_DEVICE    Exact simulator device name (e.g., "iPhone 15 Pro")
#   APP_URL       If provided, skips detection and uses this URL

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="${FRONTEND_DIR:-$ROOT_DIR/ai_mate_blazor}"
SIM_DEVICE="${SIM_DEVICE:-}"

if [[ -z "${AIMATE_DB_PASSWORD:-}" ]]; then
  echo "[FATAL] AIMATE_DB_PASSWORD is not set." >&2
  echo "Example: export AIMATE_DB_PASSWORD='your-strong-passphrase'" >&2
  exit 1
fi

if ! command -v xcrun >/dev/null 2>&1; then
  echo "[FATAL] xcrun not found. Please install Xcode command line tools: xcode-select --install" >&2
  exit 1
fi

# Start backend
pushd "$BACKEND_DIR" >/dev/null
  echo "[INFO] Restoring backend packages..."
  dotnet restore
  echo "[INFO] Starting backend on http://0.0.0.0:5280 ..."
  AIMATE_DB_PASSWORD="$AIMATE_DB_PASSWORD" dotnet run &
  BACKEND_PID=$!
  echo "[INFO] Backend PID: $BACKEND_PID"
popd >/dev/null

# Cleanup on exit
cleanup() {
  echo "[INFO] Stopping frontend (PID: ${FRONTEND_PID:-}) ..."
  if [[ -n "${FRONTEND_PID:-}" ]] && ps -p "$FRONTEND_PID" >/dev/null 2>&1; then
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

# Start frontend (auto-detect)
start_frontend() {
  local cmd="${FRONTEND_CMD:-}"
  if [[ -z "$cmd" ]]; then
    # Prefer dotnet if a csproj exists
    if ls "$FRONTEND_DIR"/*.csproj >/dev/null 2>&1; then
      cmd="dotnet run"
    elif [[ -f "$FRONTEND_DIR/package.json" ]]; then
      # Fallback to npm dev
      cmd="npm run dev"
    else
      # Try dotnet anyway
      cmd="dotnet run"
    fi
  fi
  echo "[INFO] Starting frontend: $cmd (in $FRONTEND_DIR) ..."
  pushd "$FRONTEND_DIR" >/dev/null
    bash -lc "$cmd" &
    FRONTEND_PID=$!
  popd >/dev/null
  echo "[INFO] Frontend PID: ${FRONTEND_PID:-unknown}"
}

# If APP_URL is given, skip starting frontend and detection
APP_URL="${APP_URL:-}"
if [[ -z "$APP_URL" ]]; then
  start_frontend
fi

# Probe common dev ports to determine APP_URL if not provided
if [[ -z "$APP_URL" ]]; then
  ports=(5173 5000 5001 8080 3000 4200)
  schemes=(http https)
  printf "[INFO] Detecting frontend URL"
  for p in "${ports[@]}"; do
    for s in "${schemes[@]}"; do
      url="$s://localhost:$p"
      if curl -sf "$url" >/dev/null 2>&1; then APP_URL="$url"; break 2; fi
    done
    printf "."
    sleep 1
  done
  echo
  if [[ -z "$APP_URL" ]]; then
    echo "[WARN] Could not detect frontend URL; defaulting to http://localhost:5173"
    APP_URL="http://localhost:5173"
  fi
fi

# Boot iOS Simulator and open URL
open -a Simulator || true
sleep 2
if [[ -z "$SIM_DEVICE" ]]; then
  CANDIDATES=("iPhone 15 Pro" "iPhone 15" "iPhone 14 Pro" "iPhone 14" "iPhone SE (3rd generation)")
  for name in "${CANDIDATES[@]}"; do
    if xcrun simctl list devices available | grep -q "$name"; then SIM_DEVICE="$name"; break; fi
  done
  if [[ -z "$SIM_DEVICE" ]]; then
    SIM_DEVICE=$(xcrun simctl list devices available | grep -m1 -E "iPhone" | sed -E 's/^\s*([^\(]+) \(([^\)]+)\).*/\1/' | head -n1)
  fi
fi
if [[ -z "$SIM_DEVICE" ]]; then echo "[FATAL] No available iPhone Simulator found" >&2; exit 1; fi
xcrun simctl boot "$SIM_DEVICE" 2>/dev/null || true
sleep 2
xcrun simctl openurl booted "$APP_URL" || echo "[WARN] Failed to open URL in Simulator" >&2

echo "[INFO] App URL opened in iOS Simulator: $APP_URL"
echo "[INFO] Press Ctrl+C to stop."
while true; do sleep 3600; done
