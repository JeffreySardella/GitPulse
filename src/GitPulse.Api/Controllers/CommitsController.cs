using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommitsController : ControllerBase
{
    private readonly GitPulseDbContext _db;
    public CommitsController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCommits([FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var commits = await _db.Commits
            .Where(c => c.Repo.UserId == userId)
            .OrderByDescending(c => c.AuthoredAt)
            .Take(limit)
            .Select(c => new { c.Sha, c.Message, c.AuthoredAt, RepoName = c.Repo.Name })
            .ToListAsync();

        return Ok(commits);
    }
}
