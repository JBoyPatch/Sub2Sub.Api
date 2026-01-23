namespace Sub2SubApi.Application.Models;

public sealed class RiotProfileDto
{
    public required string UserId { get; init; }
    public required string RiotPuuid { get; init; }
    public required string RiotGameName { get; init; }
    public required string RiotTagline { get; init; }
    public string? RiotSummonerId { get; init; }
    public int? RiotProfileIconId { get; init; }
    public long? RiotSummonerLevel { get; init; }
    public long? LastRiotProfileSyncAtEpoch { get; init; }
}
