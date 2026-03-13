namespace GitPulse.Api.Models;

public class DailySnapshot
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public int CommitCount { get; set; }
    public int ActiveRepoCount { get; set; }

    public User User { get; set; } = null!;
}
