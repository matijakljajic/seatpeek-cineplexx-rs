using System.Globalization;
using System.Text.Json;
using SeatPeek.Cineplexx.Rs.Api.Models;
using SeatPeek.Cineplexx.Rs.Api.Transport;

namespace SeatPeek.Cineplexx.Rs.Api.Mapping;

internal static class CineplexxMapper
{
    public static Location ToLocation(LocationDto source) => new(
        Required(source.Id, "location.id"),
        Required(source.Name, "location.name"),
        [
            .. Required(source.Items, "location.items")
                .Select(id => id.ToString(CultureInfo.InvariantCulture))
        ]);

    public static CinemaSummary ToCinemaSummary(CinemaSummaryDto source) => new(
        Required(source.Id, "cinema.id"),
        Required(source.Name, "cinema.name"),
        EmptyToNull(source.CinemaUrlName));

    public static Cinema ToCinema(CinemaDto source) => new(
        Required(source.Id, "cinema.id"),
        Required(source.Name, "cinema.name"),
        EmptyToNull(source.CinemaUrlName),
        ToAddress(source),
        source.Geo is null ? null : ToCoordinates(source.Geo));

    private static CinemaAddress? ToAddress(CinemaDto source)
    {
        var line1 = EmptyToNull(source.Address1);
        var line2 = EmptyToNull(source.Address2);
        return line1 is null && line2 is null ? null : new CinemaAddress(line1, line2);
    }

    private static GeoCoordinates ToCoordinates(GeoCoordinatesDto source) => new(
        Required(source.Latitude, "cinema.geo.latitude"),
        Required(source.Longitude, "cinema.geo.longitude"));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw Missing(field);

    private static T Required<T>(T? value, string field) where T : class =>
        value ?? throw Missing(field);

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw Missing(field) : value;

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static JsonException Missing(string field) =>
        new($"Cineplexx response is missing required field '{field}'.");
}
