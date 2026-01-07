namespace Sub2SubApi.Application.Models;

public sealed record BidResponse(
    bool Accepted,
    bool DidBecomeTopBidder,
    int CurrentTopBidCredits,
    int QueuePosition
);
