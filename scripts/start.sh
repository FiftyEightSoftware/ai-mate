#!/usr/bin/env bash
set -euo pipefail

# Project root is the directory containing this script's parent
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Configurable ports
BACKEND_PORT=${BACKEND_PORT:-5280}
FRONTEND_PORT=${FRONTEND_PORT:-5173}

# PIDs for cleanup
PIDS=()
cleanup() {
  echo -e "\n${YELLOW}Shutting down...${NC}"
  for pid in "${PIDS[@]:-}"; do
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
    fi
  done
}
trap cleanup EXIT INT TERM

# Start backend if present
start_backend() {
  local api_dir=""
  if [ -d "$ROOT_DIR/api" ]; then api_dir="$ROOT_DIR/api"; fi
  if [ -d "$ROOT_DIR/backend" ]; then api_dir="$ROOT_DIR/backend"; fi

  if [ -n "$api_dir" ]; then
    echo -e "${GREEN}Starting backend in $api_dir on http://localhost:${BACKEND_PORT}${NC}"
    (cd "$api_dir" && dotnet run --urls "http://localhost:${BACKEND_PORT}") &
    PIDS+=("$!")
  else
    echo -e "${YELLOW}No backend directory found (api/ or backend/). Skipping backend start.${NC}"
  fi
}

# Start Blazor WASM dev server
start_frontend() {
  echo -e "${GREEN}Starting Blazor WASM (ai_mate_blazor) on http://localhost:${FRONTEND_PORT}${NC}"
  (dotnet run --project ai_mate_blazor --urls "http://localhost:${FRONTEND_PORT}") &
  PIDS+=("$!")
}

# Wait for a URL to respond
wait_for_url() {
  local url="$1"
  echo -n "Waiting for ${url}"
  for i in {1..60}; do
    if curl -IfsS "$url" >/dev/null 2>&1; then
      echo -e " \n${GREEN}Ready:${NC} $url"
      return 0
    fi
    echo -n "."
    sleep 1
  done
  echo -e "\n${YELLOW}Timeout waiting for ${url}${NC}"
}

start_backend
start_frontend

wait_for_url "http://localhost:${FRONTEND_PORT}"

# Open in iOS Simulator by default (set IOS_SIM=0 to disable)
IOS_SIM=${IOS_SIM:-1}
URL="http://localhost:${FRONTEND_PORT}"
SIM_URL="http://127.0.0.1:${FRONTEND_PORT}"
if [ "$IOS_SIM" = "1" ] && command -v xcrun >/dev/null 2>&1; then
  echo -e "${GREEN}Opening in iOS Simulator: ${SIM_URL}${NC}"
  # Launch Simulator app if not running
  if command -v open >/dev/null 2>&1; then open -a Simulator >/dev/null 2>&1 || true; fi
  # Pick first available device if none booted
  if ! xcrun simctl list devices booted | grep -q Booted; then
    DEV_ID=$(xcrun simctl list devices available | awk '/iPhone/{print $NF}' | tr -d '()' | head -n1)
    if [ -n "$DEV_ID" ]; then
      xcrun simctl boot "$DEV_ID" >/dev/null 2>&1 || true
      xcrun simctl bootstatus "$DEV_ID" -b >/dev/null 2>&1 || true
    fi
  fi
  # Open the URL
  xcrun simctl openurl booted "$SIM_URL" >/dev/null 2>&1 || true
else
  # Fallback: open desktop browser (macOS)
  if command -v open >/dev/null 2>&1; then
    open "$URL"
  fi
fi

# Keep processes running
wait
