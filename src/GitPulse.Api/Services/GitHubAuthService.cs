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
        if (!string.IsNullOrEmpty(result?.Error))
            throw new InvalidOperationException($"GitHub OAuth error: {result.Error} — {result.ErrorDescription}");
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
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription
    );

    private record GitHubApiUser(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl
    );
}
