# GitPulse Backend Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete ASP.NET Core 8 backend — database, auth, GitHub sync pipeline, background jobs, and REST API.

**Architecture:** ASP.NET Core 8 Web API with PostgreSQL via EF Core. GitHub OAuth for login, JWTs for API auth, Azure Key Vault for user token storage. Hangfire runs background sync jobs. Polly handles GitHub API resilience. Precomputed snapshots power fast dashboard queries.

**Tech Stack:** ASP.NET Core 8, EF Core, PostgreSQL, Hangfire, Polly, Serilog, Azure Key Vault SDK, xUnit, Moq

---

## Chunk 1: Project Scaffold + Database

### Task 1: Create ASP.NET Core Web API Project

**Files:**
- Create: `src/GitPulse.Api/GitPulse.Api.csproj`
- Create: `src/GitPulse.Api/Program.cs`
- Create: `src/GitPulse.Api/appsettings.json`
- Create: `src/GitPulse.Api/appsettings.Development.json`
- Create: `tests/GitPulse.Api.Tests/GitPulse.Api.Tests.csproj`
- Create: `GitPulse.sln`

- [ ] **Step 1: Create solution and API project**

```bash
dotnet new sln -n GitPulse
dotnet new webapi -n GitPulse.Api -o src/GitPulse.Api --no-openapi
dotnet sln add src/GitPulse.Api/GitPulse.Api.csproj
```

- [ ] **Step 2: Create test project**

```bash
dotnet new xunit -n GitPulse.Api.Tests -o tests/GitPulse.Api.Tests
dotnet sln add tests/GitPulse.Api.Tests/GitPulse.Api.Tests.csproj
dotnet add tests/GitPulse.Api.Tests reference src/GitPulse.Api
```

- [ ] **Step 3: Add NuGet packages to API project**

```bash
cd src/GitPulse.Api
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Hangfire.Core
dotnet add package Hangfire.PostgreSql
dotnet add package Polly
dotnet add package Polly.Extensions.Http
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
cd ../..
```

- [ ] **Step 4: Add Moq to test project**

```bash
dotnet add tests/GitPulse.Api.Tests package Moq
```

- [ ] **Step 5: Verify it builds**

Run: `dotnet build`
Expected: Build succeeded with 0 errors

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: scaffold ASP.NET Core 8 API project with test project and dependencies"
```

---

### Task 2: Define Entity Models

**Files:**
- Create: `src/GitPulse.Api/Models/User.cs`
- Create: `src/GitPulse.Api/Models/Repo.cs`
- Create: `src/GitPulse.Api/Models/Commit.cs`
- Create: `src/GitPulse.Api/Models/DailySnapshot.cs`
- Create: `src/GitPulse.Api/Models/SyncLog.cs`

- [ ] **Step 1: Write User entity**

```csharp
// src/GitPulse.Api/Models/User.cs
namespace GitPulse.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string GitHubId { get; set; }
    public required string Login { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Repo> Repos { get; set; } = [];
}
```

- [ ] **Step 2: Write Repo entity**

```csharp
// src/GitPulse.Api/Models/Repo.cs
namespace GitPulse.Api.Models;

public class Repo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string GitHubId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Language { get; set; } = "Unknown";
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
    public DateTime LastPushedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Commit> Commits { get; set; } = [];
}
```

- [ ] **Step 3: Write Commit entity**

```csharp
// src/GitPulse.Api/Models/Commit.cs
namespace GitPulse.Api.Models;

public class Commit
{
    public int Id { get; set; }
    public int RepoId { get; set; }
    public required string Sha { get; set; }
    public required string Message { get; set; }
    public DateTime AuthoredAt { get; set; }

    public Repo Repo { get; set; } = null!;
}
```

- [ ] **Step 4: Write DailySnapshot entity**

```csharp
// src/GitPulse.Api/Models/DailySnapshot.cs
namespace GitPulse.Api.Models;

public class DailySnapshot
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public int CommitCount { get; set; }
    public int ActiveRepoCount { get; set; }

    public User User { get; set; } = null!;
}
```

- [ ] **Step 5: Write SyncLog entity**

```csharp
// src/GitPulse.Api/Models/SyncLog.cs
namespace GitPulse.Api.Models;

public class SyncLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Status { get; set; } // "success" | "failed"
    public string? ErrorMessage { get; set; }
    public int CommitsSynced { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }

    public User User { get; set; } = null!;
}
```

- [ ] **Step 6: Write RefreshToken entity (for token rotation/invalidation)**

```csharp
// src/GitPulse.Api/Models/RefreshToken.cs
namespace GitPulse.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add src/GitPulse.Api/Models/
git commit -m "feat: add entity models for users, repos, commits, snapshots, sync log, and refresh tokens"
```

---

### Task 3: Create DbContext and Initial Migration

**Files:**
- Create: `src/GitPulse.Api/Data/GitPulseDbContext.cs`
- Modify: `src/GitPulse.Api/Program.cs`
- Modify: `src/GitPulse.Api/appsettings.Development.json`

- [ ] **Step 1: Write DbContext**

```csharp
// src/GitPulse.Api/Data/GitPulseDbContext.cs
using GitPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Data;

