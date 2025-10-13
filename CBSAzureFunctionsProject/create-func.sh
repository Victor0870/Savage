#!/usr/bin/env bash
set -euo pipefail

read -p "Enter Function App name (e.g. myfuncapp): " FUNCAPP_NAME
LOCATION="westus2"
RG="${FUNCAPP_NAME}-rg"
STORAGE="${FUNCAPP_NAME}storage"

echo "➜ Creating resource group $RG..."
az group create --name "$RG" --location "$LOCATION"

echo "➜ Creating storage account $STORAGE..."
az storage account create \
  --name "$STORAGE" \
  --resource-group "$RG" \
  --location "$LOCATION" \
  --sku Standard_LRS

echo "➜ Creating Function App $FUNCAPP_NAME (Consumption)..."
az functionapp create \
  --name "$FUNCAPP_NAME" \
  --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet \
  --runtime-version 8.0 \
  --functions-version 4

echo "✅ Done! Function App created: $FUNCAPP_NAME"
