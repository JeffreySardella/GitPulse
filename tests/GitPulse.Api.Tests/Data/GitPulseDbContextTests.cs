using GitPulse.Api.Data;
using GitPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Tests.Data;

public class GitPulseDbContextTests
{
    private static GitPulseDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GitPulseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GitPulseDbContext(options);
    }

    [Fact]
    public async Task CanAddAndRetrieveUser()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            GitHubId = "12345",
            Login = "testuser",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Login == "testuser");
        Assert.NotNull(user);
        Assert.Equal("12345", user.GitHubId);
    }
}
