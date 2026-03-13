namespace GitPulse.Api.Services;

public interface ISecretStore
{
    Task StoreTokenAsync(int userId, string token);
    Task<string?> GetTokenAsync(int userId);
    Task DeleteTokenAsync(int userId);
}
