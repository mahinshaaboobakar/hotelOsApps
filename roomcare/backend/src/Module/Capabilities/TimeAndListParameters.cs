using System.Globalization;
using System.Text.Json;
using HotelOS.Platform;

namespace HotelOS.RoomCare.Module.Capabilities;

/// <summary>The bundle's JSON read as times, dates, instants and lists — the values Room Care's screens send beyond Jobs' set.</summary>
/// <remarks>As <see cref="ModuleParameters"/>: a malformed value is an invalid request, never a default.</remarks>
public static class TimeAndListParameters
{
    public static TimeOnly Time(this JsonElement? body, string name) =>
        OptionalTime(body, name) ?? throw new InvalidRequestException(ModuleParameters.Unreadable);

    public static TimeOnly? OptionalTime(this JsonElement? body, string name) =>
        body.OptionalText(name) is { } text
            ? TimeOnly.TryParseExact(text, ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
                ? time
                : throw new InvalidRequestException(ModuleParameters.Unreadable)
            : null;

    public static DateOnly Date(this JsonElement? body, string name) =>
        DateOnly.TryParseExact(body.Text(name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day
            : throw new InvalidRequestException(ModuleParameters.Unreadable);

    /// <summary>An instant, which must carry its offset — <c>Z</c> or <c>±hh:mm</c>.</summary>
    /// <remarks>
    /// A time with no offset is refused, never read: <c>DateTimeOffset.TryParse</c> would read it in the server's own
    /// zone, which is neither UTC nor the property's, and on a server set to the property's zone the mistake is
    /// invisible (owner instruction, 2026-09-19: a wall-clock time becomes an instant only through the property's
    /// zone). A screen that has a property-local time sends it as a time, and the service makes the instant.
    /// </remarks>
    public static DateTimeOffset Instant(this JsonElement? body, string name)
    {
        var text = body.Text(name);
        if (!OffsetAtEnd.IsMatch(text))
        {
            throw new InvalidRequestException(ModuleParameters.Unreadable);
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
            ? at
            : throw new InvalidRequestException(ModuleParameters.Unreadable);
    }

    private static readonly System.Text.RegularExpressions.Regex OffsetAtEnd = new(@"T.*(Z|[+-][0-9]{2}:[0-9]{2})$");

    public static decimal Decimal(this JsonElement? body, string name, decimal whenAbsent = 0) =>
        Property(body, name) is { ValueKind: JsonValueKind.Number } value ? value.GetDecimal() : whenAbsent;

    public static int? OptionalNumber(this JsonElement? body, string name) =>
        Property(body, name) is { ValueKind: JsonValueKind.Number } value ? value.GetInt32() : null;

    public static bool? OptionalFlag(this JsonElement? body, string name) =>
        Property(body, name) switch
        {
            { ValueKind: JsonValueKind.True } => true,
            { ValueKind: JsonValueKind.False } => false,
            _ => null,
        };

    public static IReadOnlyList<Guid> Ids(this JsonElement? body, string name) =>
        body.Texts(name).Select(t => Guid.TryParse(t, out var id) && id != Guid.Empty ? id : throw new InvalidRequestException(ModuleParameters.Unreadable)).ToList();

    public static IReadOnlyList<TimeOnly> Times(this JsonElement? body, string name) =>
        body.Texts(name).Select(t => TimeOnly.TryParseExact(t, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : throw new InvalidRequestException(ModuleParameters.Unreadable)).ToList();

    /// <summary>An array of objects, each read with the same helpers.</summary>
    public static IReadOnlyList<JsonElement?> Objects(this JsonElement? body, string name) =>
        Property(body, name) is { ValueKind: JsonValueKind.Array } array
            ? array.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object).Select(e => (JsonElement?)e).ToList()
            : [];

    private static JsonElement? Property(JsonElement? body, string name) =>
        body is { ValueKind: JsonValueKind.Object } document && document.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value
            : null;
}
