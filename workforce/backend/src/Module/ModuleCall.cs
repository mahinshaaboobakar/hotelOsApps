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
    public DateOnly Date(string field) => DateOnly.Parse(Required(field).GetString()!);

    /// <summary>A required string.</summary>
    public string Text(string field) => Required(field).GetString()!;

    /// <summary>An optional whole number, or the given default.</summary>
    public int Number(string field, int fallback)
        => Optional(field) is { } value ? value.GetInt32() : fallback;
}
