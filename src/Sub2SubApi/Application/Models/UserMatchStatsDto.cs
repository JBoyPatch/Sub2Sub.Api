namespace Sub2SubApi.Application.Models;

public sealed class UserMatchStatsDto
{
    public required string UserId { get; init; }
    public required string MatchId { get; init; }
    public required int ChampionId { get; init; }
    public required string ChampionName { get; init; }
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }
    public int CreepScore { get; init; }
    public int GoldEarned { get; init; }
    public long DamageToChampions { get; init; }
    public int VisionScore { get; init; }
    public bool Win { get; init; }
    public int QueueId { get; init; }
    public long RecordedAtEpoch { get; init; }
}
