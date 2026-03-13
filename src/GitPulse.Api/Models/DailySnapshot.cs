namespace GitPulse.Api.Models;

public class DailySnapshot
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public int CommitCount { get; set; }
    public int ActiveRepos { get; set; }
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }

    public User User { get; set; } = null!;
}
