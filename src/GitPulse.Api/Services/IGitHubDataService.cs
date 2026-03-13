namespace GitPulse.Api.Services;

public record GitHubRepoData(string Id, string Name, string? Description, string Language, int Stars, int Forks, DateTime LastPushedAt);
public record GitHubCommitData(string Sha, string Message, DateTime AuthoredAt, string RepoName);
public record GitHubLanguageData(string Language, long Bytes);

public interface IGitHubDataService
{
    Task<IReadOnlyList<GitHubRepoData>> GetReposAsync(string accessToken);
    Task<IReadOnlyList<GitHubCommitData>> GetCommitsSinceAsync(string accessToken, string repoFullName, DateTime since);
    Task<IReadOnlyList<GitHubLanguageData>> GetLanguagesAsync(string accessToken, string repoFullName);
}
