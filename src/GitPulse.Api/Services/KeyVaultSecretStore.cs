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
