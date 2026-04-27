namespace SqlTrainer.Api.Dtos;

public record RankBadgeDto(string Key, string Label, string Tone);

public record StudentProgressDto(
    int CompletedCount,
    int TotalTasks,
    int Percent,
    RankBadgeDto Badge
);
