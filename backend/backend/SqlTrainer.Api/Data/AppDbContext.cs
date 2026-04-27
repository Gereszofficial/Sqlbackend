using Microsoft.EntityFrameworkCore;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>()
            .HasIndex(x => x.Email).IsUnique();

        b.Entity<Submission>()
            .HasOne(x => x.Review)
            .WithOne(x => x.Submission)
            .HasForeignKey<Review>(x => x.SubmissionId);

        b.Entity<TaskItem>().Property(x => x.SeedSql).HasColumnType("LONGTEXT");
        b.Entity<TaskItem>().Property(x => x.ExpectedSql).HasColumnType("LONGTEXT");
        b.Entity<TaskItem>().Property(x => x.StarterSql).HasColumnType("LONGTEXT");
        b.Entity<TaskItem>().Property(x => x.Category).HasMaxLength(120);
        b.Entity<Submission>().Property(x => x.StudentSql).HasColumnType("LONGTEXT");
        b.Entity<Submission>().Property(x => x.StudentResultJson).HasColumnType("LONGTEXT");
        b.Entity<Submission>().Property(x => x.ExpectedResultJson).HasColumnType("LONGTEXT");
        b.Entity<TaskItem>().Property(x => x.DescriptionMarkdown).HasColumnType("LONGTEXT");
        b.Entity<Review>().Property(x => x.Comment).HasColumnType("LONGTEXT");

        b.Entity<Topic>().Property(x => x.DescriptionMarkdown).HasColumnType("LONGTEXT");
        b.Entity<Topic>().Property(x => x.Slug).HasMaxLength(160);

        b.Entity<TaskItem>()
            .HasOne(t => t.Topic)
            .WithMany(x => x.Tasks)
            .HasForeignKey(t => t.TopicId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
