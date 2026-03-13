namespace GitPulse.Api.Models;

public class Repo
{
    public int Id { get; set; }
    public long GitHubId { get; set; }
    public required string Name { get; set; }
    public required string FullName { get; set; }
    public string Language { get; set; } = "Unknown";
    public int Stars { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Commit> Commits { get; set; } = [];
}
