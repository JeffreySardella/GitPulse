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
            new { id = 1, name = "my-repo", full_name = "testuser/my-repo", description = "A repo", language = "C#", stargazers_count = 5, forks_count = 2, pushed_at = "2026-01-01T00:00:00Z" }
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
        Assert.Equal("testuser/my-repo", result[0].FullName);
        Assert.Equal("C#", result[0].Language);
        Assert.Equal(5, result[0].Stars);
    }
}
