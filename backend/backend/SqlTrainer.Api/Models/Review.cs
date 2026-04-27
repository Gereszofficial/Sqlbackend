namespace SqlTrainer.Api.Models;

public class Review
{
    public long Id { get; set; }

    public long SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public int Score { get; set; } // 0..100
    public string Comment { get; set; } = "";

    public long ReviewedByUserId { get; set; }
    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}
