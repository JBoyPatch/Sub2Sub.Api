namespace Sub2SubApi.Application.Models;

public sealed record BidRequest(
    int TeamIndex,
    string Role,
    int Amount
);
