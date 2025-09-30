#!/usr/bin/env bash
set -euo pipefail

# Create Azure resources for the backend on Azure Container Apps with Azure Files volume
# Usage:
#   chmod +x scripts/azure_create_aca.sh
#   ./scripts/azure_create_aca.sh \
#     --rg rg-ai-mate --region westeurope \
#     --app api-aimate \
#     --image ghcr.io/OWNER/REPO/backend:latest \
#     --origins https://YOUR-FRONTEND-DOMAIN \
#     [--invoice-min 300 --invoice-max 600 --job-min 120 --job-max 250]
#
# Prereqs: az CLI logged in and correct subscription set (az login; az account set --subscription ...)

RG=""
REGION="westeurope"
APP_NAME=""
IMAGE=""
ORIGINS=""
INVOICE_MIN="250"
INVOICE_MAX="450"
JOB_MIN="80"
JOB_MAX="200"
STORAGE_NAME=""
SHARE_NAME="aimate-db"
ENV_NAME="cae-aimate"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --rg) RG="$2"; shift 2;;
    --region) REGION="$2"; shift 2;;
    --app) APP_NAME="$2"; shift 2;;
    --image) IMAGE="$2"; shift 2;;
    --origins) ORIGINS="$2"; shift 2;;
    --invoice-min) INVOICE_MIN="$2"; shift 2;;
    --invoice-max) INVOICE_MAX="$2"; shift 2;;
    --job-min) JOB_MIN="$2"; shift 2;;
    --job-max) JOB_MAX="$2"; shift 2;;
    --storage) STORAGE_NAME="$2"; shift 2;;
    --share) SHARE_NAME="$2"; shift 2;;
    --env-name) ENV_NAME="$2"; shift 2;;
    *) echo "Unknown arg: $1"; exit 1;;
  esac
done

if [[ -z "$RG" || -z "$APP_NAME" || -z "$IMAGE" || -z "$ORIGINS" ]]; then
  echo "Missing required args. See script header for usage." >&2
  exit 1
fi

az group create -n "$RG" -l "$REGION" >/dev/null

if [[ -z "$STORAGE_NAME" ]]; then
  STORAGE_NAME="aimatestorage$RANDOM"
  az storage account create -g "$RG" -n "$STORAGE_NAME" -l "$REGION" --sku Standard_LRS >/dev/null
fi

SAKEY=$(az storage account keys list -g "$RG" -n "$STORAGE_NAME" --query "[0].value" -o tsv)
az storage share-rm create --resource-group "$RG" --storage-account "$STORAGE_NAME" --name "$SHARE_NAME" --quota 5 >/dev/null

# Create Container Apps environment if not exists
if ! az containerapp env show -g "$RG" -n "$ENV_NAME" >/dev/null 2>&1; then
  az containerapp env create -g "$RG" -n "$ENV_NAME" -l "$REGION" >/dev/null
fi

# Create Container App if not exists
if ! az containerapp show -g "$RG" -n "$APP_NAME" >/dev/null 2>&1; then
  az containerapp create -g "$RG" -n "$APP_NAME" \
    --environment "$ENV_NAME" \
    --image "$IMAGE" \
    --target-port 5000 --ingress external \
    --min-replicas 1 --max-replicas 1 \
    --env-vars \
      FRONTEND_ORIGINS="$ORIGINS" \
      INVOICE_SEED_MIN="$INVOICE_MIN" INVOICE_SEED_MAX="$INVOICE_MAX" \
      JOB_SEED_MIN="$JOB_MIN" JOB_SEED_MAX="$JOB_MAX" \
    --volume-mounts name=dbvol,path=/app/data \
    --azure-file-account-name "$STORAGE_NAME" \
    --azure-file-account-key "$SAKEY" \
    --azure-file-share-name "$SHARE_NAME" >/dev/null
else
  echo "Container App $APP_NAME already exists. Updating image and env..."
  az containerapp update -g "$RG" -n "$APP_NAME" --image "$IMAGE" >/dev/null
  az containerapp update -g "$RG" -n "$APP_NAME" --set-env-vars \
    FRONTEND_ORIGINS="$ORIGINS" \
    INVOICE_SEED_MIN="$INVOICE_MIN" INVOICE_SEED_MAX="$INVOICE_MAX" \
    JOB_SEED_MIN="$JOB_MIN" JOB_SEED_MAX="$JOB_MAX" >/dev/null
fi

FQDN=$(az containerapp show -g "$RG" -n "$APP_NAME" --query properties.configuration.ingress.fqdn -o tsv)
echo "Container App URL: https://$FQDN"