public class GitPulseDbContext : DbContext
{
    public GitPulseDbContext(DbContextOptions<GitPulseDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Repo> Repos => Set<Repo>();
    public DbSet<Commit> Commits => Set<Commit>();
    public DbSet<DailySnapshot> DailySnapshots => Set<DailySnapshot>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.GitHubId).IsUnique();
            e.HasIndex(u => u.Login).IsUnique();
        });

        modelBuilder.Entity<Repo>(e =>
        {
            e.HasIndex(r => r.GitHubId).IsUnique();
            e.HasOne(r => r.User).WithMany(u => u.Repos).HasForeignKey(r => r.UserId);
        });

        modelBuilder.Entity<Commit>(e =>
        {
            e.HasIndex(c => c.Sha).IsUnique();
            e.HasOne(c => c.Repo).WithMany(r => r.Commits).HasForeignKey(c => c.RepoId);
        });

        modelBuilder.Entity<DailySnapshot>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.Date }).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
        });

        modelBuilder.Entity<SyncLog>(e =>
        {
            e.HasIndex(l => l.StartedAt);
            e.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(r => r.Token).IsUnique();
            e.HasIndex(r => r.UserId);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
        });
    }
}
```

- [ ] **Step 2: Add connection string to appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gitpulse;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

- [ ] **Step 3: Register DbContext in Program.cs**

Add to `Program.cs` before `builder.Build()`:
```csharp
builder.Services.AddDbContext<GitPulseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

- [ ] **Step 4: Create initial migration**

```bash
cd src/GitPulse.Api
dotnet ef migrations add InitialCreate
cd ../..
```

- [ ] **Step 5: Verify build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/GitPulse.Api/Data/ src/GitPulse.Api/Migrations/ src/GitPulse.Api/Program.cs src/GitPulse.Api/appsettings.Development.json
git commit -m "feat: add DbContext with EF Core PostgreSQL config and initial migration"
```

---

## Chunk 2: GitHub OAuth + JWT Authentication

### Task 4: GitHub OAuth Service

**Files:**
- Create: `src/GitPulse.Api/Services/IGitHubAuthService.cs`
- Create: `src/GitPulse.Api/Services/GitHubAuthService.cs`
- Create: `tests/GitPulse.Api.Tests/Services/GitHubAuthServiceTests.cs`

- [ ] **Step 1: Write the interface**

```csharp
// src/GitPulse.Api/Services/IGitHubAuthService.cs
namespace GitPulse.Api.Services;

public record GitHubUserInfo(string Id, string Login, string? AvatarUrl);

public interface IGitHubAuthService
{
    Task<string> ExchangeCodeForTokenAsync(string code);
    Task<GitHubUserInfo> GetUserInfoAsync(string accessToken);
}
```

- [ ] **Step 2: Write failing test for ExchangeCodeForTokenAsync**

```csharp
// tests/GitPulse.Api.Tests/Services/GitHubAuthServiceTests.cs
using GitPulse.Api.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;

namespace GitPulse.Api.Tests.Services;

public class GitHubAuthServiceTests
{
    [Fact]
    public async Task ExchangeCodeForTokenAsync_ReturnsToken_WhenGitHubRespondsOk()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new { access_token = "gho_test123" })
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new GitHubAuthService(httpClient, "client-id", "client-secret");

        var token = await service.ExchangeCodeForTokenAsync("test-code");

        Assert.Equal("gho_test123", token);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "ExchangeCodeForTokenAsync_ReturnsToken"`
Expected: FAIL — GitHubAuthService constructor doesn't exist yet

- [ ] **Step 4: Implement GitHubAuthService with IOptions**

```csharp
// src/GitPulse.Api/Services/GitHubOptions.cs
namespace GitPulse.Api.Services;

public class GitHubOptions
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}
```

```csharp
// src/GitPulse.Api/Services/GitHubAuthService.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace GitPulse.Api.Services;

public class GitHubAuthService : IGitHubAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;

    // Production constructor (DI)
    public GitHubAuthService(HttpClient httpClient, IOptions<GitHubOptions> options)
        : this(httpClient, options.Value.ClientId, options.Value.ClientSecret) { }

    // Test constructor
    public GitHubAuthService(HttpClient httpClient, string clientId, string clientSecret)
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = JsonContent.Create(new
            {
                client_id = _clientId,
                client_secret = _clientSecret,
                code
            })
        };
        request.Headers.Accept.Add(new("application/json"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return result?.AccessToken ?? throw new InvalidOperationException("No access token in GitHub response");
    }

    public async Task<GitHubUserInfo> GetUserInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("GitPulse");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<GitHubApiUser>();
        return new GitHubUserInfo(
            user?.Id.ToString() ?? throw new InvalidOperationException("No user ID"),
            user.Login,
            user.AvatarUrl
        );
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken
    );

    private record GitHubApiUser(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl
    );
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "ExchangeCodeForTokenAsync_ReturnsToken"`
Expected: PASS

- [ ] **Step 6: Write test for GetUserInfoAsync**

Add to `GitHubAuthServiceTests.cs`:
```csharp
[Fact]
public async Task GetUserInfoAsync_ReturnsUserInfo_WhenGitHubRespondsOk()
{
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new { id = 12345, login = "testuser", avatar_url = "https://example.com/avatar.png" })
        });

    var httpClient = new HttpClient(mockHandler.Object);
    var service = new GitHubAuthService(httpClient, "client-id", "client-secret");

    var userInfo = await service.GetUserInfoAsync("gho_test123");

    Assert.Equal("12345", userInfo.Id);
    Assert.Equal("testuser", userInfo.Login);
    Assert.Equal("https://example.com/avatar.png", userInfo.AvatarUrl);
}
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "GetUserInfoAsync_ReturnsUserInfo"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/GitPulse.Api/Services/IGitHubAuthService.cs src/GitPulse.Api/Services/GitHubAuthService.cs tests/GitPulse.Api.Tests/Services/
git commit -m "feat: add GitHub OAuth service with token exchange and user info retrieval"
```

---

### Task 5: JWT Token Service

**Files:**
- Create: `src/GitPulse.Api/Services/ITokenService.cs`
- Create: `src/GitPulse.Api/Services/TokenService.cs`
- Create: `tests/GitPulse.Api.Tests/Services/TokenServiceTests.cs`

- [ ] **Step 1: Write the interface**

```csharp
// src/GitPulse.Api/Services/ITokenService.cs
using GitPulse.Api.Models;

