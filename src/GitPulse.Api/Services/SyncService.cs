using GitPulse.Api.Data;
using GitPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Services;

public class SyncService : ISyncService
{
    private readonly GitPulseDbContext _db;
    private readonly IGitHubDataService _github;
    private readonly ISecretStore _secretStore;
    private readonly ISnapshotService _snapshotService;

    public SyncService(GitPulseDbContext db, IGitHubDataService github, ISecretStore secretStore, ISnapshotService snapshotService)
    {
        _db = db;
        _github = github;
        _secretStore = secretStore;
        _snapshotService = snapshotService;
    }

    public async Task SyncUserAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var token = await _secretStore.GetTokenAsync(userId)
            ?? throw new InvalidOperationException($"No GitHub token for user {userId}");

        var syncLog = new SyncLog
        {
            UserId = userId,
            Status = "running",
            StartedAt = DateTime.UtcNow
        };
        _db.SyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();

        try
        {
            var repos = await _github.GetReposAsync(token);

            foreach (var repoData in repos)
            {
                var ghId = long.Parse(repoData.Id);
                var repo = await _db.Repos.FirstOrDefaultAsync(r => r.GitHubId == ghId);
                if (repo is null)
                {
                    repo = new Repo
                    {
                        UserId = userId,
                        GitHubId = ghId,
                        Name = repoData.Name,
                        FullName = repoData.FullName,
                        Language = repoData.Language,
                        Stars = repoData.Stars
                    };
                    _db.Repos.Add(repo);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    repo.Stars = repoData.Stars;
                    repo.Language = repoData.Language;
                    repo.FullName = repoData.FullName;
                }

                var commits = await _github.GetCommitsSinceAsync(token, repoData.FullName, user.LastSyncedAt);
                foreach (var commitData in commits)
                {
                    var exists = await _db.Commits.AnyAsync(c => c.Sha == commitData.Sha);
                    if (!exists)
                    {
                        _db.Commits.Add(new Commit
                        {
                            RepoId = repo.Id,
                            Sha = commitData.Sha,
                            Message = commitData.Message,
                            AuthoredAt = commitData.AuthoredAt
                        });
                    }
                }
            }

            user.LastSyncedAt = DateTime.UtcNow;
            syncLog.Status = "success";
            syncLog.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _snapshotService.UpdateSnapshotsForUserAsync(userId);
        }
        catch (Exception ex)
        {
            syncLog.Status = "failed";
            syncLog.ErrorMessage = ex.Message;
            syncLog.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw;
        }
    }
}
