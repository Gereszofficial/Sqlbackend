using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Services;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/student/tasks")]
[Authorize(Roles = "Student")]
public class StudentTasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISqlRunnerService _sql;
    private readonly IConfiguration _cfg;

    public StudentTasksController(AppDbContext db, ISqlRunnerService sql, IConfiguration cfg)
    {
        _db = db;
        _sql = sql;
        _cfg = cfg;
    }

    // Lista a diák feladatokhoz (csak published)
    [HttpGet]
    public async Task<ActionResult<List<TaskListItem>>> List(CancellationToken ct)
    {
        // Token userId többféle claim név alatt is jöhet (framework claim mapping miatt).
        // Ugyanazt a robusztus logikát használjuk, mint a /submit endpointnál.
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

        var raw = await _db.Tasks.AsNoTracking()
            .Where(t => t.IsPublished)
            .OrderByDescending(t => t.PublishedAtUtc ?? t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Category,
                t.TopicId,
                TopicTitle = t.Topic != null ? t.Topic.Title : null
            })
            .ToListAsync(ct);

        var items = raw
            .Select(t => new TaskListItem(
                t.Id,
                t.Title,
                t.Category,
                completedTaskIds.Contains(t.Id),
                t.TopicId,
                t.TopicTitle
            ))
            .ToList();

        return Ok(items);
    }

    // Részletek (csak published)
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TaskDetails>> Get(long id, CancellationToken ct)
    {
        var t = await _db.Tasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished, ct);

        if (t is null) return NotFound();

        return Ok(new TaskDetails(
            t.Id,
            t.Title,
            t.Category,
            t.DescriptionMarkdown,
            t.StarterSql,
            t.SqlMode
        ));
    }

    // SÉMA (a seed sql alapján) — EZ VOLT NÁLAD 500
    [HttpGet("{id:long}/schema")]
    public async Task<ActionResult<TaskSchemaResponse>> Schema(long id, CancellationToken ct)
    {
        var t = await _db.Tasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished, ct);

        if (t is null) return NotFound();

        var sandboxConnStr = _cfg.GetConnectionString("Sandbox");
        if (string.IsNullOrWhiteSpace(sandboxConnStr))
            return Problem("Missing ConnectionStrings:Sandbox in appsettings.json");

        var schema = await _sql.BuildSchemaAsync(sandboxConnStr, t.SeedSql, ct);
        return Ok(schema);
    }

    [HttpGet("{id:long}/expected-result")]
    public async Task<ActionResult<ExpectedResultResponse>> ExpectedResult(long id, CancellationToken ct)
    {
        var t = await _db.Tasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished, ct);

        if (t is null) return NotFound();

        var sandboxConnStr = _cfg.GetConnectionString("Sandbox");
        if (string.IsNullOrWhiteSpace(sandboxConnStr))
            return Problem("Missing ConnectionStrings:Sandbox in appsettings.json");

        var (ok, _, msg, _, expectedJson) =
            await _sql.RunAndCompareAsync(sandboxConnStr, t.SeedSql, t.ExpectedSql, t.ExpectedSql, t.SqlMode, ct);

        if (!ok || string.IsNullOrWhiteSpace(expectedJson))
            return Problem($"Nem sikerült betölteni az elvárt eredményt: {msg}");

        try
        {
            var rows = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(expectedJson)
                       ?? new List<Dictionary<string, object?>>();
            return Ok(new ExpectedResultResponse(rows));
        }
        catch
        {
            return Problem("Az elvárt eredmény feldolgozása nem sikerült.");
        }
    }

    // TÁBLA ADAT ELŐNÉZET (a seed sql alapján) — a diák "rendes" táblát és adatokat lát
    [HttpGet("{id:long}/preview")]
    public async Task<ActionResult<TaskPreviewResponse>> Preview(long id, CancellationToken ct)
    {
        var t = await _db.Tasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished, ct);

        if (t is null) return NotFound();

        var sandboxConnStr = _cfg.GetConnectionString("Sandbox");
        if (string.IsNullOrWhiteSpace(sandboxConnStr))
            return Problem("Missing ConnectionStrings:Sandbox in appsettings.json");

        var preview = await _sql.BuildPreviewAsync(sandboxConnStr, t.SeedSql, ct);
        return Ok(preview);
    }
}
