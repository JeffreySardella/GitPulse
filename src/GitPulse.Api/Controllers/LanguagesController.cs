using GitPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LanguagesController : ControllerBase
{
    private readonly GitPulseDbContext _db;
    public LanguagesController(GitPulseDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetLanguages()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var languages = await _db.Repos
            .Where(r => r.UserId == userId)
            .GroupBy(r => r.Language)
            .Select(g => new { Language = g.Key, RepoCount = g.Count() })
            .OrderByDescending(l => l.RepoCount)
            .ToListAsync();

        return Ok(languages);
    }
}
