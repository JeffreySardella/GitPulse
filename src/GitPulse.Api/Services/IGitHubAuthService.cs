namespace GitPulse.Api.Services;

public record GitHubUserInfo(string Id, string Login, string? AvatarUrl);

public interface IGitHubAuthService
{
    Task<string> ExchangeCodeForTokenAsync(string code);
    Task<GitHubUserInfo> GetUserInfoAsync(string accessToken);
}
