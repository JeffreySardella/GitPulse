using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GitPulse.Api.Services;

public class GitHubDataService : IGitHubDataService
{
    private readonly HttpClient _httpClient;

    public GitHubDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api.github.com/");
    }

    public async Task<IReadOnlyList<GitHubRepoData>> GetReposAsync(string accessToken)
    {
        var request = CreateRequest(HttpMethod.Get, "user/repos?per_page=100&sort=pushed", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var repos = await response.Content.ReadFromJsonAsync<List<RepoResponse>>() ?? [];
        return repos.Select(r => new GitHubRepoData(
            r.Id.ToString(), r.Name, r.FullName, r.Description, r.Language ?? "Unknown",
            r.StargazersCount, r.ForksCount, r.PushedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<GitHubCommitData>> GetCommitsSinceAsync(string accessToken, string repoFullName, DateTime since)
    {
        var request = CreateRequest(HttpMethod.Get,
            $"repos/{repoFullName}/commits?since={since:O}&per_page=100", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var commits = await response.Content.ReadFromJsonAsync<List<CommitResponse>>() ?? [];
        return commits.Select(c => new GitHubCommitData(
            c.Sha, c.Commit.Message, c.Commit.Author.Date, repoFullName
        )).ToList();
    }

    public async Task<IReadOnlyList<GitHubLanguageData>> GetLanguagesAsync(string accessToken, string repoFullName)
    {
        var request = CreateRequest(HttpMethod.Get, $"repos/{repoFullName}/languages", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var languages = await response.Content.ReadFromJsonAsync<Dictionary<string, long>>() ?? [];
        return languages.Select(kv => new GitHubLanguageData(kv.Key, kv.Value)).ToList();
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("GitPulse");
        return request;
    }

    private record RepoResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("full_name")] string FullName,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("stargazers_count")] int StargazersCount,
        [property: JsonPropertyName("forks_count")] int ForksCount,
        [property: JsonPropertyName("pushed_at")] DateTime PushedAt
    );

    private record CommitResponse(
        [property: JsonPropertyName("sha")] string Sha,
        [property: JsonPropertyName("commit")] CommitDetail Commit
    );

    private record CommitDetail(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("author")] CommitAuthor Author
    );

    private record CommitAuthor(
        [property: JsonPropertyName("date")] DateTime Date
    );
}
