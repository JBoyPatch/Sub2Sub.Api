namespace Sub2SubApi.Application.Models;

public sealed class UserRankedStatsDto
{
    public required string UserId { get; init; }
    public required string QueueType { get; init; }
    public required string Tier { get; init; }
    public required string Rank { get; init; }
    public required int LeaguePoints { get; init; }
    public required int Wins { get; init; }
    public required int Losses { get; init; }
    public long? LastSyncedAtEpoch { get; init; }
}
