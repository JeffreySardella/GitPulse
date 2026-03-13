using GitPulse.Api.Data;
using GitPulse.Api.Models;
using GitPulse.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Tests.Services;

public class TokenServiceTests
{
    private static GitPulseDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<GitPulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GitPulseDbContext(options);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var db = CreateInMemoryDb();
        var service = new TokenService(db,
            jwtSecret: "test-secret-key-that-is-at-least-32-bytes-long!",
            issuer: "gitpulse-test");

        var user = new User { Id = 1, GitHubId = "123", Login = "testuser" };
        var token = service.GenerateAccessToken(user);

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_StoresTokenInDb()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "123", Login = "testuser", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new TokenService(db,
            jwtSecret: "test-secret-key-that-is-at-least-32-bytes-long!",
            issuer: "gitpulse-test");

        var token = await service.GenerateRefreshTokenAsync(user);

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task ValidateAndRotate_ReturnsUserId_AndRevokesOldToken()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "123", Login = "testuser", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new TokenService(db,
            jwtSecret: "test-secret-key-that-is-at-least-32-bytes-long!",
            issuer: "gitpulse-test");

        var oldToken = await service.GenerateRefreshTokenAsync(user);
        var userId = await service.ValidateAndRotateRefreshTokenAsync(oldToken);

        Assert.Equal(user.Id, userId);

        // Old token should now be revoked
        var stored = await db.RefreshTokens.FirstAsync(t => t.Token == oldToken);
        Assert.True(stored.IsRevoked);
    }

    [Fact]
    public async Task ValidateAndRotate_ReturnsNull_ForRevokedToken()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "123", Login = "testuser", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new TokenService(db,
            jwtSecret: "test-secret-key-that-is-at-least-32-bytes-long!",
            issuer: "gitpulse-test");

        var token = await service.GenerateRefreshTokenAsync(user);
        // Use it once (revokes it)
        await service.ValidateAndRotateRefreshTokenAsync(token);
        // Try to use it again
        var userId = await service.ValidateAndRotateRefreshTokenAsync(token);

        Assert.Null(userId);
    }

    [Fact]
    public async Task ValidateAndRotate_ReturnsNull_ForGarbageToken()
    {
        var db = CreateInMemoryDb();
        var service = new TokenService(db,
            jwtSecret: "test-secret-key-that-is-at-least-32-bytes-long!",
            issuer: "gitpulse-test");

        var userId = await service.ValidateAndRotateRefreshTokenAsync("garbage-token");

        Assert.Null(userId);
    }
}
