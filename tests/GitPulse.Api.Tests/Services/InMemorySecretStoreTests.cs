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
