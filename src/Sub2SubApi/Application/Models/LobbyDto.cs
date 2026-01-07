namespace Sub2SubApi.Application.Models;

public sealed record LobbyDto(
    string LobbyId,
    string TournamentName,
    string StartsAtIso,
    TeamDto[] Teams
);

public sealed record TeamDto(
    string Name,
    SlotDto[] Slots
);

public sealed record SlotDto(
    string Role,
    string? DisplayName,
    string? AvatarUrl,
    int TopBidCredits
);
