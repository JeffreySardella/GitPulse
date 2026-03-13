# GitPulse Deployment Guide

Step-by-step guide to deploy GitPulse to production. Total time: ~30-45 minutes.

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- [Cloudflare account](https://dash.cloudflare.com/sign-up) (free tier works)
- [GitHub CLI](https://cli.github.com/) installed (optional, for adding secrets via terminal)
- A domain on Cloudflare (e.g., `sardella.dev`) — or use the free `*.pages.dev` subdomain

---

## Step 1: Create a GitHub OAuth App

1. Go to https://github.com/settings/developers
2. Click **New OAuth App**
3. Fill in:
   - **Application name:** `GitPulse`
   - **Homepage URL:** `https://devdash.sardella.dev` (or your domain)
   - **Authorization callback URL:** `https://devdash.sardella.dev/callback`
4. Click **Register application**
5. Copy the **Client ID**
6. Click **Generate a new client secret** and copy it

Save both values — you'll need them in Steps 3 and 6.

---

## Step 2: Create Azure Resources

Login to Azure CLI:

```bash
az login
```

Then run these commands (or copy from `infra/setup.sh`):

```bash
# Variables
RESOURCE_GROUP="gitpulse-rg"
LOCATION="eastus"
APP_NAME="gitpulse-api"
DB_NAME="gitpulse-db"
KEYVAULT_NAME="gitpulse-kv"

# Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# PostgreSQL Flexible Server
az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $DB_NAME \
  --location $LOCATION \
  --admin-user gitpulseadmin \
  --admin-password '<MAKE_A_SECURE_PASSWORD>' \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16

# Create the database
az postgres flexible-server db create \
  --resource-group $RESOURCE_GROUP \
  --server-name $DB_NAME \
  --database-name gitpulse

# App Service Plan (Linux)
az appservice plan create \
  --name gitpulse-plan \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Web App
az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan gitpulse-plan \
  --name $APP_NAME \
  --runtime "DOTNETCORE:8.0"

# Force HTTPS
az webapp config set --name $APP_NAME --resource-group $RESOURCE_GROUP --https-only true

# Key Vault
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
```

---

## Step 3: Configure Azure App Settings

Replace the placeholder values and run:

```bash
az webapp config appsettings set --name gitpulse-api --resource-group gitpulse-rg --settings \
  Jwt__Secret="<GENERATE_A_RANDOM_32+_CHAR_STRING>" \
  Jwt__Issuer="gitpulse" \
  GitHub__ClientId="<YOUR_GITHUB_CLIENT_ID>" \
  GitHub__ClientSecret="<YOUR_GITHUB_CLIENT_SECRET>" \
  KeyVault__Uri="https://gitpulse-kv.vault.azure.net/"

az webapp config connection-string set --name gitpulse-api --resource-group gitpulse-rg \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection="Host=gitpulse-db.postgres.database.azure.com;Database=gitpulse;Username=gitpulseadmin;Password=<YOUR_DB_PASSWORD>;SSL Mode=Require"
```

To generate a JWT secret, you can use:

```bash
openssl rand -base64 32
```

---

## Step 4: Run the Initial Database Migration

Allow your IP through the Azure PostgreSQL firewall:

```bash
MY_IP=$(curl -s ifconfig.me)
az postgres flexible-server firewall-rule create \
  --resource-group gitpulse-rg \
  --name gitpulse-db \
  --rule-name AllowMyIP \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP
```

Run the migration:

```bash
cd src/GitPulse.Api
dotnet ef database update --connection "Host=gitpulse-db.postgres.database.azure.com;Database=gitpulse;Username=gitpulseadmin;Password=<YOUR_DB_PASSWORD>;SSL Mode=Require"
```

Remove the firewall rule:

```bash
az postgres flexible-server firewall-rule delete \
  --resource-group gitpulse-rg \
  --name gitpulse-db \
  --rule-name AllowMyIP --yes
```

> Note: After this initial migration, the app auto-migrates on startup so you won't need to do this again.

---

## Step 5: Get Azure Publish Profile

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **App Services** > **gitpulse-api**
3. Click **Deployment Center** in the left sidebar
4. Click **Manage publish profile** > **Download publish profile**
5. Open the downloaded `.PublishSettings` file and copy the entire XML content

---

## Step 6: Create Cloudflare Pages Project

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com)
2. Click **Workers & Pages** > **Create** > **Pages** > **Direct Upload**
3. Name the project `gitpulse`
4. Upload any placeholder file (the real deploy happens via GitHub Actions)
5. **(Optional)** Add a custom domain:
   - Go to the project > **Custom domains** > Add `devdash.sardella.dev`
   - If your domain is already on Cloudflare, the DNS record is auto-created

### Create a Cloudflare API Token

1. Go to **My Profile** > **API Tokens** > **Create Token**
2. Use the **Custom token** template
3. Permissions: **Account** > **Cloudflare Pages** > **Edit**
4. Click **Continue to summary** > **Create Token**
5. Copy the token

Also note your **Account ID** from the Cloudflare dashboard sidebar.

---

## Step 7: Add GitHub Secrets and Variables

Go to your GitHub repo > **Settings** > **Secrets and variables** > **Actions**.

### Secrets (Settings > Secrets > New repository secret)

| Secret Name | Value |
|-------------|-------|
| `AZURE_PUBLISH_PROFILE` | The entire XML from Step 5 |
| `CLOUDFLARE_API_TOKEN` | The token from Step 6 |
| `CLOUDFLARE_ACCOUNT_ID` | Your Cloudflare account ID |

### Variables (Settings > Variables > New repository variable)

| Variable Name | Value |
|---------------|-------|
| `VITE_GITHUB_CLIENT_ID` | Your GitHub OAuth App client ID from Step 1 |
| `VITE_API_URL` | `https://gitpulse-api.azurewebsites.net` |

---

## Step 8: Deploy

Push to main (or re-run the failed workflows):

```bash
git push origin main
```

GitHub Actions will:
1. Run backend tests > build > deploy to Azure App Service
2. Run frontend tests > build > deploy to Cloudflare Pages

Watch the progress at: `https://github.com/JeffreySardella/GitPulse/actions`

---

## Step 9: Verify

1. Visit `https://gitpulse-api.azurewebsites.net` — should return a 404 (no root route, that's expected)
2. Visit your frontend URL (`https://devdash.sardella.dev` or `https://gitpulse.pages.dev`)
3. Click **Sign in with GitHub**
4. Authorize the app
5. You should land on the dashboard

### If the OAuth redirect fails

Make sure the GitHub OAuth App callback URL (Step 1) matches your actual frontend URL. Update it at https://github.com/settings/developers if needed.

---

## Cost Estimate

| Resource | Tier | Monthly Cost |
|----------|------|-------------|
| Azure App Service | B1 | ~$13 |
| Azure PostgreSQL Flexible | Burstable B1ms | ~$15 |
| Azure Key Vault | Standard | < $1 |
| Cloudflare Pages | Free | $0 |
| **Total** | | **~$29/mo** |

You can reduce costs by using the Azure Free Tier (F1 App Service) for testing, though it has limitations (no custom domain, 60 min/day compute).

---

## Teardown

To avoid charges when you're done:

```bash
az group delete --name gitpulse-rg --yes --no-wait
```

This deletes all Azure resources in the group (App Service, PostgreSQL, Key Vault).
