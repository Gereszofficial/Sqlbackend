using SqlTrainer.Api.Models;
using System;

namespace SqlTrainer.Api.Dtos;

// Student feladat lista elem (a frontend isCompleted mezőt vár a pipához)
// + topic mezők: hogy a UI el tudja különíteni a témás vs manuális feladatokat.
public record TaskListItem(
    long Id,
    string Title,
    string Category,
    bool IsCompleted,
    long? TopicId,
    string? TopicTitle
);

public record AdminTaskListItem(
    long Id,
    string Title,
    string Category,
    bool IsPublished,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc
);

public record CreateIdResponse(long Id);

public record TaskDetails(
    long Id,
    string Title,
    string Category,
    string DescriptionMarkdown,
    string StarterSql,
    TaskSqlMode SqlMode
);

public record AdminTaskUpsert(
    string Category,
    string Title,
    string DescriptionMarkdown,
    string StarterSql,
    string SeedSql,
    string ExpectedSql,
    TaskSqlMode SqlMode,
    bool IsPublished
);

public record ColumnSchema(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey = false,
    bool IsForeignKey = false,
    bool IsIndexed = false
);
public record TableSchema(string Name, List<ColumnSchema> Columns);
public record TaskSchemaResponse(List<TableSchema> Tables);

public record PreviewColumn(
    string Name,
    bool IsPrimaryKey = false,
    bool IsForeignKey = false,
    bool IsIndexed = false
);
public record TablePreview(string Name, List<PreviewColumn> Columns, List<Dictionary<string, object?>> Rows);
public record TaskPreviewResponse(List<TablePreview> Tables);

public record ExpectedResultResponse(List<Dictionary<string, object?>> Rows);
