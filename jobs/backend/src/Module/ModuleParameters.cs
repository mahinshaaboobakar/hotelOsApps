using System.Text.Json;
using HotelOS.Platform;

namespace HotelOS.Jobs.Module;

/// <summary>
/// The bundle's JSON, read as the values a handler needs — design page 63 §3.
/// </summary>
/// <remarks>
/// <para>
/// The envelope hands over a <see cref="JsonElement"/> and reads nothing inside
/// it: the shapes are this application's, which is the boundary the envelope
/// exists to keep. So the parsing is here, in one file, and every handler asks
/// for a value by name rather than walking the document itself.
/// </para>
/// <para>
/// <b>A missing or malformed value is an invalid request, never a default.</b>
/// A version that silently became zero would turn an optimistic-concurrency
/// check into a race that always passes, and a property guessed from nothing is
/// a call authorized somewhere the person may not be.
/// </para>
/// </remarks>
public static class ModuleParameters
{
    /// <summary>A required GUID.</summary>
    public static Guid Id(this JsonElement? body, string name)
    {
        var text = Text(body, name);
        return Guid.TryParse(text, out var id) && id != Guid.Empty
            ? id
            : throw new InvalidRequestException($"{name} must be an id");
    }

    /// <summary>An optional GUID — absent, null or empty all mean nothing was named.</summary>
    public static Guid? OptionalId(this JsonElement? body, string name) =>
        Property(body, name) is { ValueKind: JsonValueKind.String } value
            && Guid.TryParse(value.GetString(), out var id)
            && id != Guid.Empty
                ? id
                : null;

    /// <summary>Required text, trimmed, never empty.</summary>
    public static string Text(this JsonElement? body, string name)
    {
        var value = OptionalText(body, name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidRequestException($"{name} is required")
            : value;
    }

    /// <summary>Text if it was sent, else null.</summary>
    public static string? OptionalText(this JsonElement? body, string name) =>
        Property(body, name) is { ValueKind: JsonValueKind.String } value
            ? value.GetString()?.Trim() is { Length: > 0 } text ? text : null
            : null;

    /// <summary>A number, or the given value when it was not sent.</summary>
    public static int Number(this JsonElement? body, string name, int whenAbsent = 0) =>
        Property(body, name) is { ValueKind: JsonValueKind.Number } value ? value.GetInt32() : whenAbsent;

    /// <summary>A flag, or the given value when it was not sent.</summary>
    public static bool Flag(this JsonElement? body, string name, bool whenAbsent = false) =>
        Property(body, name) switch
        {
            { ValueKind: JsonValueKind.True } => true,
            { ValueKind: JsonValueKind.False } => false,
            _ => whenAbsent,
        };

    /// <summary>A list of strings, empty when it was not sent.</summary>
    public static IReadOnlyList<string> Texts(this JsonElement? body, string name)
    {
        if (Property(body, name) is not { ValueKind: JsonValueKind.Array } array) return [];
        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!.Trim())
            .Where(item => item.Length > 0)
            .ToList();
    }

    /// <summary>The row version a screen is acting on — required wherever a write is versioned.</summary>
    /// <remarks>
    /// Named separately because its absence has a specific consequence: a write
    /// with no expected version cannot lose a conflict, and two people editing
    /// one job would both be told they won.
    /// </remarks>
    public static long Version(this JsonElement? body, string name = "version") =>
        Property(body, name) is { ValueKind: JsonValueKind.Number } value
            ? value.GetInt64()
            : throw new InvalidRequestException(
                "the row version this edit is based on is required — without it a conflict cannot be seen");

    private static JsonElement? Property(JsonElement? body, string name) =>
        body is { ValueKind: JsonValueKind.Object } document
            && document.TryGetProperty(name, out var value)
            && value.ValueKind != JsonValueKind.Null
                ? value
                : null;
}
