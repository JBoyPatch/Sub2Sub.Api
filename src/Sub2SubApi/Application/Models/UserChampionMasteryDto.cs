namespace Sub2SubApi.Application.Models;

public sealed class UserChampionMasteryDto
{
    public required string UserId { get; init; }
    public required int ChampionId { get; init; }
    public required long ChampionPoints { get; init; }
    public required int ChampionLevel { get; init; }
    public long? LastPlayTimeEpoch { get; init; }
    public bool? ChestGranted { get; init; }
    public long? LastSyncedAtEpoch { get; init; }
}
