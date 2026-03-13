# GitPulse Infrastructure & CI/CD Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deploy GitPulse to production — Azure App Service for the API, Azure Database for PostgreSQL, Cloudflare Pages for the frontend, GitHub Actions for CI/CD, and Azure Key Vault for secrets.

**Architecture:** GitHub Actions pipeline runs tests and deploys on push to main. Backend deploys to Azure App Service via publish profile. Frontend deploys to Cloudflare Pages via Wrangler. Azure Key Vault stores user GitHub tokens. App Settings stores app-level secrets.

**Tech Stack:** Azure App Service, Azure Database for PostgreSQL, Azure Key Vault, Cloudflare Pages, GitHub Actions, Wrangler CLI

**Depends on:** Backend and Frontend plans must be completed first.

---

## Chunk 1: Azure Resources Setup

### Task 1: Create Azure Resources

**Files:**
- Create: `infra/setup.sh` (reference script, not automated — documents the Azure CLI commands)

- [ ] **Step 1: Document Azure resource creation script**

```bash
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
```

- [ ] **Step 2: Run the commands (manually via Azure CLI or Portal)**

Expected: All resources created in Azure Portal

- [ ] **Step 3: Commit**

```bash
git add infra/
git commit -m "docs: add Azure resource setup reference script"
```

---

### Task 2: Configure Azure App Settings

**Files:**
- Create: `infra/app-settings.md` (documents what settings to configure — never contains actual secrets)

- [ ] **Step 1: Document required App Settings**

```markdown
# infra/app-settings.md
# Required Azure App Settings for gitpulse-api

## Connection Strings
- `ConnectionStrings__DefaultConnection` — PostgreSQL connection string

## JWT
- `Jwt__Secret` — 32+ character secret for signing JWTs
- `Jwt__Issuer` — "gitpulse"

## GitHub OAuth
- `GitHub__ClientId` — GitHub OAuth App client ID
- `GitHub__ClientSecret` — GitHub OAuth App client secret

## Azure Key Vault
- `KeyVault__Uri` — https://gitpulse-kv.vault.azure.net/

## Set via Azure CLI:
az webapp config appsettings set --name gitpulse-api --resource-group gitpulse-rg --settings \
  Jwt__Secret="<SECRET>" \
  Jwt__Issuer="gitpulse" \
  GitHub__ClientId="<CLIENT_ID>" \
  GitHub__ClientSecret="<CLIENT_SECRET>" \
  KeyVault__Uri="https://gitpulse-kv.vault.azure.net/"

az webapp config connection-string set --name gitpulse-api --resource-group gitpulse-rg \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection="Host=gitpulse-db.postgres.database.azure.com;Database=gitpulse;Username=gitpulseadmin;Password=<PASSWORD>;SSL Mode=Require"
```

- [ ] **Step 2: Configure settings in Azure Portal or CLI**

Expected: All settings configured, no secrets in code

- [ ] **Step 3: Commit**

```bash
git add infra/app-settings.md
git commit -m "docs: add Azure App Settings reference for production configuration"
```

---

### Task 3: Run EF Core Migration on Azure PostgreSQL

- [ ] **Step 1: Allow your IP through Azure PostgreSQL firewall**

```bash
az postgres flexible-server firewall-rule create \
  --resource-group gitpulse-rg \
  --name gitpulse-db \
  --rule-name AllowMyIP \
  --start-ip-address <YOUR_IP> \
  --end-ip-address <YOUR_IP>
```

- [ ] **Step 2: Run migrations against Azure database**

```bash
cd src/GitPulse.Api
dotnet ef database update --connection "Host=gitpulse-db.postgres.database.azure.com;Database=gitpulse;Username=gitpulseadmin;Password=<PASSWORD>;SSL Mode=Require"
```

Expected: Migration applied successfully

- [ ] **Step 3: Remove firewall rule**

```bash
az postgres flexible-server firewall-rule delete \
  --resource-group gitpulse-rg \
  --name gitpulse-db \
  --rule-name AllowMyIP --yes
```

- [ ] **Step 4: Configure startup migration**

For this project, pending EF Core migrations run automatically at app startup.
Add to `Program.cs` (after `app.Build()`, before middleware):

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GitPulseDbContext>();
    db.Database.Migrate();
}
```

This ensures schema changes deploy alongside app code automatically. Acceptable for a single-instance portfolio project.

---

## Chunk 2: CI/CD Pipeline

### Task 4: GitHub Actions — Backend CI/CD

**Files:**
- Create: `.github/workflows/backend.yml`

- [ ] **Step 1: Write backend workflow**

```yaml
# .github/workflows/backend.yml
name: Backend CI/CD

on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - 'tests/**'
      - '*.sln'
  pull_request:
    branches: [main]
    paths:
      - 'src/**'
      - 'tests/**'
      - '*.sln'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal

      - name: Publish
        if: github.ref == 'refs/heads/main' && github.event_name == 'push'
        run: dotnet publish src/GitPulse.Api/GitPulse.Api.csproj -c Release -o ./publish

      - name: Upload artifact
        if: github.ref == 'refs/heads/main' && github.event_name == 'push'
        uses: actions/upload-artifact@v4
        with:
          name: api-publish
          path: ./publish

  deploy:
    needs: test
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    runs-on: ubuntu-latest
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: api-publish
          path: ./publish

      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: gitpulse-api
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: ./publish
```

- [ ] **Step 2: Get Azure Publish Profile**

In Azure Portal: App Service > gitpulse-api > Deployment Center > Manage publish profile > Download

- [ ] **Step 3: Add publish profile as GitHub Secret**

GitHub repo > Settings > Secrets > Actions > New secret:
- Name: `AZURE_PUBLISH_PROFILE`
- Value: contents of the downloaded publish profile XML

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/backend.yml
git commit -m "ci: add GitHub Actions workflow for backend test and deploy to Azure"
```

