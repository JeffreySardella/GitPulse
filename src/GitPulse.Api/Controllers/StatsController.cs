using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly GitPulseDbContext _db;
    public StatsController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ninetyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));
        var snapshots = await _db.DailySnapshots
            .Where(s => s.UserId == userId && s.Date >= ninetyDaysAgo)
            .OrderBy(s => s.Date)
            .Select(s => new { s.Date, s.CommitCount, s.ActiveRepos })
            .ToListAsync();

        var totalCommits = snapshots.Sum(s => s.CommitCount);
        var totalRepos = await _db.Repos.CountAsync(r => r.UserId == userId);

        return Ok(new { TotalCommits = totalCommits, TotalRepos = totalRepos, DailySnapshots = snapshots });
    }
}
