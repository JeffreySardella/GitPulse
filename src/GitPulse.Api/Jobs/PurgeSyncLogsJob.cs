using GitPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Jobs;

public class PurgeSyncLogsJob
{
    private readonly GitPulseDbContext _db;
    private readonly ILogger<PurgeSyncLogsJob> _logger;

    public PurgeSyncLogsJob(GitPulseDbContext db, ILogger<PurgeSyncLogsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var deleted = await _db.SyncLogs
            .Where(l => l.StartedAt < cutoff)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Purged {Count} sync log entries older than 30 days", deleted);
    }
}
