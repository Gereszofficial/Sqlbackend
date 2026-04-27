using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/admin/tasks")]
[Authorize(Roles = "Admin")]
public class AdminTasksController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminTasksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<AdminTaskListItem>>> List(CancellationToken ct)
    {
        return await _db.Tasks
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new AdminTaskListItem(
                t.Id,
                t.Title,
                t.Category,
                t.IsPublished,
                t.CreatedAtUtc,
                t.PublishedAtUtc
            ))
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<CreateIdResponse>> Create(AdminTaskUpsert req, CancellationToken ct)
    {
        var t = new TaskItem
        {
            Category = req.Category,
            Title = req.Title,
            DescriptionMarkdown = req.DescriptionMarkdown,
            StarterSql = req.StarterSql,
            SeedSql = req.SeedSql,
            ExpectedSql = req.ExpectedSql,
            SqlMode = req.SqlMode,
            IsPublished = req.IsPublished,
            PublishedAtUtc = req.IsPublished ? DateTime.UtcNow : null
        };

        _db.Tasks.Add(t);
        await _db.SaveChangesAsync(ct);
        return new CreateIdResponse(t.Id);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Update(long id, AdminTaskUpsert req, CancellationToken ct)
    {
        var t = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        t.Title = req.Title;
        t.Category = req.Category;
        t.DescriptionMarkdown = req.DescriptionMarkdown;
        t.StarterSql = req.StarterSql;
        t.SeedSql = req.SeedSql;
        t.ExpectedSql = req.ExpectedSql;
        t.SqlMode = req.SqlMode;

        if (!t.IsPublished && req.IsPublished)
            t.PublishedAtUtc = DateTime.UtcNow;

        t.IsPublished = req.IsPublished;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AdminTaskUpsert>> GetById(long id, CancellationToken ct)
    {
        var t = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        return new AdminTaskUpsert(
            t.Category,
            t.Title,
            t.DescriptionMarkdown,
            t.StarterSql,
            t.SeedSql,
            t.ExpectedSql,
            t.SqlMode,
            t.IsPublished
        );
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var t = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        _db.Tasks.Remove(t);
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

}
