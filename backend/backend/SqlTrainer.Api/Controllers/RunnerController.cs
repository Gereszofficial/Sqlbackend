using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;
using SqlTrainer.Api.Services;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/student/tasks/{taskId:long}")]
[Authorize(Roles = "Student")]
public class RunnerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISqlRunnerService _runner;
    private readonly IConfiguration _cfg;

    public RunnerController(AppDbContext db, ISqlRunnerService runner, IConfiguration cfg)
    {
        _db = db;
        _runner = runner;
        _cfg = cfg;
    }

    [HttpPost("run")]
    public async Task<ActionResult<RunSqlResponse>> Run(long taskId, RunSqlRequest req, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId && x.IsPublished, ct);
        if (task is null) return NotFound("Nincs ilyen publikált feladat.");

        // Use the same connection string key as the rest of the API.
        var sandboxConn = _cfg.GetConnectionString("Sandbox")!;
        var (ok, isCorrect, msg, studentJson, expectedJson) =
            await _runner.RunAndCompareAsync(sandboxConn, task.SeedSql, req.Sql, task.ExpectedSql, task.SqlMode, ct);

        return new RunSqlResponse(ok, isCorrect, msg, studentJson, expectedJson);
    }

    // NOTE: Schema endpoint is served by StudentTasksController at:
    // GET /api/student/tasks/{id}/schema
    // Keeping a second schema endpoint here causes ambiguous route matches.

    [HttpPost("submit")]
    public async Task<ActionResult<SubmitResponse>> Submit(long taskId, SubmitRequest req, CancellationToken ct)
    {
        // NOTE:
        // JwtSecurityTokenHandler may map standard JWT claims (like "sub") to
        // ClaimTypes.NameIdentifier by default. So we must accept BOTH.
        var idStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(idStr) || !long.TryParse(idStr, out var userId))
            return Unauthorized("Token userId hiányzik vagy nem szám.");

        var task = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId && x.IsPublished, ct);
        if (task is null) return NotFound("Nincs ilyen publikált feladat.");

        // Use the same connection string key as the rest of the API.
        var sandboxConn = _cfg.GetConnectionString("Sandbox")!;
        var (ok, isCorrect, msg, studentJson, expectedJson) =
            await _runner.RunAndCompareAsync(sandboxConn, task.SeedSql, req.Sql, task.ExpectedSql, task.SqlMode, ct);

        // If the runner errored, still save the submission, but mark it as incorrect
        // and surface the runner error message.
        var finalIsCorrect = ok && (isCorrect ?? false);
        var finalMessage = ok ? msg : $"Futtatási hiba: {msg}";

        var sub = new Submission
        {
            UserId = userId,
            TaskItemId = taskId,
            StudentSql = req.Sql,
            Status = SubmissionStatus.Submitted,
            SubmittedAtUtc = DateTime.UtcNow,
            IsCorrect = finalIsCorrect,
            RunnerMessage = finalMessage,
            StudentResultJson = studentJson,
            ExpectedResultJson = expectedJson
        };

        _db.Submissions.Add(sub);
        await _db.SaveChangesAsync(ct);

        return new SubmitResponse(sub.Id, sub.IsCorrect, finalMessage);
    }
}
