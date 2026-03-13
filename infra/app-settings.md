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
