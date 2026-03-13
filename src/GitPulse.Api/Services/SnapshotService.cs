using GitPulse.Api.Data;
using GitPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Services;

public class SnapshotService : ISnapshotService
{
    private readonly GitPulseDbContext _db;

    public SnapshotService(GitPulseDbContext db) => _db = db;

    public async Task UpdateSnapshotsForUserAsync(int userId)
    {
        var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

        // Load commits for user in last 90 days, then group in memory
        // (DateOnly.FromDateTime is not supported by InMemory/some LINQ providers)
        var commits = await _db.Commits
            .Where(c => c.Repo.UserId == userId && c.AuthoredAt >= ninetyDaysAgo)
            .Select(c => new { c.AuthoredAt, c.RepoId })
            .ToListAsync();

        var commitsByDate = commits
            .GroupBy(c => DateOnly.FromDateTime(c.AuthoredAt))
            .Select(g => new
            {
                Date = g.Key,
                CommitCount = g.Count(),
                ActiveRepos = g.Select(c => c.RepoId).Distinct().Count()
            })
            .ToList();

        foreach (var day in commitsByDate)
        {
            var existing = await _db.DailySnapshots
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == day.Date);

            if (existing is not null)
            {
                existing.CommitCount = day.CommitCount;
                existing.ActiveRepos = day.ActiveRepos;
            }
            else
            {
                _db.DailySnapshots.Add(new DailySnapshot
                {
                    UserId = userId,
                    Date = day.Date,
                    CommitCount = day.CommitCount,
                    ActiveRepos = day.ActiveRepos
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}