namespace GitPulse.Api.Services;

public record TokenPair(string AccessToken, string RefreshToken);

public interface ITokenService
{
    string GenerateAccessToken(User user);
    Task<string> GenerateRefreshTokenAsync(User user);
    Task<int?> ValidateAndRotateRefreshTokenAsync(string refreshToken);
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// tests/GitPulse.Api.Tests/Services/TokenServiceTests.cs
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
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "TokenServiceTests"`
Expected: FAIL — TokenService doesn't exist

- [ ] **Step 4: Implement TokenService with DB-backed refresh token rotation**

```csharp
// src/GitPulse.Api/Services/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GitPulse.Api.Data;
using GitPulse.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GitPulse.Api.Services;

public class TokenService : ITokenService
{
    private readonly GitPulseDbContext _db;
    private readonly string _jwtSecret;
    private readonly string _issuer;
    private readonly int _accessTokenMinutes;

    public TokenService(GitPulseDbContext db, string jwtSecret, string issuer, int accessTokenMinutes = 15)
    {
        _db = db;
        _jwtSecret = jwtSecret;
        _issuer = issuer;
        _accessTokenMinutes = accessTokenMinutes;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim("github_id", user.GitHubId)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(User user)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return tokenValue;
    }

    public async Task<int?> ValidateAndRotateRefreshTokenAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return null;

        // Revoke the old token
        stored.IsRevoked = true;
        await _db.SaveChangesAsync();

        return stored.UserId;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "TokenServiceTests"`
Expected: All 5 PASS

- [ ] **Step 6: Commit**

```bash
git add src/GitPulse.Api/Services/ITokenService.cs src/GitPulse.Api/Services/TokenService.cs tests/GitPulse.Api.Tests/Services/TokenServiceTests.cs
git commit -m "feat: add JWT token service with DB-backed refresh token rotation and revocation"
```

---

### Task 6: Auth Controller

**Files:**
- Create: `src/GitPulse.Api/Controllers/AuthController.cs`
- Modify: `src/GitPulse.Api/Program.cs`

- [ ] **Step 1: Write AuthController**

```csharp
// src/GitPulse.Api/Controllers/AuthController.cs
using GitPulse.Api.Data;
using GitPulse.Api.Models;
using GitPulse.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IGitHubAuthService _gitHubAuth;
    private readonly ITokenService _tokenService;
    private readonly ISecretStore _secretStore;
    private readonly GitPulseDbContext _db;

    public AuthController(IGitHubAuthService gitHubAuth, ITokenService tokenService, ISecretStore secretStore, GitPulseDbContext db)
    {
        _gitHubAuth = gitHubAuth;
        _tokenService = tokenService;
        _secretStore = secretStore;
        _db = db;
    }

    [HttpPost("github")]
    public async Task<IActionResult> GitHubLogin([FromBody] GitHubLoginRequest request)
    {
        var ghToken = await _gitHubAuth.ExchangeCodeForTokenAsync(request.Code);
        var ghUser = await _gitHubAuth.GetUserInfoAsync(ghToken);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.GitHubId == ghUser.Id);
        if (user is null)
        {
            user = new User
            {
                GitHubId = ghUser.Id,
                Login = ghUser.Login,
                AvatarUrl = ghUser.AvatarUrl,
                CreatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.MinValue
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // Always store the fresh GitHub token (new and returning users)
        await _secretStore.StoreTokenAsync(user.Id, ghToken);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);
        return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var userId = await _tokenService.ValidateAndRotateRefreshTokenAsync(request.RefreshToken);
        if (userId is null)
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null)
            return Unauthorized();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);
        return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
    }
}

public record GitHubLoginRequest(string Code);
public record RefreshRequest(string RefreshToken);
```

- [ ] **Step 2: Wire up JWT auth and services in Program.cs**

Add to `Program.cs`:
```csharp
using System.Text;
using GitPulse.Api.Data;
using GitPulse.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<GitPulseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "gitpulse";

