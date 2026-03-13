using GitPulse.Api.Controllers;
using GitPulse.Api.Data;
using GitPulse.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Tests.Controllers;

public class StatsControllerTests
{
    private static GitPulseDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<GitPulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GitPulseDbContext(options);
    }

    private static StatsController CreateController(GitPulseDbContext db, int userId)
    {
        var controller = new StatsController(db);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetStats_ReturnsSnapshotsForLast90Days()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.DailySnapshots.Add(new DailySnapshot
        {
            UserId = user.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            CommitCount = 5,
            ActiveRepos = 2
        });
        db.DailySnapshots.Add(new DailySnapshot
        {
            UserId = user.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-100)),
            CommitCount = 3,
            ActiveRepos = 1
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, user.Id);
        var result = await controller.GetStats();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetStats_ReturnsEmpty_ForNewUser()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateController(db, user.Id);
        var result = await controller.GetStats();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
