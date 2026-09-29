using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using HotelOS.Platform;

namespace HotelOS.Workforce.Module;

/// <summary>
/// What every capability handler is given: the call, and a scope to serve it in.
/// </summary>
/// <remarks>
/// <b>The scope is the request's, handed over by the envelope</b> —
/// <c>SHELL-Q40</c> §3. This record used to carry a provider this application
/// had opened itself, because the envelope passed none and an application that
/// resolved a scoped <c>DbContext</c> from the root would work on the desk it
/// was written at and share one context across every concurrent request. That
/// is the platform's again, so <see cref="Services"/> is simply what the call
/// arrived with.
/// </remarks>
/// <param name="Method">The application's own verb.</param>
/// <param name="Body">The bundle's JSON, or null when it sent none.</param>
/// <param name="Scope">Who is asking, and where. Never read from the body.</param>
/// <param name="Services">The request's own scope.</param>
public sealed record ModuleCall(
    string Method,
    JsonElement? Body,
    RequestScope Scope,
    IServiceProvider Services)
{
    /// <summary>Resolve a service for the length of this call.</summary>
    public T Service<T>() where T : notnull => Services.GetRequiredService<T>();

    /// <summary>
    /// A required field of the bundle's JSON.
    /// </summary>
    /// <remarks>
    /// Absent is <see cref="InvalidRequestException"/> rather than a default:
    /// the platform maps a 400 to the bundle's <c>invalid</c>, and a call that
    /// silently defaulted a missing id would act on whatever the default named.
    /// </remarks>
    public JsonElement Required(string field)
    {
        if (Body is not { ValueKind: JsonValueKind.Object } body
            || !body.TryGetProperty(field, out var value)
            || value.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidRequestException($"'{field}' is required");
        }

        return value;
    }

    /// <summary>An optional field, or nothing.</summary>
    public JsonElement? Optional(string field)
        => Body is { ValueKind: JsonValueKind.Object } body
           && body.TryGetProperty(field, out var value)
           && value.ValueKind != JsonValueKind.Null
            ? value
            : null;

    /// <summary>A required id.</summary>
    public Guid Id(string field) => Required(field).GetGuid();

    /// <summary>A required date, in the wire's own form.</summary>
    // ADR 0174 NUM-Q4 - a module call's field is a wire value. The culture
    // is named; the FormatException on a bad value is workforce's own
    // contract and is left exactly as it was.
    public DateOnly Date(string field) =>
        DateOnly.Parse(Required(field).GetString()!, CultureInfo.InvariantCulture);

    /// <summary>An optional date, or nothing where the call sent none.</summary>
    /// <remarks>
    /// <para>
    /// <b>Nullable rather than defaulted, because the fallbacks differ.</b> Five
    /// Views fall back to the property's operating day and <c>ScheduleView</c>
    /// falls back to <c>OperatingDay.OrUnavailable</c>, so a signature taking a
    /// fallback would have to name one of them — making the accessor lie about the
    /// others. A caller keeps its own with <c>??</c>, which also keeps the
    /// short-circuit: an <c>await</c> on the right of <c>??</c> does not run when
    /// the field is present, exactly as the ternary it replaces did not.
    /// </para>
    /// <para>
    /// <b>The culture is not the caller's to forget.</b> Six Views parsed this
    /// inline and ADR 0174 <c>NUM-Q4</c> had to visit every one of them to name
    /// <see cref="CultureInfo.InvariantCulture"/>. A new View copies whichever
    /// neighbour its author opens, and git history still holds the one-argument
    /// form — so the fix that survives the next author is the one that leaves them
    /// nothing to omit. FF measured the duplication and proposed this rather than
    /// fixing it a seventh time.
    /// </para>
    /// </remarks>
    public DateOnly? OptionalDate(string field)
        => Optional(field) is { } value
            ? DateOnly.Parse(value.GetString()!, CultureInfo.InvariantCulture)
            : null;

    /// <summary>An optional time of day, or nothing where the call sent none.</summary>
    /// <remarks>
    /// Three Views declared a byte-identical <c>private static TimeOnly? Time</c>
    /// for this and called it ten times between them. Same reasoning as
    /// <see cref="OptionalDate"/>: the parse belongs where the field is read, not
    /// once per View that happens to read one.
    /// </remarks>
    public TimeOnly? OptionalTime(string field)
        => Optional(field) is { } value
            ? TimeOnly.Parse(value.GetString()!, CultureInfo.InvariantCulture)
            : null;

    /// <summary>A required string.</summary>
    public string Text(string field) => Required(field).GetString()!;

    /// <summary>An optional whole number, or the given default.</summary>
    public int Number(string field, int fallback)
        => Optional(field) is { } value ? value.GetInt32() : fallback;
}
