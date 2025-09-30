#!/usr/bin/env bash
set -euo pipefail

# Interactive deployment script for Google Play (Internal testing) using Gradle Play Publisher
# - Prompts for (or detects) the service account JSON
# - Optionally bumps version in pubspec.yaml
# - Builds the Android App Bundle (.aab)
# - Publishes to the Internal testing track via Gradle Play Publisher
#
# Environment variable support (.env in repo root, gitignored):
#  - PLAY_SERVICE_ACCOUNT_JSON
#  - ANDROID_KEYSTORE_PATH (default ai_mate_wrapper/android/app/upload-keystore.jks)
#  - ANDROID_KEY_ALIAS (default upload)
#  - ANDROID_KEYSTORE_PASSWORD
#  - ANDROID_KEY_PASSWORD
#
# Prerequisites:
# - Flutter SDK installed and on PATH
# - Android SDK/NDK as required by Flutter
# - A valid upload keystore and android/key.properties (already wired in build.gradle.kts)
# - Gradle Play Publisher plugin configured (already wired in android/app/build.gradle.kts)
# - Release notes file exists at android/app/src/main/play/release-notes/en-GB/default.txt
#
# Usage:
#   chmod +x scripts/deploy_play.sh
#   ./scripts/deploy_play.sh

# Resolve repo root (this script lives in repo_root/scripts)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
FLUTTER_WRAPPER_DIR="$REPO_ROOT/ai_mate_wrapper"
ANDROID_DIR="$FLUTTER_WRAPPER_DIR/android"
APP_DIR="$ANDROID_DIR/app"
RELEASE_NOTES_FILE="$APP_DIR/src/main/play/release-notes/en-GB/default.txt"
AAB_OUT="$FLUTTER_WRAPPER_DIR/build/app/outputs/bundle/release/app-release.aab"

# Load .env if present
if [[ -f "$REPO_ROOT/.env" ]]; then
  # shellcheck disable=SC1090
  source "$REPO_ROOT/.env"
fi

# 1) Confirm release notes exist
if [[ ! -f "$RELEASE_NOTES_FILE" ]]; then
  echo "[!] Release notes file not found: $RELEASE_NOTES_FILE"
  echo "    Please create it (you can copy from RELEASE_NOTES.md Short section)."
  exit 1
fi

# 2) Prompt for service account JSON
DEFAULT_ROOT_CRED="$REPO_ROOT/play-service-account.json"
DEFAULT_ANDROID_CRED="$ANDROID_DIR/play-service-account.json"
CREDENTIALS_PATH="${PLAY_SERVICE_ACCOUNT_JSON:-}"

if [[ -z "$CREDENTIALS_PATH" && -f "$DEFAULT_ROOT_CRED" ]]; then
  CREDENTIALS_PATH="$DEFAULT_ROOT_CRED"
elif [[ -z "$CREDENTIALS_PATH" && -f "$DEFAULT_ANDROID_CRED" ]]; then
  CREDENTIALS_PATH="$DEFAULT_ANDROID_CRED"
fi

echo ""
echo "=== Google Play Publisher credentials ==="
if [[ -n "$CREDENTIALS_PATH" ]]; then
  echo "Detected credentials file: $CREDENTIALS_PATH"
  read -r -p "Use this credentials file? [Y/n]: " USE_DETECTED
  USE_DETECTED=${USE_DETECTED:-Y}
  if [[ "$USE_DETECTED" =~ ^[Yy]$ ]]; then
    : # keep detected
  else
    read -r -p "Enter absolute path to your service account JSON: " INPUT_PATH
    if [[ ! -f "$INPUT_PATH" ]]; then
      echo "[!] File not found: $INPUT_PATH"
      exit 1
    fi
    CREDENTIALS_PATH="$INPUT_PATH"
  fi
else
  echo "No credentials detected."
  read -r -p "Enter absolute path to your service account JSON: " INPUT_PATH
  if [[ ! -f "$INPUT_PATH" ]]; then
    echo "[!] File not found: $INPUT_PATH"
    exit 1
  fi
  CREDENTIALS_PATH="$INPUT_PATH"
fi

# Export for child processes
export PLAY_SERVICE_ACCOUNT_JSON="$CREDENTIALS_PATH"

# 3) Optional: bump version in pubspec.yaml
PUBSPEC="$FLUTTER_WRAPPER_DIR/pubspec.yaml"
if [[ ! -f "$PUBSPEC" ]]; then
  echo "[!] pubspec.yaml not found at $PUBSPEC"
  exit 1
