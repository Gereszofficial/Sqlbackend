using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/student/topics")]
[Authorize(Roles = "Student")]
public class StudentTopicsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StudentTopicsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TopicListItem>>> List(CancellationToken ct)
    {
        var idStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(idStr) || !long.TryParse(idStr, out var userId))
            return Unauthorized("Token userId hiányzik vagy nem szám.");

        var completedTaskIds = (await _db.Submissions.AsNoTracking()
                .Where(s => s.UserId == userId
                            && s.Status == SubmissionStatus.Submitted
                            && s.IsCorrect == true)
                .Select(s => s.TaskItemId)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        var topics = await _db.Topics.AsNoTracking()
            .Where(t => t.IsPublished)
            .OrderByDescending(t => t.PublishedAtUtc ?? t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Slug,
                TaskIds = t.Tasks.Where(x => x.IsPublished).Select(x => x.Id).ToList()
            })
            .ToListAsync(ct);

        var items = topics.Select(t =>
        {
            var all = t.TaskIds.Count;
            var done = t.TaskIds.Count(id => completedTaskIds.Contains(id));
            // "IsCompleted" itt azt jelenti: minden published feladat kész
            var isCompleted = all > 0 && done == all;
            return new TopicListItem(t.Id, t.Title, t.Slug, isCompleted);
        }).ToList();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TopicDetails>> Get(long id, CancellationToken ct)
    {
        var idStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(idStr) || !long.TryParse(idStr, out var userId))
            return Unauthorized("Token userId hiányzik vagy nem szám.");

        var completedTaskIds = (await _db.Submissions.AsNoTracking()
                .Where(s => s.UserId == userId
                            && s.Status == SubmissionStatus.Submitted
                            && s.IsCorrect == true)
                .Select(s => s.TaskItemId)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        var topic = await _db.Topics.AsNoTracking()
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsPublished, ct);

        if (topic is null) return NotFound();

        var tasks = topic.Tasks
            .Where(x => x.IsPublished)
            .OrderBy(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new TopicTaskListItem(x.Id, x.Title, completedTaskIds.Contains(x.Id)))
            .ToList();

        return Ok(new TopicDetails(topic.Id, topic.Title, topic.Slug, topic.DescriptionMarkdown, tasks));
    }
}
