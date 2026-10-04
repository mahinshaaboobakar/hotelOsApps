using System.Text.Json;
using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Writing this application's own configuration — the Setup bar's `Save`, and
/// ADR 0356's ruled shape.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR 0356 (`GUEST-Q15`) rules this application state rather than platform
/// configuration</b>: <i>"a value does not become manifest configuration merely
/// because an administrator changes it in a settings screen"</i>, and
/// <i>"for application-owned policy the owning application persists it,
/// validates it at the write boundary, and owns its concurrency/version
/// semantics"</i>. <c>SettingsService.SaveAsync</c> already does all three. This
/// is the door, and nothing else.
/// </para>
/// <para>
/// <b>What its absence cost, which is why this is not cosmetic.</b> The write
/// was served on the gRPC surface and by no module method, so the pane's `Save`
/// drew off and no property could ever store a settings row through the desk —
/// and <c>MintCardNumber</c> refuses to number a registration card until one is
/// stored. One missing arm made frame 15's capture permanently <c>409</c>
/// through the product's own path.
/// </para>
/// <para>
/// <b>The version is required, not defaulted.</b> <c>SaveAsync</c> takes the
/// version the caller last read and treats <c>0</c> as a create. Defaulting an
/// absent one to <c>0</c> would still refuse an existing row — with a
/// concurrency error naming a version nobody sent, three layers from the caller
/// who omitted it. Absent is refused here, by name.
/// </para>
/// <para>
/// <b>And the series is not writable, by the shape of <c>SettingsEdit</c>.</b>
/// It carries a prefix and no next-number: a property that could set the counter
/// backwards would issue one card number twice. The API having nowhere to put it
/// is stronger than validating it away.
/// </para>
/// </remarks>
/// <param name="settings">Where this application's configuration lives.</param>
public sealed class SettingsCommand(SettingsService settings)
{
    /// <summary>Store the property's configuration.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The values to apply, and the version last read.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>What is now stored, as the screen redraws from.</returns>
    /// <exception cref="InvalidRequestException">
    /// No body, no version, a missing value, or a scope this application does not have.
    /// </exception>
    public async Task<object?> RunAsync(
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException(
                "saving settings needs the values to apply and the version they were read at");
        }

        var version = Bodies.Version(sheet)
            ?? throw new InvalidRequestException(
                "saving settings needs the version the configuration was read at — 0 to store it "
                + "for the first time");

        var saved = await settings.SaveAsync(scope, Edit(sheet), version, cancellationToken);

        return new
        {
            version = saved.Version,
            stored = saved.Stored,

            // Echoed from what was STORED rather than from what was sent, so a
            // screen that redraws from this is reading the row rather than its
            // own intention — and `HomeCountry` is upper-cased by the service.
            homeCountry = saved.HomeCountry,
            reportingAppliesTo = ReportingScopes.Wire(saved.ReportingAppliesTo),
        };
    }

    /// <summary>The values, as this application's own vocabulary.</summary>
    private static SettingsEdit Edit(JsonElement body)
        => new(
            Text(body, "homeCountry"),
            List(body, "requiredForHomeCountry"),
            List(body, "requiredForVisitors"),
            List(body, "acceptedIdTypes"),
            Flag(body, "signatureRequired"),
            Flag(body, "printOnCheckIn"),
            Text(body, "cardNumberPrefix"),
            Flag(body, "reportingRequired"),
            ReportingScopes.Parse(Text(body, "reportingAppliesTo")),

            // Absent and empty are one value here: a property with no authority
            // named has none, which is what the column's nullability says.
            Optional(body, "reportingAuthority"),
            Number(body, "reportingDueHours"));

    /// <summary>A value the caller must send.</summary>
    private static string Text(JsonElement body, string name)
        => body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : throw new InvalidRequestException($"saving settings needs {name}");

    /// <summary>A value the caller may leave unset.</summary>
    private static string? Optional(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()
                : null;

    /// <summary>A flag, refused rather than defaulted.</summary>
    /// <remarks>
    /// Both of these decide what a card demands. A missing
    /// <c>signatureRequired</c> read as <c>false</c> would silently drop a
    /// requirement the property had set, and nothing on the screen would say so.
    /// </remarks>
    private static bool Flag(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : throw new InvalidRequestException($"saving settings needs {name}");

    private static int Number(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var number)
                ? number
                : throw new InvalidRequestException($"saving settings needs {name}");

    /// <summary>A list of the property's own words.</summary>
    /// <remarks>
    /// An absent list is refused rather than read as empty. An empty required-set
    /// means <i>this property demands nothing</i>, which is a decision; an absent
    /// one means the caller did not say, and the two must not arrive as one.
    /// </remarks>
    private static IReadOnlyList<string> List(JsonElement body, string name)
    {
        if (!body.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidRequestException($"saving settings needs {name} as a list");
        }

        return [.. value.EnumerateArray()
            .Where(one => one.ValueKind == JsonValueKind.String)
            .Select(one => one.GetString() ?? string.Empty)
            .Where(one => one.Length > 0)];
    }
}
