using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReposController : ControllerBase
{
    private readonly GitPulseDbContext _db;
    public ReposController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetRepos()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var repos = await _db.Repos
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Stars)
            .Select(r => new
            {
                r.Name,
                r.FullName,
                r.Language,
                r.Stars,
                CommitCount = r.Commits.Count
            })
            .ToListAsync();

        return Ok(repos);
    }
}
