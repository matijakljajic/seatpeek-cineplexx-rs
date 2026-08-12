using System.Net.Http.Json;
using System.Text.Json;
using SeatPeek.Cineplexx.Rs.Api.Mapping;
using SeatPeek.Cineplexx.Rs.Api.Models;
using SeatPeek.Cineplexx.Rs.Api.Transport;

namespace SeatPeek.Cineplexx.Rs.Api;

public sealed class CineplexxApiClient
{
    private static readonly Uri DefaultBaseAddress = new("https://app.cineplexx.rs/api/");
    private readonly HttpClient _httpClient;

    public CineplexxApiClient(HttpClient httpClient)
    {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this._httpClient.BaseAddress ??= DefaultBaseAddress;
    }

    public async Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var locations = await GetRequiredJsonAsync<List<LocationDto>>("v1/locations", cancellationToken);
        return [.. locations.Select(CineplexxMapper.ToLocation)];
    }

    public async Task<IReadOnlyList<CinemaSummary>> GetCinemasAsync(CancellationToken cancellationToken = default)
    {
        var cinemas = await GetRequiredJsonAsync<List<CinemaSummaryDto>>("v1/cinemas", cancellationToken);
        return [.. cinemas.Select(CineplexxMapper.ToCinemaSummary)];
    }

    public async Task<Cinema> GetCinemaAsync(string cinemaId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cinemaId);

        var cinema = await GetRequiredJsonAsync<CinemaDto>(
            $"v1/cinemas/{Uri.EscapeDataString(cinemaId)}",
            cancellationToken);
        return CineplexxMapper.ToCinema(cinema);
    }

    private async Task<T> GetRequiredJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return value ?? throw new JsonException($"Cineplexx response for '{path}' was empty.");
    }
}