---

### Task 5: GitHub Actions — Frontend CI/CD (Cloudflare Pages)

**Files:**
- Create: `.github/workflows/frontend.yml`

- [ ] **Step 1: Write frontend workflow**

```yaml
# .github/workflows/frontend.yml
name: Frontend CI/CD

on:
  push:
    branches: [main]
    paths:
      - 'client/**'
  pull_request:
    branches: [main]
    paths:
      - 'client/**'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: client/package-lock.json

      - name: Install dependencies
        run: cd client && npm ci

      - name: Run tests
        run: cd client && npm run test:run

      - name: Build
        run: cd client && npm run build
        env:
          VITE_GITHUB_CLIENT_ID: ${{ vars.VITE_GITHUB_CLIENT_ID }}
          VITE_API_URL: ${{ vars.VITE_API_URL }}

  deploy:
    needs: test
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: client/package-lock.json

      - name: Install dependencies
        run: cd client && npm ci

      - name: Build
        run: cd client && npm run build
        env:
          VITE_GITHUB_CLIENT_ID: ${{ vars.VITE_GITHUB_CLIENT_ID }}
          VITE_API_URL: ${{ vars.VITE_API_URL }}

      - name: Deploy to Cloudflare Pages
        uses: cloudflare/wrangler-action@v3
        with:
          apiToken: ${{ secrets.CLOUDFLARE_API_TOKEN }}
          accountId: ${{ secrets.CLOUDFLARE_ACCOUNT_ID }}
          command: pages deploy client/dist --project-name=gitpulse
```

- [ ] **Step 2: Create Cloudflare Pages project**

```bash
npx wrangler pages project create gitpulse --production-branch main
```

- [ ] **Step 3: Add GitHub Secrets**

GitHub repo > Settings > Secrets > Actions > New secrets:
- `CLOUDFLARE_API_TOKEN` — from Cloudflare dashboard (API Tokens > Create > Edit Cloudflare Pages)
- `CLOUDFLARE_ACCOUNT_ID` — from Cloudflare dashboard

GitHub repo > Settings > Variables > Actions > New variables (these are public, not secrets):
- `VITE_GITHUB_CLIENT_ID` — your GitHub OAuth App client ID
- `VITE_API_URL` — `https://gitpulse-api.azurewebsites.net`

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/frontend.yml
git commit -m "ci: add GitHub Actions workflow for frontend test and deploy to Cloudflare Pages"
```

---

## Chunk 3: Custom Domain + Final Wiring

### Task 6: Cloudflare Pages Custom Domain

- [ ] **Step 1: Add custom domain in Cloudflare Pages**

Cloudflare Dashboard > Pages > gitpulse > Custom domains > Add:
- `devdash.sardella.dev`

Since you already own `sardella.dev` on Cloudflare, the DNS record is auto-created.

- [ ] **Step 2: Verify domain resolves**

```bash
curl -I https://devdash.sardella.dev
```
Expected: 200 OK (or redirect to the app)

---

### Task 7: CORS Configuration for API

**Files:**
- Modify: `src/GitPulse.Api/Program.cs`

- [ ] **Step 1: Add CORS to Program.cs**

```csharp
// In service registration:
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://devdash.sardella.dev",
                "http://localhost:5173" // dev
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// In middleware pipeline (before UseAuthentication):
app.UseCors();
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add src/GitPulse.Api/Program.cs
git commit -m "feat: add CORS configuration for Cloudflare Pages domain"
```

---

### Task 8: Update GitHub OAuth App Redirect

- [ ] **Step 1: Update GitHub OAuth App settings**

GitHub > Settings > Developer settings > OAuth Apps > your app:
- Homepage URL: `https://devdash.sardella.dev`
- Authorization callback URL: `https://devdash.sardella.dev/callback`

---

### Task 9: End-to-End Smoke Test

- [ ] **Step 1: Push to main and verify CI/CD**

```bash
git push origin main
```

Watch GitHub Actions — both workflows should:
- Run tests
- Build
- Deploy

- [ ] **Step 2: Test the full OAuth flow**

1. Navigate to `https://devdash.sardella.dev`
2. Click "Sign in with GitHub"
3. Authorize the app
4. Verify redirect to `/dashboard`
5. Verify data loads (stats, commits, languages, repos)

- [ ] **Step 3: Verify API health**

```bash
curl https://gitpulse-api.azurewebsites.net/api/stats -H "Authorization: Bearer <token>"
```
Expected: 200 with JSON response

- [ ] **Step 4: Verify Hangfire dashboard (authenticated)**

Navigate to `https://gitpulse-api.azurewebsites.net/hangfire`
Expected: Requires authentication (HangfireAuthorizationFilter configured in backend plan Task 11), shows job schedules

---

## Summary

| Chunk | Tasks | What it delivers |
|-------|-------|-----------------|
| 1 | 1-3 | Azure resources (App Service, PostgreSQL, Key Vault), App Settings, migrations |
| 2 | 4-5 | GitHub Actions CI/CD for backend (Azure) and frontend (Cloudflare Pages) |
| 3 | 6-9 | Custom domain, CORS, OAuth redirect, end-to-end smoke test |