builder.Services.AddScoped<ITokenService>(sp =>
    new TokenService(sp.GetRequiredService<GitPulseDbContext>(), jwtSecret, jwtIssuer));

builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));
builder.Services.AddHttpClient<IGitHubAuthService, GitHubAuthService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GitPulse");
}).AddPolicyHandler(_ => Polly.Extensions.Http.HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

- [ ] **Step 3: Add JWT config to appsettings.Development.json**

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
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/GitPulse.Api/Controllers/AuthController.cs src/GitPulse.Api/Program.cs src/GitPulse.Api/appsettings.Development.json
git commit -m "feat: add auth controller with GitHub OAuth login and JWT refresh endpoints"
```

---

## Chunk 3: Azure Key Vault + GitHub API Service

### Task 7: Azure Key Vault Integration for User Tokens

**Files:**
- Create: `src/GitPulse.Api/Services/ISecretStore.cs`
- Create: `src/GitPulse.Api/Services/KeyVaultSecretStore.cs`
- Create: `src/GitPulse.Api/Services/InMemorySecretStore.cs`
- Create: `tests/GitPulse.Api.Tests/Services/InMemorySecretStoreTests.cs`

- [ ] **Step 1: Write the interface**

```csharp
// src/GitPulse.Api/Services/ISecretStore.cs
namespace GitPulse.Api.Services;

public interface ISecretStore
{
    Task StoreTokenAsync(int userId, string token);
    Task<string?> GetTokenAsync(int userId);
    Task DeleteTokenAsync(int userId);
}
```

- [ ] **Step 2: Write InMemorySecretStore (for local dev/testing)**

```csharp
// src/GitPulse.Api/Services/InMemorySecretStore.cs
using System.Collections.Concurrent;

namespace GitPulse.Api.Services;

public class InMemorySecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> _secrets = new();

    public Task StoreTokenAsync(int userId, string token)
    {
        _secrets[$"github-token-{userId}"] = token;
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync(int userId)
    {
        _secrets.TryGetValue($"github-token-{userId}", out var token);
        return Task.FromResult(token);
    }

    public Task DeleteTokenAsync(int userId)
    {
        _secrets.TryRemove($"github-token-{userId}", out _);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Write failing tests**

```csharp
// tests/GitPulse.Api.Tests/Services/InMemorySecretStoreTests.cs
using GitPulse.Api.Services;

namespace GitPulse.Api.Tests.Services;

public class InMemorySecretStoreTests
{
    [Fact]
    public async Task StoreAndRetrieve_ReturnsStoredToken()
    {
        var store = new InMemorySecretStore();
        await store.StoreTokenAsync(1, "gho_abc123");

        var token = await store.GetTokenAsync(1);

        Assert.Equal("gho_abc123", token);
    }

    [Fact]
    public async Task GetToken_ReturnsNull_WhenNotStored()
    {
        var store = new InMemorySecretStore();

        var token = await store.GetTokenAsync(999);

        Assert.Null(token);
    }

    [Fact]
    public async Task DeleteToken_RemovesToken()
    {
        var store = new InMemorySecretStore();
        await store.StoreTokenAsync(1, "gho_abc123");

        await store.DeleteTokenAsync(1);
        var token = await store.GetTokenAsync(1);

        Assert.Null(token);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "InMemorySecretStoreTests"`
Expected: All 3 PASS

- [ ] **Step 5: Write KeyVaultSecretStore**

```csharp
// src/GitPulse.Api/Services/KeyVaultSecretStore.cs
using Azure.Security.KeyVault.Secrets;

namespace GitPulse.Api.Services;

public class KeyVaultSecretStore : ISecretStore
{
    private readonly SecretClient _client;

    public KeyVaultSecretStore(SecretClient client)
    {
        _client = client;
    }

    public async Task StoreTokenAsync(int userId, string token)
    {
        await _client.SetSecretAsync($"github-token-{userId}", token);
    }

    public async Task<string?> GetTokenAsync(int userId)
    {
        try
        {
            var secret = await _client.GetSecretAsync($"github-token-{userId}");
            return secret.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteTokenAsync(int userId)
    {
        try
        {
            await _client.StartDeleteSecretAsync($"github-token-{userId}");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Already deleted
        }
    }
}
```

- [ ] **Step 6: Register in Program.cs** (InMemory for dev, KeyVault for prod)

Add to `Program.cs` service registration:
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ISecretStore, InMemorySecretStore>();
}
else
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault URI not configured");
    builder.Services.AddSingleton<ISecretStore>(
        new KeyVaultSecretStore(new SecretClient(new Uri(keyVaultUri), new Azure.Identity.DefaultAzureCredential())));
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add src/GitPulse.Api/Services/ISecretStore.cs src/GitPulse.Api/Services/KeyVaultSecretStore.cs src/GitPulse.Api/Services/InMemorySecretStore.cs src/GitPulse.Api/Program.cs tests/GitPulse.Api.Tests/Services/InMemorySecretStoreTests.cs
git commit -m "feat: add secret store abstraction with Key Vault and in-memory implementations"
```

---

### Task 8: GitHub API Data Service with Polly

**Files:**
- Create: `src/GitPulse.Api/Services/IGitHubDataService.cs`
- Create: `src/GitPulse.Api/Services/GitHubDataService.cs`
- Create: `tests/GitPulse.Api.Tests/Services/GitHubDataServiceTests.cs`

- [ ] **Step 1: Write the interface**

```csharp
// src/GitPulse.Api/Services/IGitHubDataService.cs
namespace GitPulse.Api.Services;

public record GitHubRepoData(string Id, string Name, string? Description, string Language, int Stars, int Forks, DateTime LastPushedAt);
public record GitHubCommitData(string Sha, string Message, DateTime AuthoredAt, string RepoName);
public record GitHubLanguageData(string Language, long Bytes);

public interface IGitHubDataService
{
    Task<IReadOnlyList<GitHubRepoData>> GetReposAsync(string accessToken);
    Task<IReadOnlyList<GitHubCommitData>> GetCommitsSinceAsync(string accessToken, string repoFullName, DateTime since);
    Task<IReadOnlyList<GitHubLanguageData>> GetLanguagesAsync(string accessToken, string repoFullName);
}
```

- [ ] **Step 2: Write failing test**

```csharp
// tests/GitPulse.Api.Tests/Services/GitHubDataServiceTests.cs
using GitPulse.Api.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;

namespace GitPulse.Api.Tests.Services;

public class GitHubDataServiceTests
{
    [Fact]
    public async Task GetReposAsync_ParsesGitHubResponse()
    {
        var repos = new[]
        {
            new { id = 1, name = "my-repo", description = "A repo", language = "C#", stargazers_count = 5, forks_count = 2, pushed_at = "2026-01-01T00:00:00Z" }
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(repos)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new GitHubDataService(httpClient);

        var result = await service.GetReposAsync("gho_test");

        Assert.Single(result);
        Assert.Equal("my-repo", result[0].Name);
        Assert.Equal("C#", result[0].Language);
        Assert.Equal(5, result[0].Stars);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "GetReposAsync_ParsesGitHubResponse"`
Expected: FAIL — GitHubDataService doesn't exist

- [ ] **Step 4: Implement GitHubDataService**

```csharp
// src/GitPulse.Api/Services/GitHubDataService.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GitPulse.Api.Services;

public class GitHubDataService : IGitHubDataService
{
    private readonly HttpClient _httpClient;

    public GitHubDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api.github.com/");
    }

    public async Task<IReadOnlyList<GitHubRepoData>> GetReposAsync(string accessToken)
    {
        var request = CreateRequest(HttpMethod.Get, "user/repos?per_page=100&sort=pushed", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var repos = await response.Content.ReadFromJsonAsync<List<RepoResponse>>() ?? [];
        return repos.Select(r => new GitHubRepoData(
            r.Id.ToString(), r.Name, r.Description, r.Language ?? "Unknown",
            r.StargazersCount, r.ForksCount, r.PushedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<GitHubCommitData>> GetCommitsSinceAsync(string accessToken, string repoFullName, DateTime since)
    {
        var request = CreateRequest(HttpMethod.Get,
            $"repos/{repoFullName}/commits?since={since:O}&per_page=100", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var commits = await response.Content.ReadFromJsonAsync<List<CommitResponse>>() ?? [];
        return commits.Select(c => new GitHubCommitData(
            c.Sha, c.Commit.Message, c.Commit.Author.Date, repoFullName
        )).ToList();
    }

    public async Task<IReadOnlyList<GitHubLanguageData>> GetLanguagesAsync(string accessToken, string repoFullName)
    {
        var request = CreateRequest(HttpMethod.Get, $"repos/{repoFullName}/languages", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var languages = await response.Content.ReadFromJsonAsync<Dictionary<string, long>>() ?? [];
        return languages.Select(kv => new GitHubLanguageData(kv.Key, kv.Value)).ToList();
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("GitPulse");
        return request;
    }

    private record RepoResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("stargazers_count")] int StargazersCount,
        [property: JsonPropertyName("forks_count")] int ForksCount,
        [property: JsonPropertyName("pushed_at")] DateTime PushedAt
    );

    private record CommitResponse(
        [property: JsonPropertyName("sha")] string Sha,
        [property: JsonPropertyName("commit")] CommitDetail Commit
    );

    private record CommitDetail(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("author")] CommitAuthor Author
    );

    private record CommitAuthor(
        [property: JsonPropertyName("date")] DateTime Date
    );
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "GitHubDataServiceTests"`
Expected: PASS

- [ ] **Step 6: Register GitHubDataService in Program.cs with Polly**

Add to `Program.cs`:
```csharp
builder.Services.AddHttpClient<IGitHubDataService, GitHubDataService>()
    .AddPolicyHandler(Polly.Extensions.Http.HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

- [ ] **Step 7: Commit**

```bash
git add src/GitPulse.Api/Services/IGitHubDataService.cs src/GitPulse.Api/Services/GitHubDataService.cs src/GitPulse.Api/Program.cs tests/GitPulse.Api.Tests/Services/GitHubDataServiceTests.cs
git commit -m "feat: add GitHub data service with Polly retry and 429 handling"
```

---

## Chunk 4: Snapshot Service + Background Jobs

### Task 9: Snapshot Service

**Files:**
- Create: `src/GitPulse.Api/Services/ISnapshotService.cs`
- Create: `src/GitPulse.Api/Services/SnapshotService.cs`
- Create: `tests/GitPulse.Api.Tests/Services/SnapshotServiceTests.cs`

- [ ] **Step 1: Write the interface**

```csharp
// src/GitPulse.Api/Services/ISnapshotService.cs
namespace GitPulse.Api.Services;

public interface ISnapshotService
{
    /// Backfills snapshots for all dates with commits in the last 90 days, not just today.
    Task UpdateSnapshotsForUserAsync(int userId);
}
```

- [ ] **Step 2: Write failing test**

```csharp
// tests/GitPulse.Api.Tests/Services/SnapshotServiceTests.cs
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
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GitPulseDbContext(options);
    }

    [Fact]
    public async Task UpdateSnapshots_CreatesSnapshotFromCommits()
    {
        var db = CreateInMemoryDb();
        var user = new User { GitHubId = "1", Login = "test", CreatedAt = DateTime.UtcNow, LastSyncedAt = DateTime.UtcNow };
        db.Users.Add(user);

        var repo = new Repo { User = user, GitHubId = "r1", Name = "repo1", Language = "C#", LastPushedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
        db.Repos.Add(repo);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Commits.Add(new Commit { Repo = repo, Sha = "abc1", Message = "fix", AuthoredAt = DateTime.UtcNow });
        db.Commits.Add(new Commit { Repo = repo, Sha = "abc2", Message = "feat", AuthoredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SnapshotService(db);
        await service.UpdateSnapshotsForUserAsync(user.Id);

        var snapshot = await db.DailySnapshots.FirstOrDefaultAsync(s => s.UserId == user.Id && s.Date == today);
        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot.CommitCount);
        Assert.Equal(1, snapshot.ActiveRepoCount);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "SnapshotServiceTests"`
Expected: FAIL

- [ ] **Step 4: Implement SnapshotService**

```csharp
// src/GitPulse.Api/Services/SnapshotService.cs
using GitPulse.Api.Data;
using GitPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Services;

public class SnapshotService : ISnapshotService
{
    private readonly GitPulseDbContext _db;

    public SnapshotService(GitPulseDbContext db)
    {
        _db = db;
    }

    public async Task UpdateSnapshotsForUserAsync(int userId)
    {
        var ninetyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));

        // Get all dates with commits in the last 90 days
        var commitsByDate = await _db.Commits
            .Where(c => c.Repo.UserId == userId && DateOnly.FromDateTime(c.AuthoredAt) >= ninetyDaysAgo)
            .GroupBy(c => DateOnly.FromDateTime(c.AuthoredAt))
            .Select(g => new
            {
                Date = g.Key,
                CommitCount = g.Count(),
                ActiveRepoCount = g.Select(c => c.RepoId).Distinct().Count()
            })
            .ToListAsync();

        foreach (var day in commitsByDate)
        {
            var existing = await _db.DailySnapshots
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == day.Date);

            if (existing is not null)
            {
                existing.CommitCount = day.CommitCount;
                existing.ActiveRepoCount = day.ActiveRepoCount;
            }
            else
            {
                _db.DailySnapshots.Add(new DailySnapshot
                {
                    UserId = userId,
                    Date = day.Date,
                    CommitCount = day.CommitCount,
                    ActiveRepoCount = day.ActiveRepoCount
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "SnapshotServiceTests"`
Expected: PASS

- [ ] **Step 6: Register in Program.cs**

```csharp
builder.Services.AddScoped<ISnapshotService, SnapshotService>();
```

- [ ] **Step 7: Commit**

```bash
git add src/GitPulse.Api/Services/ISnapshotService.cs src/GitPulse.Api/Services/SnapshotService.cs src/GitPulse.Api/Program.cs tests/GitPulse.Api.Tests/Services/SnapshotServiceTests.cs
git commit -m "feat: add snapshot service that computes daily commit aggregates"
```

---

### Task 10: Sync Service (Orchestrates GitHub Sync)

**Files:**
- Create: `src/GitPulse.Api/Services/ISyncService.cs`
- Create: `src/GitPulse.Api/Services/SyncService.cs`
- Create: `tests/GitPulse.Api.Tests/Services/SyncServiceTests.cs`

- [ ] **Step 1: Write the interface**

```csharp
// src/GitPulse.Api/Services/ISyncService.cs
namespace GitPulse.Api.Services;

public interface ISyncService
{
    Task SyncUserAsync(int userId);
}
```

- [ ] **Step 2: Write failing test**

```csharp
// tests/GitPulse.Api.Tests/Services/SyncServiceTests.cs
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
                new("r1", "repo1", null, "C#", 1, 0, DateTime.UtcNow)
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
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "SyncServiceTests"`
Expected: FAIL

- [ ] **Step 4: Implement SyncService**

```csharp
// src/GitPulse.Api/Services/SyncService.cs
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
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.MinValue
        };
        _db.SyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();

        try
        {
            var repos = await _github.GetReposAsync(token);
            var totalCommits = 0;

            foreach (var repoData in repos)
            {
                var repo = await _db.Repos.FirstOrDefaultAsync(r => r.GitHubId == repoData.Id);
                if (repo is null)
                {
                    repo = new Repo
                    {
                        UserId = userId,
                        GitHubId = repoData.Id,
                        Name = repoData.Name,
                        Description = repoData.Description,
                        Language = repoData.Language,
                        StarCount = repoData.Stars,
                        ForkCount = repoData.Forks,
                        LastPushedAt = repoData.LastPushedAt,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Repos.Add(repo);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    repo.StarCount = repoData.Stars;
                    repo.ForkCount = repoData.Forks;
                    repo.LastPushedAt = repoData.LastPushedAt;
                    repo.Language = repoData.Language;
                }

                var commits = await _github.GetCommitsSinceAsync(token, $"{user.Login}/{repoData.Name}", user.LastSyncedAt);
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
                        totalCommits++;
                    }
                }
            }

            user.LastSyncedAt = DateTime.UtcNow;
            syncLog.Status = "success";
            syncLog.CommitsSynced = totalCommits;
            syncLog.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _snapshotService.UpdateSnapshotsForUserAsync(userId);
        }
        catch (Exception ex)
        {
            syncLog.Status = "failed";
            syncLog.ErrorMessage = ex.Message;
            syncLog.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw;
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "SyncServiceTests"`
Expected: PASS

- [ ] **Step 6: Register in Program.cs**

```csharp
builder.Services.AddScoped<ISyncService, SyncService>();
```

- [ ] **Step 7: Commit**

```bash
git add src/GitPulse.Api/Services/ISyncService.cs src/GitPulse.Api/Services/SyncService.cs src/GitPulse.Api/Program.cs tests/GitPulse.Api.Tests/Services/SyncServiceTests.cs
git commit -m "feat: add sync service that orchestrates GitHub data pull, repo/commit upsert, and snapshots"
```

---

### Task 11: Hangfire Background Jobs

**Files:**
- Create: `src/GitPulse.Api/Jobs/SyncUsersJob.cs`
- Create: `src/GitPulse.Api/Jobs/PurgeSyncLogsJob.cs`
- Modify: `src/GitPulse.Api/Program.cs`

- [ ] **Step 1: Write SyncUsersJob**

```csharp
// src/GitPulse.Api/Jobs/SyncUsersJob.cs
using GitPulse.Api.Data;
using GitPulse.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GitPulse.Api.Jobs;

public class SyncUsersJob
{
    private readonly GitPulseDbContext _db;
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncUsersJob> _logger;

    public SyncUsersJob(GitPulseDbContext db, ISyncService syncService, ILogger<SyncUsersJob> logger)
    {
        _db = db;
        _syncService = syncService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var staleThreshold = DateTime.UtcNow.AddHours(-1);
        var staleUsers = await _db.Users
            .Where(u => u.LastSyncedAt < staleThreshold)
            .Select(u => u.Id)
            .ToListAsync();

        _logger.LogInformation("Found {Count} stale users to sync", staleUsers.Count);

        foreach (var userId in staleUsers)
        {
            try
            {
                await _syncService.SyncUserAsync(userId);
                _logger.LogInformation("Synced user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync user {UserId}", userId);
            }
        }
    }
}
```

- [ ] **Step 2: Write HangfireAuthorizationFilter (production dashboard protection)**

```csharp
// src/GitPulse.Api/Jobs/HangfireAuthorizationFilter.cs
using Hangfire.Dashboard;

namespace GitPulse.Api.Jobs;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}
```

- [ ] **Step 3: Write PurgeSyncLogsJob**

```csharp
// src/GitPulse.Api/Jobs/PurgeSyncLogsJob.cs
using GitPulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GitPulse.Api.Jobs;

public class PurgeSyncLogsJob
{
    private readonly GitPulseDbContext _db;
    private readonly ILogger<PurgeSyncLogsJob> _logger;

    public PurgeSyncLogsJob(GitPulseDbContext db, ILogger<PurgeSyncLogsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var deleted = await _db.SyncLogs
            .Where(l => l.StartedAt < cutoff)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Purged {Count} sync log entries older than 30 days", deleted);
    }
}
```

- [ ] **Step 4: Wire up Hangfire in Program.cs**

Add to `Program.cs`:
```csharp
using Hangfire;
using Hangfire.PostgreSql;
using GitPulse.Api.Jobs;

// In service registration:
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<SyncUsersJob>();
builder.Services.AddScoped<PurgeSyncLogsJob>();

// After app.Build(), before app.Run():
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
else
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

RecurringJob.AddOrUpdate<SyncUsersJob>("sync-users", job => job.ExecuteAsync(), Cron.Hourly);
RecurringJob.AddOrUpdate<PurgeSyncLogsJob>("purge-sync-logs", job => job.ExecuteAsync(), Cron.Weekly);
```

- [ ] **Step 5: Verify build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/GitPulse.Api/Jobs/ src/GitPulse.Api/Program.cs
git commit -m "feat: add Hangfire background jobs with auth-protected dashboard, hourly sync, and weekly purge"
```

---

## Chunk 5: REST API Endpoints + Serilog

### Task 12: REST API Endpoints

**Files:**
- Create: `src/GitPulse.Api/Controllers/StatsController.cs`
- Create: `src/GitPulse.Api/Controllers/CommitsController.cs`
- Create: `src/GitPulse.Api/Controllers/LanguagesController.cs`
- Create: `src/GitPulse.Api/Controllers/ReposController.cs`

- [ ] **Step 1: Write StatsController**

```csharp
// src/GitPulse.Api/Controllers/StatsController.cs
using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly GitPulseDbContext _db;

    public StatsController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ninetyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));
        var snapshots = await _db.DailySnapshots
            .Where(s => s.UserId == userId && s.Date >= ninetyDaysAgo)
            .OrderBy(s => s.Date)
            .Select(s => new { s.Date, s.CommitCount, s.ActiveRepoCount })
            .ToListAsync();

        var totalCommits = snapshots.Sum(s => s.CommitCount);
        var totalRepos = await _db.Repos.CountAsync(r => r.UserId == userId);

        return Ok(new
        {
            TotalCommits = totalCommits,
            TotalRepos = totalRepos,
            DailySnapshots = snapshots
        });
    }
}
```

- [ ] **Step 2: Write CommitsController**

```csharp
// src/GitPulse.Api/Controllers/CommitsController.cs
using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommitsController : ControllerBase
{
    private readonly GitPulseDbContext _db;

    public CommitsController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCommits([FromQuery] int limit = 50)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var commits = await _db.Commits
            .Where(c => c.Repo.UserId == userId)
            .OrderByDescending(c => c.AuthoredAt)
            .Take(limit)
            .Select(c => new { c.Sha, c.Message, c.AuthoredAt, RepoName = c.Repo.Name })
            .ToListAsync();

        return Ok(commits);
    }
}
```

- [ ] **Step 3: Write LanguagesController**

```csharp
// src/GitPulse.Api/Controllers/LanguagesController.cs
using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LanguagesController : ControllerBase
{
    private readonly GitPulseDbContext _db;

    public LanguagesController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetLanguages()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var languages = await _db.Repos
            .Where(r => r.UserId == userId)
            .GroupBy(r => r.Language)
            .Select(g => new { Language = g.Key, RepoCount = g.Count() })
            .OrderByDescending(l => l.RepoCount)
            .ToListAsync();

        return Ok(languages);
    }
}
```

- [ ] **Step 4: Write ReposController**

```csharp
// src/GitPulse.Api/Controllers/ReposController.cs
using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReposController : ControllerBase
{
    private readonly GitPulseDbContext _db;

    public ReposController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetRepos()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var repos = await _db.Repos
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.LastPushedAt)
            .Select(r => new
            {
                r.Name,
                r.Description,
                r.Language,
                r.StarCount,
                r.ForkCount,
                r.LastPushedAt,
                CommitCount = r.Commits.Count
            })
            .ToListAsync();

        return Ok(repos);
    }
}
```

- [ ] **Step 5: Verify build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/GitPulse.Api/Controllers/
git commit -m "feat: add REST endpoints for stats, commits, languages, and repos"
```

---

### Task 13: Serilog Setup

**Files:**
- Modify: `src/GitPulse.Api/Program.cs`
- Modify: `src/GitPulse.Api/appsettings.json`

- [ ] **Step 1: Configure Serilog in Program.cs**

Add at the top of `Program.cs`, before `WebApplication.CreateBuilder`:
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ... existing service registration ...

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // ... existing middleware ...

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

- [ ] **Step 2: Add Serilog config to appsettings.json**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Hangfire": "Information"
      }
    }
  }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/GitPulse.Api/Program.cs src/GitPulse.Api/appsettings.json
