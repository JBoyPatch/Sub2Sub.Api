namespace Sub2SubApi.Application.Models;

public sealed class MatchDto
{
    public required string MatchId { get; init; }
    public long GameStartTimestamp { get; init; }
    public int QueueId { get; init; }
    public long GameDurationSeconds { get; init; }
    public string? RawJson { get; init; }
    public long CreatedAtEpoch { get; init; }
}
