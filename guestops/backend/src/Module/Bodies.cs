using System.Text.Json;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Reading the two things every lifecycle write is addressed by — which stay,
/// and the version it was read at.
/// </summary>
/// <remarks>
/// <para>
/// <b>Mechanism, never meaning.</b> This answers <i>what did the bundle
/// send</i> and decides nothing: each caller keeps its own refusal sentence,
/// because <i>"a no-show needs the stay it is about"</i> and <i>"a check-out
/// needs the stay it is about"</i> are different sentences to the person
/// reading the failure, and a shared one would name neither operation.
/// </para>
/// <para>
/// <b>It exists because there were about to be three copies.</b>
/// <see cref="CheckOutCommand"/> held these two privately; <see
/// cref="NoShowCommand"/> and <see cref="CorrectCommand"/> need the same pair,
/// and a version reader copied three times is three chances for one of them to
/// start accepting a JSON string — after which two doors onto one service
/// disagree about what a missing version is, and neither fails loudly.
/// </para>
/// <para>
/// <b>Both return null rather than throwing.</b> The caller is the only thing
/// that knows which operation is being refused, so the decision to refuse —
/// and the words for it — stay with it.
/// </para>
/// </remarks>
public static class Bodies
{
    /// <summary>An identifier the bundle sent, where it sent a usable one.</summary>
    /// <param name="body">The application's own JSON object.</param>
    /// <param name="name">The property to read.</param>
    /// <returns>The identifier, or null if it is absent or not one.</returns>
    public static Guid? Id(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var id)
                ? id
                : null;

    /// <summary>The version the caller read the stay at, where it sent one.</summary>
    /// <remarks>
    /// <b>A JSON string is not a version.</b> Accepting <c>"7"</c> here would
    /// let a bundle that had stopped sending a number keep passing the
    /// concurrency check by accident, and the first sign of it would be a lost
    /// update nobody could trace to a serialiser change.
    /// </remarks>
    /// <param name="body">The application's own JSON object.</param>
    /// <returns>The version, or null if it is absent or not a number.</returns>
    public static long? Version(JsonElement body)
        => body.TryGetProperty("version", out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var version)
                ? version
                : null;
}
