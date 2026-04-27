using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/admin/submissions")]
[Authorize(Roles = "Admin")]
public class AdminSubmissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminSubmissionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] long? taskId, CancellationToken ct)
    {
        var q = _db.Submissions
            .Include(s => s.User)
            .Include(s => s.TaskItem)
            .Include(s => s.Review)
            .AsQueryable();

        if (taskId is not null) q = q.Where(s => s.TaskItemId == taskId);

        var data = await q
            .OrderByDescending(s => s.SubmittedAtUtc)
            .Select(s => new
            {
                s.Id,
                TaskId = s.TaskItemId,
                StudentEmail = s.User.Email,
                TaskTitle = s.TaskItem.Title,
                s.IsCorrect,
                s.RunnerMessage,
                s.SubmittedAtUtc,
                Sql = s.StudentSql,
                Score = s.Review == null ? (int?)null : s.Review.Score,
                Comment = s.Review == null ? null : s.Review.Comment,
                ReviewedAtUtc = s.Review == null ? (DateTime?)null : s.Review.ReviewedAtUtc
            })
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("{submissionId:long}")]
    public async Task<ActionResult> GetById(long submissionId, CancellationToken ct)
    {
        var s = await _db.Submissions
            .Include(x => x.User)
            .Include(x => x.TaskItem)
            .Include(x => x.Review)
            .FirstOrDefaultAsync(x => x.Id == submissionId, ct);

        if (s is null) return NotFound();

        return Ok(new
        {
            s.Id,
            TaskId = s.TaskItemId,
            TaskTitle = s.TaskItem.Title,
            StudentEmail = s.User.Email,
            s.SubmittedAtUtc,
            s.Status,
            Sql = s.StudentSql,
            s.IsCorrect,
            s.RunnerMessage,
            s.StudentResultJson,
            s.ExpectedResultJson,
            Review = s.Review == null ? null : new { s.Review.Score, s.Review.Comment, s.Review.ReviewedAtUtc }
        });
    }

    [HttpPost("{submissionId:long}/review")]
    public async Task<ActionResult> Review(long submissionId, ReviewRequest req, CancellationToken ct)
    {
        var sub = await _db.Submissions.Include(s => s.Review).FirstOrDefaultAsync(s => s.Id == submissionId, ct);
        if (sub is null) return NotFound();

        var adminId = 0L;

        if (sub.Review is null)
        {
            sub.Review = new Review
            {
                SubmissionId = sub.Id,
                Score = req.Score,
                Comment = req.Comment,
                ReviewedByUserId = adminId,
                ReviewedAtUtc = DateTime.UtcNow
            };
        }
        else
        {
            sub.Review.Score = req.Score;
            sub.Review.Comment = req.Comment;
            sub.Review.ReviewedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }
}
