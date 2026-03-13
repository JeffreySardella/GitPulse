namespace GitPulse.Api.Models;

public class SyncLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Status { get; set; } // "success" | "failed"
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public User User { get; set; } = null!;
}
