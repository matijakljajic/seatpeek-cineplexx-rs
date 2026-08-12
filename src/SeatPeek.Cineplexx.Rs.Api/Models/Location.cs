namespace SeatPeek.Cineplexx.Rs.Api.Models;

public sealed record Location(
    int Id,
    string Name,
    IReadOnlyList<string> CinemaIds);
