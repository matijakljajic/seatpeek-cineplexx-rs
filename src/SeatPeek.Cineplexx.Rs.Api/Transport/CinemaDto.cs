using System.Text.Json.Serialization;

namespace SeatPeek.Cineplexx.Rs.Api.Transport;

internal sealed class CinemaDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("cinemaUrlName")]
    public string? CinemaUrlName { get; init; }

    [JsonPropertyName("address1")]
    public string? Address1 { get; init; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; init; }

    [JsonPropertyName("geo")]
    public GeoCoordinatesDto? Geo { get; init; }
}
