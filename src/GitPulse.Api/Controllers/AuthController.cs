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
