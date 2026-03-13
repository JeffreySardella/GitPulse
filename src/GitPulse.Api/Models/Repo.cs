namespace GitPulse.Api.Models;

public class Repo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string GitHubId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Language { get; set; } = "Unknown";
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
    public DateTime LastPushedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Commit> Commits { get; set; } = [];
}
