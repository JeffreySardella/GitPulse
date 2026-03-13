namespace GitPulse.Api.Models;

public class Commit
{
    public int Id { get; set; }
    public int RepoId { get; set; }
    public required string Sha { get; set; }
    public required string Message { get; set; }
    public DateTime AuthoredAt { get; set; }

    public Repo Repo { get; set; } = null!;
}
