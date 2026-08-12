using System.Globalization;
using System.Net;
using System.Text.Json;

namespace SeatPeek.Cineplexx.Rs.Api.Tests.Upstream;

public sealed class CineplexxLiveApiContractTests
{
    private static readonly Uri ApiBaseAddress = new("https://app.cineplexx.rs/api/");

    [Trait("Category", "LiveApiContract")]
    [LiveApiFact]
    public async Task Cinema_first_flow_preserves_required_response_shapes()
    {
        using var client = new HttpClient();
        client.BaseAddress = ApiBaseAddress;
        client.Timeout = TimeSpan.FromSeconds(30);

        using var locationsResponse = await GetJsonAsync(client, "v1/locations");
        using var cinemasResponse = await GetJsonAsync(client, "v1/cinemas");
        var cinema = SelectCinema(locationsResponse.RootElement, cinemasResponse.RootElement);
        AssertCinemaSummary(cinema);
        var cinemaId = StringProperty(cinema, "id");

        using var cinemaResponse = await GetJsonAsync(client, $"v1/cinemas/{cinemaId}");
        AssertCinema(cinemaResponse.RootElement, cinemaId);

        using var programmeResponse = await GetJsonAsync(client, $"v1/cinemas/{cinemaId}/sessions");
        using var observation = await FindWorkingSessionAsync(client, Array(programmeResponse.RootElement), cinemaId);

        AssertSession(observation, cinemaId);
        AssertSeatPlan(observation.SeatPlanResponse.RootElement);

        if (RefreshSamplesEnabled)
        {
            LiveApiSampleWriter.Refresh(
                locationsResponse.RootElement,
                cinema,
                cinemaResponse.RootElement,
                observation);
        }
    }

    private static bool RefreshSamplesEnabled => string.Equals(
        Environment.GetEnvironmentVariable("UPDATE_LIVE_API_SAMPLES"),
        "true",
        StringComparison.OrdinalIgnoreCase);

    private static JsonElement SelectCinema(JsonElement locationsResponse, JsonElement cinemasResponse)
    {
        var locationCinemaIds = Array(locationsResponse).EnumerateArray()
            .Select(location =>
            {
                AssertLocation(location);
                return ArrayProperty(location, "items").EnumerateArray()
                    .Select(value => value.GetInt32().ToString(CultureInfo.InvariantCulture));
            })
            .SelectMany(ids => ids)
            .ToHashSet(StringComparer.Ordinal);

        var cinema = Array(cinemasResponse).EnumerateArray().FirstOrDefault(value =>
            value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty("id", out var id) &&
            id.ValueKind == JsonValueKind.String &&
            locationCinemaIds.Contains(id.GetString()!));

        Object(cinema);
        StringProperty(cinema, "id");
        StringProperty(cinema, "name");
        return cinema;
    }

    private static void AssertLocation(JsonElement location)
    {
        Object(location);
        NumberProperty(location, "id");
        StringProperty(location, "name");
        Assert.All(ArrayProperty(location, "items").EnumerateArray(), value =>
            Assert.Equal(JsonValueKind.Number, value.ValueKind));
    }

    private static void AssertCinemaSummary(JsonElement cinema)
    {
        Object(cinema);
        StringProperty(cinema, "id");
        StringProperty(cinema, "name");
        OptionalStringProperty(cinema, "cinemaUrlName");
    }

    private static void AssertCinema(JsonElement cinema, string cinemaId)
    {
        Object(cinema);
        Assert.Equal(cinemaId, StringProperty(cinema, "id"));
        StringProperty(cinema, "name");
        OptionalStringProperty(cinema, "cinemaUrlName");
        OptionalStringProperty(cinema, "address1");
        OptionalStringProperty(cinema, "address2");

        if (cinema.TryGetProperty("geo", out var geo) && geo.ValueKind != JsonValueKind.Null)
        {
            Object(geo);
            NumericProperty(geo, "latitude");
            NumericProperty(geo, "longitude");
        }
    }

    private static async Task<LiveApiObservation> FindWorkingSessionAsync(
        HttpClient client,
        JsonElement programme,
        string cinemaId)
    {
        var attempts = new List<string>();

        foreach (var day in programme.EnumerateArray())
        {
            Object(day);
            StringProperty(day, "date");

            foreach (var candidate in ArrayProperty(day, "sessions").EnumerateArray())
            {
                AssertProgrammeSession(candidate, cinemaId);
                var sessionKey = StringProperty(candidate, "id");

                var sessionAttempt = await TryGetJsonAsync(client, $"v1/sessions/{sessionKey}");
                if (sessionAttempt.Document is null)
                {
                    attempts.Add($"{sessionKey}: session details returned {(int)sessionAttempt.StatusCode}");
                    continue;
                }

                var sessionId = StringProperty(ObjectProperty(sessionAttempt.Document.RootElement, "session"), "sessionId");
                var seatPlanAttempt = await TryGetJsonAsync(client, $"v1/seat-plan/{cinemaId}/{sessionId}");
                if (seatPlanAttempt.Document is null)
                {
                    sessionAttempt.Document.Dispose();
                    attempts.Add($"{sessionKey}: seat plan returned {(int)seatPlanAttempt.StatusCode}");
                    continue;
                }

                return new LiveApiObservation(
                    sessionKey,
                    sessionId,
                    day.Clone(),
                    candidate.Clone(),
                    sessionAttempt.Document,
                    seatPlanAttempt.Document);
            }
        }

        throw new Xunit.Sdk.XunitException(
            "No currently returned session provided both session details and a seat plan with HTTP 200. " +
            $"Attempts: {string.Join("; ", attempts)}");
    }

