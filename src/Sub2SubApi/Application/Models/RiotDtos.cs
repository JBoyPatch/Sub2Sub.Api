namespace Sub2SubApi.Application.Models;

public sealed class RiotAccountDto
{
    public required string Puuid { get; init; }
}

public sealed class RiotSummonerDto
{
    public required string SummonerId { get; init; }
    public required int ProfileIconId { get; init; }
    public required long SummonerLevel { get; init; }
}

public sealed class RiotRankedEntryDto
{
    public required string QueueType { get; init; }
    public required string Tier { get; init; }
    public required string Rank { get; init; }
    public required int LeaguePoints { get; init; }
    public required int Wins { get; init; }
    public required int Losses { get; init; }
}

public sealed class RiotChampionMasteryDto
{
    public required int ChampionId { get; init; }
    public required long ChampionPoints { get; init; }
    public required int ChampionLevel { get; init; }
    public long? LastPlayTime { get; init; }
    public bool? ChestGranted { get; init; }
}

public sealed class RiotMatchDto
{
    public required string MatchId { get; init; }
    public string? RawJson { get; init; }
}
