namespace Sub2SubApi.Application.Models;

public sealed record MatchResultDto(
    bool DidWin,
    string? WonRole,
    string? MatchCode
);
