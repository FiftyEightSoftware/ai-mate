#!/usr/bin/env bash
set -euo pipefail

# Diagnostic helper for Android release signing
# Verifies key.properties, keystore path, alias presence, and password validity.
# Usage:
#   chmod +x scripts/check_signing.sh
#   ./scripts/check_signing.sh

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
FLUTTER_WRAPPER_DIR="$REPO_ROOT/ai_mate_wrapper"
ANDROID_DIR="$FLUTTER_WRAPPER_DIR/android"
APP_DIR="$ANDROID_DIR/app"
# Prefer repo-root key.properties, fallback to android/key.properties to mirror Gradle logic
if [[ -f "$REPO_ROOT/key.properties" ]]; then
  KEY_PROPS="$REPO_ROOT/key.properties"
else
  KEY_PROPS="$ANDROID_DIR/key.properties"
fi

banner() {
  echo ""
  echo "==================================================================="
  echo "$1"
  echo "==================================================================="
}

fail() {
  echo "[!] $1" >&2
  exit 1
}

banner "Locate key.properties"
if [[ ! -f "$KEY_PROPS" ]]; then
  fail "Missing $KEY_PROPS. Create it with storeFile/storePassword/keyAlias/keyPassword."
fi

# Read key.properties
STORE_FILE=""
STORE_PASSWORD=""
KEY_ALIAS=""
KEY_PASSWORD=""

# shellcheck disable=SC2162
while IFS='=' read -r k v; do
  k_trim=$(echo "$k" | sed 's/[[:space:]]//g')
  v_trim=$(echo "$v" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
  case "$k_trim" in
    storeFile) STORE_FILE="$v_trim" ;;
    storePassword) STORE_PASSWORD="$v_trim" ;;
    keyAlias) KEY_ALIAS="$v_trim" ;;
    keyPassword) KEY_PASSWORD="$v_trim" ;;
  esac
done < <(grep -v '^#' "$KEY_PROPS" | grep -E '^(storeFile|storePassword|keyAlias|keyPassword)\s*=')

if [[ -z "$STORE_FILE" || -z "$KEY_ALIAS" ]]; then
  fail "key.properties is missing required fields. Ensure storeFile and keyAlias are set."
fi

# Resolve keystore path robustly: try absolute, then several relative bases
resolve_keystore_path() {
  local path="$1"
  local base
  base="$(basename "$path")"
  # Absolute
  if [[ "$path" == /* ]]; then
    [[ -f "$path" ]] && echo "$path" && return 0
  else
    # As-is relative to repo root (when script is run from repo root)
    local as_is="$REPO_ROOT/$path"
    [[ -f "$as_is" ]] && echo "$as_is" && return 0

    # Relative to android/ directory
    local rel_android="$ANDROID_DIR/$path"
    [[ -f "$rel_android" ]] && echo "$rel_android" && return 0

    # Relative to app/ directory with original path
    local rel_app="$APP_DIR/$path"
    [[ -f "$rel_app" ]] && echo "$rel_app" && return 0

    # Relative to app/ directory using just the basename
    local rel_app_base="$APP_DIR/$base"
    [[ -f "$rel_app_base" ]] && echo "$rel_app_base" && return 0
  fi
  # Fallback: return original path (may not exist)
  echo "$path"
}

KEYSTORE_PATH="$(resolve_keystore_path "$STORE_FILE")"

banner "Validate keystore path"
echo "Resolved keystore path: $KEYSTORE_PATH"
if [[ ! -f "$KEYSTORE_PATH" ]]; then
  fail "Keystore not found at: $KEYSTORE_PATH"
fi

banner "List aliases in keystore"
# Try non-interactive listing using storePassword, fallback to interactive if missing/incorrect
if [[ -n "${STORE_PASSWORD}" ]]; then
  if keytool -list -v -keystore "$KEYSTORE_PATH" -storepass "$STORE_PASSWORD" >/tmp/keystore_aliases.$$ 2>/dev/null; then
    echo "Aliases present (non-interactive):"
    grep -E '^Alias name:' /tmp/keystore_aliases.$$ || true
    rm -f /tmp/keystore_aliases.$$ || true
  else
    echo "[!] Non-interactive alias listing failed (password may be wrong)."
    echo "    Falling back to interactive prompt..."
    keytool -list -v -keystore "$KEYSTORE_PATH"
  fi
else
  echo "Note: storePassword not provided in key.properties; keytool will prompt."
  keytool -list -v -keystore "$KEYSTORE_PATH"
fi

banner "Verify specific alias"
echo "Checking alias: $KEY_ALIAS"
if [[ -n "${STORE_PASSWORD}" ]]; then
  if keytool -list -v -keystore "$KEYSTORE_PATH" -alias "$KEY_ALIAS" -storepass "$STORE_PASSWORD"; then
    echo "[OK] Alias exists and keystore password accepted."
  else
    echo "[!] Alias '$KEY_ALIAS' not found or password incorrect."
    echo "    - If alias is different, update keyAlias in $KEY_PROPS"
    echo "    - If password is incorrect, update storePassword/keyPassword in $KEY_PROPS"
    exit 2
  fi
else
  echo "Note: storePassword not provided; keytool will prompt."
  if keytool -list -v -keystore "$KEYSTORE_PATH" -alias "$KEY_ALIAS"; then
    echo "[OK] Alias exists and keystore password accepted."
  else
    echo "[!] Alias '$KEY_ALIAS' not found or password incorrect."
    echo "    - If alias is different, update keyAlias in $KEY_PROPS"
    echo "    - If password is incorrect, update storePassword/keyPassword in $KEY_PROPS"
    exit 2
  fi
fi

banner "Summary"
echo "key.properties: $KEY_PROPS"
echo "storeFile: $STORE_FILE"
echo "resolved keystore: $KEYSTORE_PATH"
echo "keyAlias: $KEY_ALIAS"
echo "If you saw certificate details above, credentials are correct."
echo "Next: build with 'flutter build appbundle --release' and publish with Gradle Play Publisher."
