namespace SqlTrainer.Api.Models;

public enum SubmissionStatus { Draft = 0, Ran = 1, Submitted = 2 }

public class Submission
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public long TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;

    public string StudentSql { get; set; } = "";

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;

    public bool? IsCorrect { get; set; }
    public string RunnerMessage { get; set; } = "";
    public string? StudentResultJson { get; set; }
    public string? ExpectedResultJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }

    public Review? Review { get; set; }
}
