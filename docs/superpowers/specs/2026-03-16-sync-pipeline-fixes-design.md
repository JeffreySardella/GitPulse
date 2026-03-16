# Sync Pipeline Fixes — Design Spec

**Date:** 2026-03-16
**Goal:** Fix data gaps in the GitPulse sync pipeline and deploy the application so the project is portfolio-ready for job applications.

## Scope

Three workstreams:
1. Populate commit LOC metrics (Additions/Deletions) during sync
2. Surface LOC data through the API and frontend dashboard
3. Deploy backend to Azure and frontend to Cloudflare Pages

Out of scope:
- Language byte breakdown (current repo-count-based chart is sufficient)
- GraphQL API integration
- Mobile-specific responsive testing

---

## 1. Backend — Commit Stats Fetching

### 1a. New method: `GitHubDataService.GetCommitDetailAsync`

- Signature: `GetCommitDetailAsync(string accessToken, string repoFullName, string sha)`
  - Follows the existing convention — `repoFullName` is `"owner/repo"` format, same as `GetCommitsSinceAsync` and `GetLanguagesAsync`
- Calls `GET /repos/{repoFullName}/commits/{sha}`
- Parses `stats.additions` and `stats.deletions` from the response
- Returns a new record `GitHubCommitStats(int Additions, int Deletions)`
- Add the method signature to `IGitHubDataService`
- Uses existing `CreateRequest` helper for auth and User-Agent headers
- Polly retry policy already covers this HttpClient — no additional config needed

**GitHub API response shape** (relevant fields only):
```json
{
  "sha": "abc123",
  "stats": {
    "total": 10,
    "additions": 7,
    "deletions": 3
  }
}
```

### 1b. Wire into `SyncService.SyncUserAsync`

- For each new commit (not in `existingShas`), call `GetCommitDetailAsync(token, repoData.FullName, commitData.Sha)` before `_db.Commits.Add(...)`
- Populate `commit.Additions` and `commit.Deletions` from the result
- Only fetches stats for **new** commits, so re-syncs don't refetch
- **Graceful degradation:** Wrap each `GetCommitDetailAsync` call in a try/catch — if it fails (429, 5xx, timeout), log a warning and leave Additions/Deletions as 0. Do NOT fail the entire sync over a missing stat. The commit still gets saved.
- **First-sync guard:** When `user.LastSyncedAt == DateTime.MinValue` (first sync), override `since` to 90 days ago. This matches the snapshot window and prevents fetching thousands of historical commits + their detail calls. Cap keeps first-sync API calls reasonable.
- Polly handles transient retries; graceful degradation handles exhaustion

### 1c. Aggregate LOC in `SnapshotService.UpdateSnapshotsForUserAsync`

- Extend the initial `.Select()` to include `c.Additions` and `c.Deletions`
- In the `GroupBy` aggregation, add:
  - `LinesAdded = g.Sum(c => c.Additions)`
  - `LinesDeleted = g.Sum(c => c.Deletions)`
- **Update path:** Set `existing.LinesAdded` and `existing.LinesDeleted` on existing snapshots
- **Create path:** Set `LinesAdded` and `LinesDeleted` on new `DailySnapshot` entities

**No database migration required** — `Commits.Additions`, `Commits.Deletions`, `DailySnapshots.LinesAdded`, and `DailySnapshots.LinesDeleted` columns already exist in the initial migration schema.

---

## 2. API & Frontend — Surfacing LOC Metrics

### 2a. `StatsController.GetStats`

- Extend the per-snapshot `.Select()` projection to include `s.LinesAdded` and `s.LinesDeleted` (so each snapshot in the array has LOC data)
- Add `TotalLinesAdded` and `TotalLinesDeleted` (summed from snapshots) as top-level fields in the response object

### 2b. Frontend `statsStore`

- Add `totalLinesAdded: number` and `totalLinesDeleted: number` to `StatsState`
- Map from API response in `fetchStats`
- Add `linesAdded` and `linesDeleted` to the `DailySnapshot` interface

### 2c. Frontend `SummaryCards`

- Replace the "Total Stars" card with "Lines Changed" showing `totalLinesAdded + totalLinesDeleted`
- Keeps the 3-card grid layout

---

## 3. Documentation & Deployment

### 3a. CLAUDE.md

- Update state management line from "TBD" to "Zustand"

### 3b. Azure Deployment (Backend)

- Provision using existing `infra/setup.sh` as reference:
  - Resource Group, PostgreSQL Flexible Server, App Service Plan, Web App, Key Vault
- Configure Azure App Settings:
  - `ConnectionStrings__DefaultConnection` (PostgreSQL)
  - `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`
  - `GitHub__ClientId`, `GitHub__ClientSecret`
- Add `AZURE_PUBLISH_PROFILE` secret to GitHub repo for CI/CD
- Backend workflow (`.github/workflows/backend.yml`) handles build + deploy

### 3c. Cloudflare Pages Deployment (Frontend)

- Add GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`
- Set `VITE_GITHUB_CLIENT_ID` and `VITE_API_URL` in workflow environment
- Frontend workflow (`.github/workflows/frontend.yml`) handles build + deploy

### 3d. Deployment Note

Cloud provisioning and secret configuration requires the user's Azure/Cloudflare accounts. Implementation will prepare exact commands and guide the user through execution.

---

## Files Modified

| File | Change |
|------|--------|
| `src/GitPulse.Api/Services/IGitHubDataService.cs` | Add `GetCommitDetailAsync` signature and `GitHubCommitStats` record |
| `src/GitPulse.Api/Services/GitHubDataService.cs` | Implement `GetCommitDetailAsync`, add `CommitDetailResponse` record |
| `src/GitPulse.Api/Services/SyncService.cs` | Call `GetCommitDetailAsync` for new commits, populate Additions/Deletions |
| `src/GitPulse.Api/Services/SnapshotService.cs` | Aggregate LinesAdded/LinesDeleted in snapshot computation |
| `src/GitPulse.Api/Controllers/StatsController.cs` | Include LOC fields in stats response |
| `client/src/stores/statsStore.ts` | Add LOC fields to state and fetch mapping |
| `client/src/components/SummaryCards.tsx` | Replace "Total Stars" with "Lines Changed" |
| `CLAUDE.md` | Update state management from "TBD" to "Zustand" |

## Tests to Update

| File | Change |
|------|--------|
| `tests/GitPulse.Api.Tests/Services/GitHubDataServiceTests.cs` | Add test for `GetCommitDetailAsync` |
| `tests/GitPulse.Api.Tests/Services/SyncServiceTests.cs` | Update mocks to include `GetCommitDetailAsync` calls |
| `tests/GitPulse.Api.Tests/Services/SnapshotServiceTests.cs` | Verify LOC aggregation in snapshots |
| `tests/GitPulse.Api.Tests/Controllers/StatsControllerTests.cs` | Verify LOC fields in response |
| `client/src/test/components/SummaryCards.test.tsx` | Update `useStatsStore.setState` to include `totalLinesAdded`/`totalLinesDeleted`. Remove `useReposStore` mock (Stars card removed). Assert "Lines Changed" card. |
