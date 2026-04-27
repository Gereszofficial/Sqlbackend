using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/admin/topics")]
[Authorize(Roles = "Admin")]
public class AdminTopicsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminTopicsController(AppDbContext db) => _db = db;

    /// <summary>
    /// ZIP import: topic + multiple tasks without manual SQL typing.
    /// Expected structure (minimum):
    /// - topic.json (title, slug?, isPublished?) OR topic.md (markdown content)
    /// - topic.md (markdown content) optional if you provide descriptionMarkdown in topic.json
    /// - tasks/*.md + tasks/*.json pairs (json: title, isPublished?, starterSql?, seedSql?, expectedSql?, sqlMode?)
    ///
    /// Returns the created topic id.
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<CreateIdResponse>> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Hiányzó fájl.");
        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Csak .zip fájl tölthető fel.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        // Read topic.json (optional)
        TopicJson? topicJson = null;
        var topicJsonEntry = zip.Entries.FirstOrDefault(e => e.FullName.Equals("topic.json", StringComparison.OrdinalIgnoreCase));
        if (topicJsonEntry is not null)
        {
            topicJson = JsonSerializer.Deserialize<TopicJson>(await ReadZipText(topicJsonEntry, ct), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        // Read topic.md (optional)
        string topicMarkdown = "";
        var topicMdEntry = zip.Entries.FirstOrDefault(e => e.FullName.Equals("topic.md", StringComparison.OrdinalIgnoreCase));
        if (topicMdEntry is not null)
            topicMarkdown = await ReadZipText(topicMdEntry, ct);

        var title = topicJson?.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            title = "Importált téma";

        var description = topicJson?.DescriptionMarkdown;
        if (string.IsNullOrWhiteSpace(description))
            description = topicMarkdown;

        if (string.IsNullOrWhiteSpace(description))
            description = "";

        var isPublished = topicJson?.IsPublished ?? false;

        var topic = new Topic
        {
            Title = title,
            Slug = (topicJson?.Slug ?? "").Trim(),
            DescriptionMarkdown = description,
            IsPublished = isPublished,
            PublishedAtUtc = isPublished ? DateTime.UtcNow : null
        };

        _db.Topics.Add(topic);
        await _db.SaveChangesAsync(ct);

        // Import tasks
        // We look for tasks/*.json and optionally matching .md by same base name.
        var taskJsonEntries = zip.Entries
            .Where(e => e.FullName.StartsWith("tasks/", StringComparison.OrdinalIgnoreCase)
                        && e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.FullName)
            .ToList();

        foreach (var tEntry in taskJsonEntries)
        {
            TaskJson? tj;
            try
            {
                tj = JsonSerializer.Deserialize<TaskJson>(await ReadZipText(tEntry, ct), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                // Skip invalid json to avoid breaking the whole import.
                continue;
            }

            if (tj is null) continue;

            // Optional markdown file with same base name
            var basePath = tEntry.FullName[..^5]; // remove .json
            var mdEntry = zip.Entries.FirstOrDefault(e => e.FullName.Equals(basePath + ".md", StringComparison.OrdinalIgnoreCase));
            var md = mdEntry is not null ? await ReadZipText(mdEntry, ct) : (tj.DescriptionMarkdown ?? "");

            var taskTitle = string.IsNullOrWhiteSpace(tj.Title) ? "Feladat" : tj.Title.Trim();
            var taskPublished = tj.IsPublished ?? false;

            var task = new TaskItem
            {
                TopicId = topic.Id,
                Category = "", // legacy
                Title = taskTitle,
                DescriptionMarkdown = md ?? "",
                StarterSql = tj.StarterSql ?? "",
                SeedSql = tj.SeedSql ?? "",
                ExpectedSql = tj.ExpectedSql ?? "",
                SqlMode = ParseSqlMode(tj.SqlMode),
                IsPublished = taskPublished,
                PublishedAtUtc = taskPublished ? DateTime.UtcNow : null
            };

            _db.Tasks.Add(task);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new CreateIdResponse(topic.Id));
    }

    private static async Task<string> ReadZipText(ZipArchiveEntry entry, CancellationToken ct)
    {
        await using var s = entry.Open();
        using var sr = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await sr.ReadToEndAsync(ct);
    }

    private static TaskSqlMode ParseSqlMode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return TaskSqlMode.SelectOnly;

        raw = raw.Trim();

        // Numeric support ("0"/"1")
        if (int.TryParse(raw, out var n) && Enum.IsDefined(typeof(TaskSqlMode), n))
            return (TaskSqlMode)n;

        // Common aliases
        if (raw.Equals("select", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("selectonly", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("select_only", StringComparison.OrdinalIgnoreCase))
            return TaskSqlMode.SelectOnly;

        if (raw.Equals("sandbox", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("sandboxsafe", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("sandbox_safe", StringComparison.OrdinalIgnoreCase))
            return TaskSqlMode.SandboxSafe;

        // Enum name support
        if (Enum.TryParse<TaskSqlMode>(raw, ignoreCase: true, out var parsed))
            return parsed;

        // Fallback
        return TaskSqlMode.SelectOnly;
    }

    private sealed record TopicJson(string? Title, string? Slug, bool? IsPublished, string? DescriptionMarkdown);
    private sealed record TaskJson(
        string? Title,
        bool? IsPublished,
        string? DescriptionMarkdown,
        string? StarterSql,
        string? SeedSql,
        string? ExpectedSql,
        string? SqlMode
    );

    [HttpGet]
    public async Task<ActionResult<List<AdminTopicListItem>>> List(CancellationToken ct)
    {
        return await _db.Topics
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new AdminTopicListItem(
                t.Id,
                t.Title,
                t.Slug,
                t.IsPublished,
                t.CreatedAtUtc,
                t.PublishedAtUtc,
                t.Tasks.Count
            ))
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<CreateIdResponse>> Create(AdminTopicUpsert req, CancellationToken ct)
    {
        var t = new Topic
        {
            Title = req.Title,
            Slug = req.Slug ?? "",
            DescriptionMarkdown = req.DescriptionMarkdown,
            IsPublished = req.IsPublished,
            PublishedAtUtc = req.IsPublished ? DateTime.UtcNow : null
        };

        _db.Topics.Add(t);
        await _db.SaveChangesAsync(ct);
        return new CreateIdResponse(t.Id);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AdminTopicWithTasks>> Get(long id, CancellationToken ct)
    {
        var topic = await _db.Topics
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (topic is null) return NotFound();

        var tasks = topic.Tasks
            .OrderBy(x => x.Id)
            .Select(x => new AdminTopicTaskUpsert(
                x.Id,
                x.Title,
                x.DescriptionMarkdown,
                x.StarterSql,
                x.SeedSql,
                x.ExpectedSql,
                x.SqlMode,
                x.IsPublished
            ))
            .ToList();

        return Ok(new AdminTopicWithTasks(
            topic.Id,
            topic.Title,
            topic.Slug,
            topic.DescriptionMarkdown,
            topic.IsPublished,
            tasks
        ));
    }

    /// <summary>
    /// Téma + feladatok mentése egyben (a tanárnak ne kelljen "kódolni").
    /// A feladatok listája: meglévő Id-vel update, Id nélkül create.
    /// A topic-hoz nem tartozó régi feladatok nem érintettek.
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult> Upsert(long id, AdminTopicWithTasks req, CancellationToken ct)
    {
        var topic = await _db.Topics
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (topic is null) return NotFound();

        topic.Title = req.Title;
        topic.Slug = req.Slug ?? "";
        topic.DescriptionMarkdown = req.DescriptionMarkdown;

        if (!topic.IsPublished && req.IsPublished)
            topic.PublishedAtUtc = DateTime.UtcNow;
        topic.IsPublished = req.IsPublished;

        // sync tasks
        var incomingIds = req.Tasks.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();

        // delete removed
        var toRemove = topic.Tasks.Where(x => !incomingIds.Contains(x.Id)).ToList();
        foreach (var r in toRemove)
        {
            // keep submissions? If deleting a task with submissions is desired, remove will cascade.
            _db.Tasks.Remove(r);
        }

        foreach (var t in req.Tasks)
        {
            if (t.Id is null)
            {
                var nt = new TaskItem
                {
                    TopicId = topic.Id,
                    Category = "", // legacy mező, topic módban nem használjuk
                    Title = t.Title,
                    DescriptionMarkdown = t.DescriptionMarkdown,
                    StarterSql = t.StarterSql,
                    SeedSql = t.SeedSql,
                    ExpectedSql = t.ExpectedSql,
                    SqlMode = t.SqlMode,
                    IsPublished = t.IsPublished,
                    PublishedAtUtc = t.IsPublished ? DateTime.UtcNow : null
                };
                topic.Tasks.Add(nt);
            }
            else
            {
                var et = topic.Tasks.FirstOrDefault(x => x.Id == t.Id.Value);
                if (et is null) continue;

                et.Title = t.Title;
                et.DescriptionMarkdown = t.DescriptionMarkdown;
                et.StarterSql = t.StarterSql;
                et.SeedSql = t.SeedSql;
                et.ExpectedSql = t.ExpectedSql;
                et.SqlMode = t.SqlMode;

                if (!et.IsPublished && t.IsPublished)
                    et.PublishedAtUtc = DateTime.UtcNow;
                et.IsPublished = t.IsPublished;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var topic = await _db.Topics
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (topic is null) return NotFound();

        // remove tasks explicitly for clarity
        _db.Tasks.RemoveRange(topic.Tasks);
        _db.Topics.Remove(topic);

        await _db.SaveChangesAsync(ct);
        return Ok();
    }
}
