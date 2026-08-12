using System.Text.Json.Serialization;

namespace SeatPeek.Cineplexx.Rs.Api.Transport;

internal sealed class GeoCoordinatesDto
{
    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }
}
