# GitPulse

A full-stack developer analytics platform that syncs GitHub data, precomputes daily snapshots, and serves a real-time dashboard.

> Built and deployed a full-stack developer analytics platform with a React dashboard and an ASP.NET Core backend. Engineered an Azure PostgreSQL-backed commit sync pipeline using Hangfire and Polly to handle GitHub API rate limits, with automated testing and CI/CD via GitHub Actions.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 8, PostgreSQL, Hangfire, Polly, Serilog |
| Frontend | React 19, TypeScript, Vite, Tailwind CSS v4, Zustand, Chart.js |
| Testing | xUnit + Moq (backend), Vitest + React Testing Library (frontend) |
| Infrastructure | Azure App Service, Azure Key Vault, Cloudflare Pages, GitHub Actions |

## Features

- **GitHub OAuth** login with JWT access tokens and refresh token rotation
- **Dashboard** with commit activity chart, language breakdown, repository list, and recent commits
- **Background sync** — Hangfire hourly job fetches new commits from GitHub
- **Precomputed snapshots** — daily aggregates for instant dashboard loads
- **Resilient API calls** — Polly handles retries and GitHub 429 rate limits
- **Secure token storage** — Azure Key Vault for user GitHub tokens, in-memory JWTs on the frontend

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 16](https://www.postgresql.org/download/)
- A [GitHub OAuth App](https://github.com/settings/developers) (for authentication)

## Local Development Setup

### 1. Clone the repo

```bash
git clone https://github.com/JeffreySardella/GitPulse.git
cd GitPulse
```

### 2. Set up PostgreSQL

Create a local database:

```sql
CREATE DATABASE gitpulse;
```

### 3. Configure the backend

Edit `src/GitPulse.Api/appsettings.Development.json` with your local values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gitpulse;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "dev-secret-key-change-this-in-production-min32chars!",
    "Issuer": "gitpulse"
  },
  "GitHub": {
    "ClientId": "your-github-oauth-client-id",
    "ClientSecret": "your-github-oauth-client-secret"
  }
}
```

To get GitHub OAuth credentials:
1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Create a new OAuth App
3. Set **Homepage URL** to `http://localhost:5173`
4. Set **Authorization callback URL** to `http://localhost:5173/callback`

### 4. Run the backend

```bash
dotnet restore
dotnet run --project src/GitPulse.Api
```

The API starts at `http://localhost:5000`. EF Core migrations run automatically on startup.

### 5. Configure the frontend

```bash
cd client
cp .env.example .env
```

Edit `client/.env`:

```
VITE_GITHUB_CLIENT_ID=your-github-oauth-client-id
VITE_API_URL=http://localhost:5000
```

### 6. Run the frontend

```bash
cd client
npm install
npm run dev
```

The app opens at `http://localhost:5173`. The Vite dev server proxies `/api` requests to the backend.

## Running Tests

```bash
# Backend (23 tests)
dotnet test

# Frontend (14 tests)
cd client && npm run test:run
```

## Project Structure

```
GitPulse/
├── src/GitPulse.Api/
│   ├── Controllers/       # REST API endpoints
│   ├── Data/              # EF Core DbContext and converters
│   ├── Jobs/              # Hangfire background jobs
│   ├── Models/            # Entity models
│   ├── Services/          # Business logic (auth, sync, snapshots)
│   └── Program.cs         # App startup and DI configuration
├── tests/GitPulse.Api.Tests/
│   ├── Controllers/       # Controller integration tests
│   └── Services/          # Service unit tests
├── client/
│   ├── src/
│   │   ├── components/    # React UI components
│   │   ├── pages/         # Page-level components
│   │   ├── stores/        # Zustand state stores
│   │   ├── lib/           # API client and Chart.js setup
│   │   └── test/          # Frontend tests
│   └── vite.config.ts
├── infra/                 # Azure setup scripts and docs
└── .github/workflows/     # CI/CD pipelines
```

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/github` | No | Exchange GitHub OAuth code for JWT |
| POST | `/api/auth/refresh` | No | Rotate refresh token for new JWT |
| GET | `/api/stats` | Yes | 90-day commit stats and daily snapshots |
| GET | `/api/commits?limit=50` | Yes | Recent commits (limit 1-200) |
| GET | `/api/languages` | Yes | Language breakdown by repo count |
| GET | `/api/repos` | Yes | Repositories ordered by stars |

## Production Deployment

### Azure Resources

Run the commands in `infra/setup.sh` to create:
- Azure Resource Group
- Azure Database for PostgreSQL (Flexible Server)
- Azure App Service (Linux, .NET 8)
- Azure Key Vault

### App Configuration

See `infra/app-settings.md` for all required Azure App Settings.

### GitHub Actions Secrets

Add these to your GitHub repo (Settings > Secrets > Actions):

| Secret | Description |
|--------|-------------|
| `AZURE_PUBLISH_PROFILE` | Download from Azure App Service > Deployment Center |
| `CLOUDFLARE_API_TOKEN` | Cloudflare API token with Pages edit permissions |
| `CLOUDFLARE_ACCOUNT_ID` | Your Cloudflare account ID |

Add these as **Variables** (not secrets):

| Variable | Description |
|----------|-------------|
| `VITE_GITHUB_CLIENT_ID` | GitHub OAuth App client ID |
| `VITE_API_URL` | `https://gitpulse-api.azurewebsites.net` |

### CI/CD

GitHub Actions automatically:
- **On PR to main:** Runs tests (backend + frontend)
- **On push to main:** Runs tests, builds, and deploys backend to Azure App Service and frontend to Cloudflare Pages

## License

MIT
