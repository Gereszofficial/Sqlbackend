using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Models;
using SqlTrainer.Api.Services;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/admin/rank")]
[Authorize(Roles = "Admin")]
public class AdminRankController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminRankController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken ct)
    {
        // Csak publikált feladatok számítanak a haladásnál
        var tasks = await _db.Tasks.AsNoTracking()
            .Where(t => t.IsPublished)
            .OrderBy(t => t.Id)
            .Select(t => new { t.Id, t.Title, t.Category })
            .ToListAsync(ct);

        var students = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Student)
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(ct);

        // Késznek azt tekintjük, ami: Submitted + IsCorrect == true
        var completedPairs = await _db.Submissions.AsNoTracking()
            .Where(s => s.Status == SubmissionStatus.Submitted && s.IsCorrect == true)
            .Select(s => new { s.UserId, s.TaskItemId })
            .Distinct()
            .ToListAsync(ct);

        var byUser = completedPairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TaskItemId).ToHashSet());

        var taskIdSet = tasks.Select(t => t.Id).ToHashSet();

        var rows = students.Select(s =>
        {
            byUser.TryGetValue(s.Id, out var doneAll);
            doneAll ??= new HashSet<long>();
            // Csak a publikált feladatok érdekesek
            var done = doneAll.Where(taskIdSet.Contains).ToHashSet();
            var completedCount = done.Count;
            var total = tasks.Count;
            var percent = total == 0 ? 0 : (int)Math.Round(100.0 * completedCount / total);
            var badge = RankBadge.ForPercent(percent);
            return new
            {
                studentId = s.Id,
                email = s.Email,
                completedTaskIds = done.OrderBy(x => x).ToList(),
                completedCount,
                totalTasks = total,
                percent,
                badge
            };
        }).ToList();

        // Rang: több kész -> előrébb, holtversenyben abc
        rows = rows
            .OrderByDescending(r => r.completedCount)
            .ThenBy(r => r.email)
            .ToList();

        return Ok(new { tasks, students = rows, generatedAtUtc = DateTime.UtcNow });
    }
}
