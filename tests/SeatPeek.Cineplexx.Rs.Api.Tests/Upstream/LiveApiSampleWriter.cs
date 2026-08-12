using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SeatPeek.Cineplexx.Rs.Api.Tests.Upstream;

internal static class LiveApiSampleWriter
{
    public static void Refresh(
        JsonElement locations,
        JsonElement cinema,
        JsonElement cinemaDetails,
        LiveApiObservation observation)
    {
        var repositoryRoot = FindRepositoryRoot();
        var responsesDirectory = Path.Combine(repositoryRoot, "samples", "responses");
        Directory.CreateDirectory(responsesDirectory);

        var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var sessionResponse = observation.SessionResponse.RootElement;
        var session = Property(sessionResponse, "session");
        var scheduledFilm = Property(sessionResponse, "scheduledFilm");

        Write(Path.Combine(responsesDirectory, "locations.json"), Node(locations));
        Write(Path.Combine(responsesDirectory, "cinemas.json"),
            new JsonArray(Pick(cinema, "id", "name", "cinemaUrlName")));
        Write(Path.Combine(responsesDirectory, "cinema.json"),
            Pick(cinemaDetails, "id", "name", "cinemaUrlName", "address1", "address2", "geo"));
        Write(Path.Combine(responsesDirectory, $"cinema-sessions-{date}.json"), new JsonArray(Programme(observation)));
        var scheduledFilmSample = Pick(scheduledFilm, "id", "scheduledFilmId", "cinemaId", "title", "duration");
        scheduledFilmSample["slug"] = Node(Property(Property(scheduledFilm, "film"), "shortURL"));
        Write(Path.Combine(responsesDirectory, $"session-{date}.json"), new JsonObject
        {
            ["scheduledFilm"] = scheduledFilmSample,
            ["session"] = Pick(session,
                "id", "sessionId", "cinemaId", "movieId", "showtime", "status", "seatsAvailable",
                "seatsTotal", "screenName", "screenNumber", "sessionAttributesNames", "allowTicketSales",
                "technologies")
        });
        Write(Path.Combine(responsesDirectory, $"seat-plan-{date}.json"), SeatPlan(observation.SeatPlanResponse.RootElement));

        RemoveOldTimeSensitiveSamples(responsesDirectory, date);
        UpdateReadme(repositoryRoot, date, cinema, observation.SessionKey);
    }

    private static JsonObject Programme(LiveApiObservation observation)
    {
        var sessions = new JsonArray { Pick(observation.ProgrammeSession,
            "id", "cinemaId", "movieId", "sessionId", "screenName", "screenNumber", "technologies",
            "showtime", "isAllocatedSeating", "status") };

        var additionalSession = Property(observation.ProgrammeDay, "sessions").EnumerateArray()
            .FirstOrDefault(candidate => candidate.ValueKind == JsonValueKind.Object &&
                StringProperty(candidate, "id") != observation.SessionKey);
        if (additionalSession.ValueKind != JsonValueKind.Undefined)
        {
            sessions.Add(Pick(additionalSession,
                "id", "cinemaId", "movieId", "sessionId", "screenName", "screenNumber", "technologies",
                "showtime", "isAllocatedSeating", "status"));
        }

        var day = Pick(observation.ProgrammeDay, "date");
        day["sessions"] = sessions;
        return day;
    }

    private static JsonObject SeatPlan(JsonElement source)
    {
        var sourceRows = Property(source, "rows").EnumerateArray()
            .Where(row => row.ValueKind == JsonValueKind.Object)
            .ToList();
        var selectedRows = new List<JsonElement>();

        AddFirst(selectedRows, sourceRows, row => Seats(row).Any(HasGroup));
        AddFirst(selectedRows, sourceRows, row => Seats(row).Any(HasUnavailableStatus));
        AddFirst(selectedRows, sourceRows, _ => true);

        var rows = new JsonArray();
        foreach (var row in selectedRows)
        {
            var trimmed = Pick(row,
                "physicalName", "number", "areaCategoryCode", "description", "right", "bottom", "height",
                "columnCount");
            trimmed["seats"] = new JsonArray(RepresentativeSeats(Seats(row))
                .Select(seat => (JsonNode)Pick(seat,
                    "columnIndex", "statusCalculated", "doubleSeatId", "position", "id", "status", "seatStyle",
                    "seatsInGroup", "originalStatus", "seatIconId", "seatImprovedIconId", "areaCategoryCode",
                    "rowName", "normalizedRowIndex", "rowRight", "normalizedColumnIndex"))
                .ToArray());
            rows.Add(trimmed);
        }

        var iconIds = rows.OfType<JsonObject>()
            .SelectMany(row => row["seats"]!.AsArray())
            .OfType<JsonObject>()
            .Select(seat => seat["seatIconId"]?.GetValue<int>())
            .OfType<int>()
            .ToHashSet();
        var icons = source.TryGetProperty("icons", out var sourceIcons) && sourceIcons.ValueKind == JsonValueKind.Array
            ? new JsonArray(sourceIcons.EnumerateArray()
                .Where(icon => icon.TryGetProperty("id", out var id) && iconIds.Contains(id.GetInt32()))
                .Select(icon => (JsonNode)Pick(icon, "id", "imageUrl"))
                .ToArray())
            : new JsonArray();

        var result = Pick(source, "rowsMax", "seatPlanSaved");
        result["rows"] = rows;
        result["icons"] = icons;
        return result;
    }

