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
}
