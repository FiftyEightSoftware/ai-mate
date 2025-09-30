#!/usr/bin/env bash
set -euo pipefail

# Helper script to create a Google Cloud service account and JSON key
# for Gradle Play Publisher (GPP) uploads, and guide you to grant
# permissions in the Google Play Console.
#
# What this script does:
# - Verifies gcloud is installed and authenticated
# - Ensures the Android Publisher API is enabled
# - Creates (or reuses) a service account
# - Generates a JSON key locally
# - Saves the key to your chosen path (repo root by default)
# - Prints next steps to grant Play Console access
#
# What this script cannot do:
# - It cannot grant Play Console permissions automatically.
#   You must go to Google Play Console → Settings → API access and
#   "Grant access" to the created service account.
#
# Usage:
#   chmod +x scripts/setup_play_credentials.sh
#   ./scripts/setup_play_credentials.sh
#
# After running this script, run the deploy script:
#   ./scripts/deploy_play.sh

# Resolve repo root (this script lives in repo_root/scripts)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DEFAULT_OUTPUT_ROOT_JSON="$REPO_ROOT/play-service-account.json"
DEFAULT_OUTPUT_ANDROID_JSON="$REPO_ROOT/ai_mate_wrapper/android/play-service-account.json"

banner() {
  echo ""
  echo "==================================================================="
  echo "$1"
  echo "==================================================================="
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[!] Required command not found: $1"
    exit 1
  fi
}

banner "Pre-flight checks"
require_cmd gcloud

echo "Checking gcloud auth..."
if ! gcloud auth list --format="value(account)" | grep -q ".@"; then
  echo "You are not authenticated with gcloud. Running 'gcloud auth login'..."
  gcloud auth login
fi

echo ""
echo "=== GCP Project selection ==="
# Try current configured project
CURRENT_PROJ=$(gcloud config get-value project 2>/dev/null || true)
PROJ_LIST=$(gcloud projects list --format="value(projectId)" 2>/dev/null || true)

if [[ -n "$CURRENT_PROJ" && "$CURRENT_PROJ" != "(unset)" ]]; then
  echo "Detected current gcloud project: $CURRENT_PROJ"
  read -r -p "Use this project? [Y/n]: " USE_CUR
  USE_CUR=${USE_CUR:-Y}
  if [[ "$USE_CUR" =~ ^[Yy]$ ]]; then
    GCP_PROJECT="$CURRENT_PROJ"
  fi
fi

if [[ -z "${GCP_PROJECT:-}" ]]; then
  if [[ -n "$PROJ_LIST" ]]; then
    echo "Available projects:"
    IFS=$'\n' read -r -d '' -a PROJS < <(printf '%s\n' "$PROJ_LIST" && printf '\0')
    for i in "${!PROJS[@]}"; do
      printf "  %d) %s\n" "$((i+1))" "${PROJS[$i]}"
    done
    read -r -p "Select a project by number or press Enter to type manually: " SEL
    if [[ -n "$SEL" && "$SEL" =~ ^[0-9]+$ ]] && (( SEL >= 1 && SEL <= ${#PROJS[@]} )); then
      GCP_PROJECT="${PROJS[$((SEL-1))]}"
    fi
  fi
fi

if [[ -z "${GCP_PROJECT:-}" ]]; then
  read -r -p "Enter your GCP Project ID (e.g., my-company-123): " GCP_PROJECT
fi

if [[ -z "$GCP_PROJECT" || "$GCP_PROJECT" == *.* ]]; then
  echo "[!] Invalid Project ID. It must not contain dots and should be lowercase letters, digits, and hyphens."
  exit 1
fi

echo "Setting gcloud project to: $GCP_PROJECT"
gcloud config set project "$GCP_PROJECT" >/dev/null

banner "Enable Android Publisher API"
# Enable the Android Publisher API required by GPP
if gcloud services list --enabled --format="value(config.name)" | grep -q "androidpublisher.googleapis.com"; then
  echo "Android Publisher API already enabled."
else
  gcloud services enable androidpublisher.googleapis.com
fi

banner "Create or reuse a service account"
read -r -p "Service account name (e.g., play-publisher): " SA_NAME
if [[ -z "$SA_NAME" ]]; then
  SA_NAME="play-publisher"
fi
SA_EMAIL="$SA_NAME@$GCP_PROJECT.iam.gserviceaccount.com"

echo "Checking if service account exists: $SA_EMAIL"
if gcloud iam service-accounts list --format="value(email)" | grep -q "^$SA_EMAIL$"; then
  echo "Service account exists."
else
  echo "Creating service account..."
  gcloud iam service-accounts create "$SA_NAME" \
    --display-name "Play Publisher Service Account"
fi

banner "Generate JSON key"
# Choose output path
echo "Where should the JSON key be saved?"
echo "1) Repo root (recommended): $DEFAULT_OUTPUT_ROOT_JSON"
echo "2) Android module: $DEFAULT_OUTPUT_ANDROID_JSON"
echo "3) Custom absolute path"
read -r -p "Select [1/2/3]: " OUTPUT_CHOICE

KEY_PATH=""
case "$OUTPUT_CHOICE" in
  1|"") KEY_PATH="$DEFAULT_OUTPUT_ROOT_JSON" ;;
  2) KEY_PATH="$DEFAULT_OUTPUT_ANDROID_JSON" ;;
  3) read -r -p "Enter absolute path to save the JSON: " KEY_PATH ;;
  *) echo "Invalid choice" ; exit 1 ;;
esac

# Ensure directory exists
mkdir -p "$(dirname "$KEY_PATH")"

if [[ -f "$KEY_PATH" ]]; then
  echo "[!] Key already exists at: $KEY_PATH"
  read -r -p "Overwrite? [y/N]: " OW
  if [[ ! "$OW" =~ ^[Yy]$ ]]; then
    echo "Aborting to avoid overwrite."
    exit 1
  fi
  rm -f "$KEY_PATH"
fi

echo "Creating JSON key at: $KEY_PATH"
gcloud iam service-accounts keys create "$KEY_PATH" \
  --iam-account="$SA_EMAIL"

echo ""
banner "Grant access in Google Play Console"
cat <<EOF
Manual step required:
1) Open Google Play Console → Settings → API access
2) Under "Service accounts", find: $SA_EMAIL
3) Click "Grant access"
4) Assign permissions for your app (at minimum):
   - View app information
   - Upload app artifacts to tracks
   - Manage releases
   - Release to internal testing
5) Save

After that, run your deploy:
  ./scripts/deploy_play.sh
EOF

banner "Done"
echo "Service account JSON: $KEY_PATH"
echo "IMPORTANT: Do not commit this file to Git. It should be gitignored."
