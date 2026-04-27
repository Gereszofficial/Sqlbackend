using System;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Dtos;

public record TopicListItem(long Id, string Title, string Slug, bool IsCompleted);

public record TopicTaskListItem(long Id, string Title, bool IsCompleted);

public record TopicDetails(
    long Id,
    string Title,
    string Slug,
    string DescriptionMarkdown,
    List<TopicTaskListItem> Tasks
);

public record AdminTopicListItem(
    long Id,
    string Title,
    string Slug,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    int TaskCount
);

public record AdminTopicUpsert(
    string Title,
    string Slug,
    string DescriptionMarkdown,
    bool IsPublished
);

public record AdminTopicTaskUpsert(
    long? Id,
    string Title,
    string DescriptionMarkdown,
    string StarterSql,
    string SeedSql,
    string ExpectedSql,
    TaskSqlMode SqlMode,
    bool IsPublished
);

public record AdminTopicWithTasks(
    long Id,
    string Title,
    string Slug,
    string DescriptionMarkdown,
    bool IsPublished,
    List<AdminTopicTaskUpsert> Tasks
);