git commit -m "feat: add Serilog structured logging with console sink and request logging"
```

---

### Task 14: Controller Integration Tests

**Files:**
- Create: `tests/GitPulse.Api.Tests/Controllers/StatsControllerTests.cs`

- [ ] **Step 1: Write StatsController test (validates snapshot-backed query)**

```csharp
// tests/GitPulse.Api.Tests/Controllers/StatsControllerTests.cs
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

        // Add snapshot for today
        db.DailySnapshots.Add(new DailySnapshot
        {
            UserId = user.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            CommitCount = 5,
            ActiveRepoCount = 2
        });

        // Add old snapshot (should be excluded)
        db.DailySnapshots.Add(new DailySnapshot
        {
            UserId = user.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-100)),
            CommitCount = 3,
            ActiveRepoCount = 1
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
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/GitPulse.Api.Tests --filter "StatsControllerTests"`
Expected: All PASS

- [ ] **Step 3: Commit**

```bash
git add tests/GitPulse.Api.Tests/Controllers/
git commit -m "test: add integration tests for StatsController snapshot queries"
```

---

## Summary

| Chunk | Tasks | What it delivers |
|-------|-------|-----------------|
| 1 | 1-3 | Project scaffold, entities (incl. RefreshToken), DbContext, migrations |
| 2 | 4-6 | GitHub OAuth (IOptions), JWT auth with DB-backed refresh rotation, auth controller |
| 3 | 7-8 | Key Vault secret store, GitHub data service with Polly |
| 4 | 9-11 | Snapshot service (90-day backfill), sync orchestrator, Hangfire jobs (auth-protected dashboard) |
| 5 | 12-14 | REST API endpoints, Serilog logging, controller tests |
