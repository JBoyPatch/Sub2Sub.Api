namespace Sub2SubApi.Application.Models;

public sealed class CreateLobbyRequest
{
    // Optional: if empty, server will generate an id
    public string? LobbyId { get; init; }

    public string? TournamentName { get; init; }

    // ISO 8601 timestamp string; optional
    public string? StartsAtIso { get; init; }
}
