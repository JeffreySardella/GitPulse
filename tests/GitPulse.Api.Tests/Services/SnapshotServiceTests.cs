using GitPulse.Api.Data;
using GitPulse.Api.Models;
using GitPulse.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Tests.Services;

public class SnapshotServiceTests
{
    private static GitPulseDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<GitPulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GitPulseDbContext(options);
    }

    [Fact]
    public async Task UpdateSnapshots_CreatesSnapshotFromCommits()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);

        var repo = new Repo { User = user, GitHubId = 1, Name = "repo1", FullName = "test/repo1", Language = "C#" };
        db.Repos.Add(repo);

        db.Commits.Add(new Commit { Repo = repo, Sha = "abc1", Message = "fix", AuthoredAt = DateTime.UtcNow });
        db.Commits.Add(new Commit { Repo = repo, Sha = "abc2", Message = "feat", AuthoredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SnapshotService(db);
        await service.UpdateSnapshotsForUserAsync(user.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var snapshot = await db.DailySnapshots.FirstOrDefaultAsync(s => s.UserId == user.Id && s.Date == today);
        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot.CommitCount);
        Assert.Equal(1, snapshot.ActiveRepos);
    }

    [Fact]
    public async Task UpdateSnapshots_UpdatesExistingSnapshot()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "2", Login = "test2", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);

        var repo = new Repo { User = user, GitHubId = 2, Name = "repo2", FullName = "test/repo2", Language = "C#" };
        db.Repos.Add(repo);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.DailySnapshots.Add(new DailySnapshot { User = user, Date = today, CommitCount = 1, ActiveRepos = 1 });

        db.Commits.Add(new Commit { Repo = repo, Sha = "abc1", Message = "fix", AuthoredAt = DateTime.UtcNow });
        db.Commits.Add(new Commit { Repo = repo, Sha = "abc2", Message = "feat", AuthoredAt = DateTime.UtcNow });
        db.Commits.Add(new Commit { Repo = repo, Sha = "abc3", Message = "docs", AuthoredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SnapshotService(db);
        await service.UpdateSnapshotsForUserAsync(user.Id);

        var snapshots = await db.DailySnapshots.Where(s => s.UserId == user.Id && s.Date == today).ToListAsync();
        Assert.Single(snapshots);
        Assert.Equal(3, snapshots[0].CommitCount);
        Assert.Equal(1, snapshots[0].ActiveRepos);
    }

    [Fact]
    public async Task UpdateSnapshots_GroupsByDateAndRepo()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "3", Login = "test3", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);

        var repo1 = new Repo { User = user, GitHubId = 3, Name = "repo1", FullName = "test/repo1", Language = "C#" };
        var repo2 = new Repo { User = user, GitHubId = 4, Name = "repo2", FullName = "test/repo2", Language = "Go" };
        db.Repos.AddRange(repo1, repo2);

        var yesterday = DateTime.UtcNow.AddDays(-1);
        db.Commits.Add(new Commit { Repo = repo1, Sha = "sha1", Message = "m1", AuthoredAt = yesterday });
        db.Commits.Add(new Commit { Repo = repo2, Sha = "sha2", Message = "m2", AuthoredAt = yesterday });
        db.Commits.Add(new Commit { Repo = repo1, Sha = "sha3", Message = "m3", AuthoredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SnapshotService(db);
        await service.UpdateSnapshotsForUserAsync(user.Id);

        var snapshots = await db.DailySnapshots.Where(s => s.UserId == user.Id).ToListAsync();
        Assert.Equal(2, snapshots.Count);

        var yesterdaySnapshot = snapshots.First(s => s.Date == DateOnly.FromDateTime(yesterday));
        Assert.Equal(2, yesterdaySnapshot.CommitCount);
        Assert.Equal(2, yesterdaySnapshot.ActiveRepos);

        var todaySnapshot = snapshots.First(s => s.Date == DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(1, todaySnapshot.CommitCount);
        Assert.Equal(1, todaySnapshot.ActiveRepos);
    }
}
