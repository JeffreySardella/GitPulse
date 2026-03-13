namespace GitPulse.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string GitHubId { get; set; }
    public required string Login { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Repo> Repos { get; set; } = [];
}
