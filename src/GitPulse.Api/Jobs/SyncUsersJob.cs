using GitPulse.Api.Data;
using GitPulse.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Jobs;

public class SyncUsersJob
{
    private readonly GitPulseDbContext _db;
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncUsersJob> _logger;

    public SyncUsersJob(GitPulseDbContext db, ISyncService syncService, ILogger<SyncUsersJob> logger)
    {
        _db = db;
        _syncService = syncService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var staleThreshold = DateTime.UtcNow.AddHours(-1);
        var staleUsers = await _db.Users
            .Where(u => u.LastSyncedAt < staleThreshold)
            .Select(u => u.Id)
            .ToListAsync();

        _logger.LogInformation("Found {Count} stale users to sync", staleUsers.Count);

        foreach (var userId in staleUsers)
        {
            try
            {
                await _syncService.SyncUserAsync(userId);
                _logger.LogInformation("Synced user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync user {UserId}", userId);
            }
        }
    }
}
