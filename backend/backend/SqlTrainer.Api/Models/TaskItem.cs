namespace SqlTrainer.Api.Models;

public enum TaskSqlMode
{
    /// <summary>Csak SELECT engedélyezett (alap mód).</summary>
    SelectOnly = 0,
    /// <summary>
    /// Sandbox-ban biztonságos DDL/DML is engedélyezett (pl. CREATE TABLE, INSERT).
    /// A runner továbbra is tiltja a veszélyes parancsokat (pl. DROP DATABASE, CREATE USER, GRANT, stb.).
    /// </summary>
    SandboxSafe = 1
}

public class TaskItem
{
    public long Id { get; set; }

    /// <summary>
    /// Opcionális téma azonosító. Ha null, akkor a feladat "önálló" (legacy mód).
    /// </summary>
    public long? TopicId { get; set; }
    public Topic? Topic { get; set; }

    /// <summary>Opcionális csoport / fejezet (pl. "Törpék").</summary>
    public string Category { get; set; } = "";

    public string Title { get; set; } = null!;
    public string DescriptionMarkdown { get; set; } = "";

    /// <summary>Opcionális kezdő kód, amit a diák a szerkesztőben alapból lát.</summary>
    public string StarterSql { get; set; } = "";

    /// <summary>A sandbox seed (DDL + adatok).</summary>
    public string SeedSql { get; set; } = "";

    /// <summary>Az elvárt (helyes) SQL, aminek az eredménye a referenciának számít.</summary>
    public string ExpectedSql { get; set; } = "";

    public TaskSqlMode SqlMode { get; set; } = TaskSqlMode.SelectOnly;

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }

    public List<Submission> Submissions { get; set; } = new();
}