fi
CURRENT_VERSION_LINE=$(grep -E '^version:\s*' "$PUBSPEC" || true)
CURRENT_VERSION_VALUE=${CURRENT_VERSION_LINE#version: }

echo ""
echo "=== Versioning ==="
echo "Current pubspec version: ${CURRENT_VERSION_VALUE:-<not found>}"
echo "Example format: 1.0.1+2 (left side is versionName, right side after + is versionCode)"
read -r -p "Enter new version (or leave blank to keep current): " NEW_VER
if [[ -n "$NEW_VER" ]]; then
  # Update the version line in pubspec.yaml
  # Create a backup first
  cp "$PUBSPEC" "$PUBSPEC.bak"
  if grep -qE '^version:\s*' "$PUBSPEC"; then
    sed -i'' -E "s/^version:\s*.*/version: $NEW_VER/" "$PUBSPEC"
  else
    echo "version: $NEW_VER" >> "$PUBSPEC"
  fi
  echo "Updated pubspec.yaml version to: $NEW_VER"
fi

# 4) Ensure android/key.properties exists (release signing) or generate from env/prompts
KEYPROPS="$ANDROID_DIR/key.properties"
echo ""
echo "=== Signing ==="

# Defaults for env-configurable values
ANDROID_KEYSTORE_PATH_DEFAULT="$APP_DIR/upload-keystore.jks"
ANDROID_KEY_ALIAS_DEFAULT="upload"

ANDROID_KEYSTORE_PATH="${ANDROID_KEYSTORE_PATH:-$ANDROID_KEYSTORE_PATH_DEFAULT}"
ANDROID_KEY_ALIAS="${ANDROID_KEY_ALIAS:-$ANDROID_KEY_ALIAS_DEFAULT}"
ANDROID_KEYSTORE_PASSWORD="${ANDROID_KEYSTORE_PASSWORD:-}"
ANDROID_KEY_PASSWORD="${ANDROID_KEY_PASSWORD:-}"

if [[ -z "$ANDROID_KEYSTORE_PASSWORD" ]]; then
  read -r -s -p "Enter keystore password: " ANDROID_KEYSTORE_PASSWORD; echo
fi
if [[ -z "$ANDROID_KEY_PASSWORD" ]]; then
  read -r -s -p "Enter key password (press Enter to reuse keystore password): " ANDROID_KEY_PASSWORD; echo
  if [[ -z "$ANDROID_KEY_PASSWORD" ]]; then ANDROID_KEY_PASSWORD="$ANDROID_KEYSTORE_PASSWORD"; fi
fi

echo "Using keystore: $ANDROID_KEYSTORE_PATH"
if [[ ! -f "$ANDROID_KEYSTORE_PATH" ]]; then
  echo "[!] Keystore not found at $ANDROID_KEYSTORE_PATH"
  echo "    Update ANDROID_KEYSTORE_PATH or place the keystore there."
  exit 1
fi

# Sync key.properties from env
cat > "$KEYPROPS" <<EOF
storeFile=${ANDROID_KEYSTORE_PATH#${APP_DIR}/}
storePassword=$ANDROID_KEYSTORE_PASSWORD
keyAlias=$ANDROID_KEY_ALIAS
keyPassword=$ANDROID_KEY_PASSWORD
EOF
echo "Synced signing config to: $KEYPROPS"

# 5) Build the AAB
cd "$FLUTTER_WRAPPER_DIR"
echo ""
echo "=== Flutter build (release App Bundle) ==="
flutter pub get
# Use a bigger temp dir if needed
export JAVA_TOOL_OPTIONS="-Djava.io.tmpdir=/tmp/build-tmp"
mkdir -p /tmp/build-tmp
flutter build appbundle --release

if [[ ! -f "$AAB_OUT" ]]; then
  echo "[!] Build did not produce expected AAB: $AAB_OUT"
  exit 1
fi

# 6) Publish to Internal testing via Gradle Play Publisher
cd "$ANDROID_DIR"
echo ""
echo "=== Publishing to Google Play (Internal testing) ==="
# Prefer credentials at repo root; override via -P if user provided absolute path different from defaults
if [[ "$CREDENTIALS_PATH" == "$DEFAULT_ROOT_CRED" || "$CREDENTIALS_PATH" == "$DEFAULT_ANDROID_CRED" ]]; then
  ./gradlew publishReleaseBundle
else
  ./gradlew publishReleaseBundle -Pplay.serviceAccountCredentials="$CREDENTIALS_PATH"
fi

echo ""
echo "Done. If successful, check Google Play Console → Testing → Internal testing for the new release."
