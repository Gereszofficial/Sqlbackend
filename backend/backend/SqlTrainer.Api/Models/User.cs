namespace SqlTrainer.Api.Models;

public enum UserRole { Student = 0, Admin = 1 }

public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Student;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Submission> Submissions { get; set; } = new();
}
