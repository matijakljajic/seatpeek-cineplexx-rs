using System.Text.Json.Serialization;

namespace SeatPeek.Cineplexx.Rs.Api.Transport;

internal sealed class LocationDto
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("items")]
    public List<int>? Items { get; init; }
}
