# GitPulse

## Project Overview

GitPulse is a full-stack developer analytics platform that syncs GitHub data,
precomputes daily snapshots, and serves a real-time dashboard.

This is a portfolio project. The goal is to demonstrate practical experience with:
OAuth, background jobs, resilient API design, relational databases, React,
automated testing, CI/CD, and Azure cloud deployment.

Resume-ready summary:
> "Built and deployed a full-stack developer analytics platform with a React
> dashboard and an ASP.NET Core backend. Engineered an Azure PostgreSQL-backed
> commit sync pipeline using Hangfire and Polly to handle GitHub API rate limits,
> with automated testing and CI/CD via GitHub Actions."

## Backend

**Stack:** ASP.NET Core 8 Web API, PostgreSQL, Hangfire, Polly, Serilog

**Testing:** xUnit + Moq — validates GitHub API parsing and snapshot math

### Authentication
- GitHub OAuth — user authenticates via GitHub
- API issues short-lived JWTs (15-30 min expiry) for frontend auth
- Refresh token rotation — new refresh token on each use, old one invalidated

### GitHub API Service Layer
- Fetches repos, commits, and languages for a given user
- Polly handles retry logic and 429 rate-limit responses from GitHub

### Database Schema (PostgreSQL)
Tables:
- `users`
- `repos`
- `commits`
- `daily_snapshots`
- `sync_log`

### Background Jobs (Hangfire)
- **Hourly sync** — finds stale users, syncs new commits since last pull, updates snapshots
- **Weekly purge** — deletes `sync_log` entries older than 30 days to keep the database lean

### Logging (Serilog)
- Structured logging for background job failures, API health, and rate-limit hits

### REST API Endpoints
- `GET /api/stats`
- `GET /api/commits`
- `GET /api/languages`
- `GET /api/repos`

### Key Architecture Decision
The `daily_snapshots` table precomputes aggregates (e.g. commits per day for the
last 90 days). The dashboard reads from snapshots so it loads instantly instead of
crunching raw commit data on every request.

## Frontend

**Stack:** React + TypeScript + Chart.js + Tailwind CSS

**State management:** TBD — candidates: React Context, Zustand, or Redux Toolkit

### Features
- Dashboard: heatmap, language breakdown bars, repo list, recent commits
- GitHub OAuth redirect handling
- All data reads from our API, never GitHub directly
- Mobile responsive

## Infrastructure & Deployment

- **Azure App Service** — hosts the ASP.NET Core API
- **Azure Database for PostgreSQL** — managed database
- **Cloudflare Pages** — frontend deploy, currently hosts `sardella.dev`
- **GitHub Actions** — on push to main: run xUnit tests, build, deploy to Azure
- **Custom domain** — target: `devdash.sardella.dev` or similar subdomain
- **Environment variables** — managed via Azure App Settings, no secrets in code

## Security

### Secrets Management
- **App secrets** (OAuth client ID/secret, DB connection string) → Azure App Settings (encrypted at rest)
- **User tokens** (GitHub OAuth tokens) → Azure Key Vault (per-user, auditable, rotatable)
- Never store tokens in PostgreSQL — keeps the DB attack surface limited to application data

### API Security
- JWT bearer tokens — issued after GitHub OAuth login, sent on every API request
- Short-lived JWTs (15-30 min expiry) to limit exposure if stolen
- Refresh token rotation — new token issued on each use, previous one invalidated
- HTTPS only — tokens never transmitted over plain HTTP
- Frontend stores JWT in memory only (not localStorage) — prevents XSS token theft

### Guardrails
- Frontend never calls GitHub API directly — always goes through our backend
- No secrets in code or git history
- Dashboard data always reads from snapshot table, never raw commit queries
