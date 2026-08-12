namespace SeatPeek.Cineplexx.Rs.Api.Models;

public sealed record Cinema(
    string Id,
    string Name,
    string? UrlName,
    CinemaAddress? Address,
    GeoCoordinates? Coordinates);
