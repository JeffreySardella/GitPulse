# infra/setup.sh
# Reference script — run these commands manually via Azure CLI or Portal
# Requires: az login

RESOURCE_GROUP="gitpulse-rg"
LOCATION="eastus"
APP_NAME="gitpulse-api"
DB_NAME="gitpulse-db"
KEYVAULT_NAME="gitpulse-kv"

# Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Azure Database for PostgreSQL Flexible Server
az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $DB_NAME \
  --location $LOCATION \
  --admin-user gitpulseadmin \
  --admin-password '<GENERATE_SECURE_PASSWORD>' \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16

# Create the database
az postgres flexible-server db create \
  --resource-group $RESOURCE_GROUP \
  --server-name $DB_NAME \
  --database-name gitpulse

# Azure App Service Plan + Web App
az appservice plan create \
  --name gitpulse-plan \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan gitpulse-plan \
  --name $APP_NAME \
  --runtime "DOTNETCORE:8.0"

# Enforce HTTPS only (no plain HTTP)
az webapp config set --name $APP_NAME --resource-group $RESOURCE_GROUP --https-only true

# Azure Key Vault
az keyvault create \
  --name $KEYVAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Grant App Service access to Key Vault
az webapp identity assign --name $APP_NAME --resource-group $RESOURCE_GROUP
PRINCIPAL_ID=$(az webapp identity show --name $APP_NAME --resource-group $RESOURCE_GROUP --query principalId -o tsv)
az keyvault set-policy \
  --name $KEYVAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list set