    private static IEnumerable<JsonElement> Seats(JsonElement row) =>
        row.TryGetProperty("seats", out var seats) && seats.ValueKind == JsonValueKind.Array
            ? seats.EnumerateArray().ToArray()
            : Array.Empty<JsonElement>();

    private static IEnumerable<JsonElement> RepresentativeSeats(IEnumerable<JsonElement> source)
    {
        var seats = source.ToList();
        var selected = new List<JsonElement>();
        AddFirst(selected, seats, HasAvailableStatus);
        AddFirst(selected, seats, HasUnavailableStatus);
        AddFirst(selected, seats, HasGroup);
        return selected;
    }

    private static bool HasAvailableStatus(JsonElement seat) =>
        seat.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Number && status.GetInt32() == 0;

    private static bool HasUnavailableStatus(JsonElement seat) =>
        seat.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Number && status.GetInt32() != 0;

    private static bool HasGroup(JsonElement seat) =>
        seat.TryGetProperty("seatsInGroup", out var group) &&
        group.ValueKind == JsonValueKind.Array && group.GetArrayLength() > 0;

    private static void AddFirst(
        ICollection<JsonElement> target,
        IEnumerable<JsonElement> candidates,
        Func<JsonElement, bool> condition)
    {
        var match = candidates.FirstOrDefault(candidate =>
            target.All(existing => existing.GetRawText() != candidate.GetRawText()) && condition(candidate));
        if (match.ValueKind != JsonValueKind.Undefined)
        {
            target.Add(match);
        }
    }

    private static JsonObject Pick(JsonElement source, params string[] names)
    {
        var result = new JsonObject();
        foreach (var name in names)
        {
            if (source.TryGetProperty(name, out var value))
            {
                result[name] = Node(value);
            }
        }

        return result;
    }

    private static JsonElement Property(JsonElement source, string name)
    {
        if (!source.TryGetProperty(name, out var property))
        {
            throw new InvalidOperationException($"Observed response is missing '{name}'.");
        }

        return property;
    }

    private static string StringProperty(JsonElement source, string name) => Property(source, name).GetString()!;

    private static JsonNode Node(JsonElement value) => JsonNode.Parse(value.GetRawText())!;

    private static void Write(string path, JsonNode value) =>
        File.WriteAllText(path, value.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

    private static void RemoveOldTimeSensitiveSamples(string directory, string date)
    {
        var current = new HashSet<string>(StringComparer.Ordinal)
        {
            $"cinema-sessions-{date}.json", $"session-{date}.json", $"seat-plan-{date}.json"
        };

        foreach (var pattern in new[] { "cinema-sessions-*.json", "session-*.json", "seat-plan-*.json" })
        {
            foreach (var path in Directory.EnumerateFiles(directory, pattern).Where(path => !current.Contains(Path.GetFileName(path))))
            {
                File.Delete(path);
            }
        }
    }

    private static void UpdateReadme(string root, string date, JsonElement cinema, string sessionKey)
    {
        var path = Path.Combine(root, "samples", "README.md");
        var source = File.ReadAllText(path);
        var replacement = $"The responses were observed on **{date}** using cinema **{StringProperty(cinema, "id")}** " +
            $"(`{StringProperty(cinema, "name")}`) and session **{sessionKey}**.";
        var updated = Regex.Replace(source,
            @"The responses were observed on \*\*[^*]+\*\* using cinema \*\*[^*]+\*\* \(`[^`]+`\) and session \*\*[^*]+\*\*\.",
            _ => replacement);

        if (updated != source)
        {
            File.WriteAllText(path, updated);
        }
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (DirectoryInfo? directory = new(start); directory is not null; directory = directory.Parent)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, ".git")) &&
                    File.Exists(Path.Combine(directory.FullName, "SeatPeek.Cineplexx.Rs.slnx")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root for refreshing API samples.");
    }
}

internal sealed class LiveApiObservation(
    string sessionKey,
    string sessionId,
    JsonElement programmeDay,
    JsonElement programmeSession,
    JsonDocument sessionResponse,
    JsonDocument seatPlanResponse) : IDisposable
{
    public string SessionKey { get; } = sessionKey;

    public string SessionId { get; } = sessionId;

    public JsonElement ProgrammeDay { get; } = programmeDay;

    public JsonElement ProgrammeSession { get; } = programmeSession;

    public JsonDocument SessionResponse { get; } = sessionResponse;

    public JsonDocument SeatPlanResponse { get; } = seatPlanResponse;

    public void Dispose()
    {
        SessionResponse.Dispose();
        SeatPlanResponse.Dispose();
    }
}
