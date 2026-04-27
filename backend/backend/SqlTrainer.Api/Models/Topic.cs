namespace SqlTrainer.Api.Models;

public class Topic
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>
    /// URL-barát azonosító (pl. "select-alapok"). Nem kötelező, de ajánlott.
    /// </summary>
    public string Slug { get; set; } = "";

    /// <summary>
    /// A téma leírása / tananyag markdownban.
    /// </summary>
    public string DescriptionMarkdown { get; set; } = "";

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }

    public List<TaskItem> Tasks { get; set; } = new();
}