    private static void AssertProgrammeSession(JsonElement session, string cinemaId)
    {
        Object(session);
        var sessionId = StringProperty(session, "sessionId");
        Assert.Equal(cinemaId, StringProperty(session, "cinemaId"));
        Assert.Equal($"{cinemaId}-{sessionId}", StringProperty(session, "id"));
        StringProperty(session, "movieId");
        StringProperty(session, "showtime");
        StringProperty(session, "status");
        TechnologyGroups(ArrayProperty(session, "technologies"));
    }

    private static void AssertSession(LiveApiObservation observation, string cinemaId)
    {
        var response = observation.SessionResponse.RootElement;
        var session = ObjectProperty(response, "session");
        var scheduledFilm = ObjectProperty(response, "scheduledFilm");

        Assert.Equal(observation.SessionKey, StringProperty(session, "id"));
        Assert.Equal(cinemaId, StringProperty(session, "cinemaId"));
        Assert.Equal(observation.SessionId, StringProperty(session, "sessionId"));
        foreach (var property in new[] { "movieId", "showtime", "status", "screenName" })
        {
            StringProperty(session, property);
        }

        NumberProperty(session, "screenNumber");
        TechnologyGroups(ArrayProperty(session, "technologies"));
        // TODO: Derive availability from seat plans when the public client implements seat-plan support.
        // Cineplexx's aggregate values are useful hints but can be internally inconsistent.
        NumberProperty(session, "seatsAvailable");
        NumberProperty(session, "seatsTotal");
        Assert.All(ArrayProperty(session, "sessionAttributesNames").EnumerateArray(), ObjectString);
        KindProperty(session, "allowTicketSales", JsonValueKind.True, JsonValueKind.False);
        StringProperty(scheduledFilm, "title");
        StringProperty(scheduledFilm, "duration");
    }

    private static void AssertSeatPlan(JsonElement seatPlan)
    {
        var row = ArrayProperty(seatPlan, "rows").EnumerateArray().FirstOrDefault(value =>
            value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty("seats", out var seats) &&
            seats.ValueKind == JsonValueKind.Array && seats.GetArrayLength() > 0);

        Object(row);
        StringProperty(row, "physicalName");
        NumberProperty(row, "number");
        var seat = ArrayProperty(row, "seats")[0];
        Object(seat);
        StringProperty(seat, "id");
        foreach (var property in new[] { "status", "statusCalculated", "columnIndex" })
        {
            NumberProperty(seat, property);
        }

        var position = ObjectProperty(seat, "position");
        foreach (var property in new[] { "rowIndex", "columnIndex", "areaNumber" })
        {
            NumberProperty(position, property);
        }

    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET /{path} returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        return JsonDocument.Parse(body);
    }

    private static async Task<(HttpStatusCode StatusCode, JsonDocument? Document)> TryGetJsonAsync(
        HttpClient client,
        string path)
    {
        using var response = await client.GetAsync(path);
        return response.StatusCode == HttpStatusCode.OK
            ? (response.StatusCode, JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            : (response.StatusCode, null);
    }

    private static JsonElement Array(JsonElement value)
    {
        Assert.Equal(JsonValueKind.Array, value.ValueKind);
        Assert.NotEmpty(value.EnumerateArray());
        return value;
    }

    private static JsonElement ArrayProperty(JsonElement value, string property) =>
        KindProperty(value, property, JsonValueKind.Array);

    private static JsonElement ObjectProperty(JsonElement value, string property) =>
        KindProperty(value, property, JsonValueKind.Object);

    private static JsonElement KindProperty(JsonElement value, string property, params JsonValueKind[] kinds)
    {
        Assert.True(value.TryGetProperty(property, out var result), $"Missing '{property}'.");
        Assert.Contains(result.ValueKind, kinds);
        return result;
    }

    private static void Object(JsonElement value) => Assert.Equal(JsonValueKind.Object, value.ValueKind);

    private static string StringProperty(JsonElement value, string property) =>
        KindProperty(value, property, JsonValueKind.String).GetString()!;

    private static void OptionalStringProperty(JsonElement value, string property)
    {
        if (value.TryGetProperty(property, out var result))
        {
            Assert.Contains(result.ValueKind, new[] { JsonValueKind.String, JsonValueKind.Null });
        }
    }

    private static int NumberProperty(JsonElement value, string property) =>
        KindProperty(value, property, JsonValueKind.Number).GetInt32();

    private static double NumericProperty(JsonElement value, string property) =>
        KindProperty(value, property, JsonValueKind.Number).GetDouble();

    private static void ObjectString(JsonElement value) => Assert.Equal(JsonValueKind.String, value.ValueKind);

    private static void TechnologyGroups(JsonElement technologies) =>
        Assert.All(technologies.EnumerateArray(), group =>
        {
            Assert.Equal(JsonValueKind.Array, group.ValueKind);
            Assert.All(group.EnumerateArray(), ObjectString);
        });

}
