namespace SeatPeek.Cineplexx.Rs.Api.Models;

public sealed record CinemaAddress(
    string? Line1,
    string? Line2)
{
    public string Formatted => string.Join("\n", new[] { Line1, Line2 }.Where(line => !string.IsNullOrWhiteSpace(line)));
}
