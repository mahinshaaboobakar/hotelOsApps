using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using HotelOS.Platform;

namespace HotelOS.Workforce.Module;

/// <summary>
/// What every capability handler is given: the call, and a scope to serve it in.
/// </summary>
/// <remarks>
/// <para>
/// <b>The envelope hands a handler no service provider.</b>
/// <c>ModuleEnvelope.MapModuleCapability</c> passes the method, the body, the
/// caller and the request scope — everything about the <i>call</i> and nothing
/// about the <i>container</i>. So an application that needs a DbContext has to
/// capture the root provider and open a scope of its own, and every application
/// will write that same line. It is written once here.
/// </para>
/// <para>
/// This is reported as a redline rather than worked around quietly: the
/// envelope's own reasoning is that a check an author must remember is a check
/// that will be forgotten, and <i>resolving a scoped service from the root
/// provider</i> is exactly that shape — it works on the desk it was written at
/// and leaks a DbContext across requests in production.
/// </para>
/// </remarks>
/// <param name="Method">The application's own verb.</param>
/// <param name="Body">The bundle's JSON, or null when it sent none.</param>
/// <param name="Scope">Who is asking, and where. Never read from the body.</param>
/// <param name="Services">This call's own container scope.</param>
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
