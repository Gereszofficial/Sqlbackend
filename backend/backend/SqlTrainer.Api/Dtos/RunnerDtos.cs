namespace SqlTrainer.Api.Dtos;

public record RunSqlRequest(string Sql);
public record RunSqlResponse(bool Ok, bool? IsCorrect, string Message, string? StudentResultJson, string? ExpectedResultJson);

public record SubmitRequest(string Sql);
public record SubmitResponse(long SubmissionId, bool? IsCorrect, string Message);
