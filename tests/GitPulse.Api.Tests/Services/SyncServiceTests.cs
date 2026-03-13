using GitPulse.Api.Data;
using GitPulse.Api.Models;
using GitPulse.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GitPulse.Api.Tests.Services;

public class SyncServiceTests
{
    private static GitPulseDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<GitPulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GitPulseDbContext(options);
    }

    [Fact]
    public async Task SyncUserAsync_CreatesReposAndCommits()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.MinValue };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockGitHub = new Mock<IGitHubDataService>();
        mockGitHub.Setup(g => g.GetReposAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<GitHubRepoData>
            {
                new("1", "repo1", "test/repo1", null, "C#", 1, 0, DateTime.UtcNow)
            });
        mockGitHub.Setup(g => g.GetCommitsSinceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<GitHubCommitData>
            {
                new("sha1", "init", DateTime.UtcNow, "test/repo1")
            });

        var mockSecretStore = new Mock<ISecretStore>();
        mockSecretStore.Setup(s => s.GetTokenAsync(user.Id)).ReturnsAsync("gho_test");

        var mockSnapshot = new Mock<ISnapshotService>();

        var service = new SyncService(db, mockGitHub.Object, mockSecretStore.Object, mockSnapshot.Object);
        await service.SyncUserAsync(user.Id);

        Assert.Equal(1, await db.Repos.CountAsync());
        Assert.Equal(1, await db.Commits.CountAsync());
        Assert.Equal(1, await db.SyncLogs.CountAsync(l => l.Status == "success"));
    }

    [Fact]
    public async Task SyncUserAsync_WriteFailedSyncLog_WhenGitHubThrows()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.MinValue };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockGitHub = new Mock<IGitHubDataService>();
        mockGitHub.Setup(g => g.GetReposAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("GitHub API down"));

        var mockSecretStore = new Mock<ISecretStore>();
        mockSecretStore.Setup(s => s.GetTokenAsync(user.Id)).ReturnsAsync("gho_test");

        var mockSnapshot = new Mock<ISnapshotService>();

        var service = new SyncService(db, mockGitHub.Object, mockSecretStore.Object, mockSnapshot.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.SyncUserAsync(user.Id));

        Assert.Equal(1, await db.SyncLogs.CountAsync(l => l.Status == "failed"));
        var log = await db.SyncLogs.FirstAsync();
        Assert.Contains("GitHub API down", log.ErrorMessage);
    }

    [Fact]
    public async Task SyncUserAsync_Throws_WhenNoToken()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.MinValue };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockGitHub = new Mock<IGitHubDataService>();
        var mockSecretStore = new Mock<ISecretStore>();
        mockSecretStore.Setup(s => s.GetTokenAsync(user.Id)).ReturnsAsync((string?)null);
        var mockSnapshot = new Mock<ISnapshotService>();

        var service = new SyncService(db, mockGitHub.Object, mockSecretStore.Object, mockSnapshot.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SyncUserAsync(user.Id));
    }

    [Fact]
    public async Task SyncUserAsync_UpdatesExistingRepo()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.MinValue };
        db.Users.Add(user);

        var existingRepo = new Repo { User = user, GitHubId = 1, Name = "repo1", FullName = "test/repo1", Language = "C#", Stars = 0 };
        db.Repos.Add(existingRepo);
        await db.SaveChangesAsync();

        var mockGitHub = new Mock<IGitHubDataService>();
        mockGitHub.Setup(g => g.GetReposAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<GitHubRepoData>
            {
                new("1", "repo1", "test/repo1", "desc", "C#", 10, 0, DateTime.UtcNow)
            });
        mockGitHub.Setup(g => g.GetCommitsSinceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<GitHubCommitData>());

        var mockSecretStore = new Mock<ISecretStore>();
        mockSecretStore.Setup(s => s.GetTokenAsync(user.Id)).ReturnsAsync("gho_test");

        var mockSnapshot = new Mock<ISnapshotService>();

        var service = new SyncService(db, mockGitHub.Object, mockSecretStore.Object, mockSnapshot.Object);
        await service.SyncUserAsync(user.Id);

        Assert.Equal(1, await db.Repos.CountAsync());
        var repo = await db.Repos.FirstAsync();
        Assert.Equal(10, repo.Stars);
    }

    [Fact]
    public async Task SyncUserAsync_SkipsDuplicateCommits()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.MinValue };
        db.Users.Add(user);

        var existingRepo = new Repo { User = user, GitHubId = 1, Name = "repo1", FullName = "test/repo1", Language = "C#" };
        db.Repos.Add(existingRepo);
        db.Commits.Add(new Commit { Repo = existingRepo, Sha = "sha1", Message = "init", AuthoredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var mockGitHub = new Mock<IGitHubDataService>();
        mockGitHub.Setup(g => g.GetReposAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<GitHubRepoData>
            {
                new("1", "repo1", "test/repo1", null, "C#", 1, 0, DateTime.UtcNow)
            });
        mockGitHub.Setup(g => g.GetCommitsSinceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<GitHubCommitData>
            {
                new("sha1", "init", DateTime.UtcNow, "test/repo1"),
                new("sha2", "new commit", DateTime.UtcNow, "test/repo1")
            });

        var mockSecretStore = new Mock<ISecretStore>();
        mockSecretStore.Setup(s => s.GetTokenAsync(user.Id)).ReturnsAsync("gho_test");

        var mockSnapshot = new Mock<ISnapshotService>();

        var service = new SyncService(db, mockGitHub.Object, mockSecretStore.Object, mockSnapshot.Object);
        await service.SyncUserAsync(user.Id);

        Assert.Equal(2, await db.Commits.CountAsync());
    }

    [Fact]
    public async Task SyncUserAsync_CallsSnapshotServiceOnSuccess()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.MinValue };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockGitHub = new Mock<IGitHubDataService>();
        mockGitHub.Setup(g => g.GetReposAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<GitHubRepoData>());

        var mockSecretStore = new Mock<ISecretStore>();
        mockSecretStore.Setup(s => s.GetTokenAsync(user.Id)).ReturnsAsync("gho_test");

        var mockSnapshot = new Mock<ISnapshotService>();

        var service = new SyncService(db, mockGitHub.Object, mockSecretStore.Object, mockSnapshot.Object);
        await service.SyncUserAsync(user.Id);

        mockSnapshot.Verify(s => s.UpdateSnapshotsForUserAsync(user.Id), Times.Once);
    }
}
