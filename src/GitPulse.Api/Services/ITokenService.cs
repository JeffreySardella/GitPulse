using GitPulse.Api.Models;

namespace GitPulse.Api.Services;

public record TokenPair(string AccessToken, string RefreshToken);

public interface ITokenService
{
    string GenerateAccessToken(User user);
    Task<string> GenerateRefreshTokenAsync(User user);
    Task<int?> ValidateAndRotateRefreshTokenAsync(string refreshToken);
}
