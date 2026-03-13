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
