using System.Text.Json.Serialization;

namespace SeatPeek.Cineplexx.Rs.Api.Transport;

internal sealed class CinemaSummaryDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("cinemaUrlName")]
    public string? CinemaUrlName { get; init; }
}
