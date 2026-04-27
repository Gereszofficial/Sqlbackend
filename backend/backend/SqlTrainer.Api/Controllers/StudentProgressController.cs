using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;
using SqlTrainer.Api.Services;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/student/progress")]
[Authorize(Roles = "Student")]
public class StudentProgressController : ControllerBase
{
    private readonly AppDbContext _db;
    public StudentProgressController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<StudentProgressDto>> Get(CancellationToken ct)
    {
        var idStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(idStr) || !long.TryParse(idStr, out var userId))
            return Unauthorized("Token userId hiányzik vagy nem szám.");

        var total = await _db.Tasks.AsNoTracking()
            .CountAsync(t => t.IsPublished, ct);

        var completedCount = await _db.Submissions.AsNoTracking()
            .Where(s => s.UserId == userId
                        && s.Status == SubmissionStatus.Submitted
                        && s.IsCorrect == true
                        && s.TaskItem.IsPublished)
            .Select(s => s.TaskItemId)
            .Distinct()
            .CountAsync(ct);

        var percent = total == 0 ? 0 : (int)Math.Round(100.0 * completedCount / total);
        var badge = RankBadge.ForPercent(percent);

        return Ok(new StudentProgressDto(completedCount, total, percent, badge));
    }
}
