using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Putting right a lifecycle fact recorded in error — the owner's C1 and C2,
/// ruled 2026-09-24.
/// </summary>
/// <remarks>
/// <para>
/// <b>A correction is not an undo.</b> The mistaken fact stays, the correction
/// is recorded beside it, and both are readable afterwards — which is what
/// lets somebody in November ask why 214 shows a departure clean on the 24th
/// and find the answer in the list rather than in nobody's memory. The drawn
/// end state keeps the earlier departure on the card, marked <i>recorded in
/// error</i>, rather than removing it.
/// </para>
/// <para>
/// <b>The reason is required, and the service is what requires it.</b>
/// <c>CorrectAsync</c> refuses an empty one in as many words — <i>"without one
/// it is indistinguishable from a mistake"</i> — so the dialog's required
/// field and the refusal agree because they are the same rule, not because two
/// places were kept in step.
/// </para>
/// <para>
/// <b>Any lifecycle, because the service rules on that and this does not.</b>
/// Two corrections are drawn — a mistaken check-out put back in house, and a
/// no-show reinstated — and a door that admitted exactly those two would be a
/// rule nobody made, enforced at one of the two surfaces that reach this
/// service. The gRPC surface already passes whatever it is given; so does
/// this, and <c>CorrectAsync</c> is the single place that decides.
/// </para>
/// <para>
/// <b>An unknown state is refused by name.</b> Parsing a lifecycle the
/// application does not have into a default would write some other state onto
/// the stay and report success — a correction that corrupts the thing it was
/// asked to put right.
/// </para>
/// </remarks>
/// <param name="lifecycle">Where the stay's transitions live.</param>
public sealed class CorrectCommand(StayLifecycleService lifecycle)
{
    /// <summary>Record the correction, and what it was for.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The stay, the state to correct it to, the reason, and the version.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>The stay's new state and version, as the screen redraws from.</returns>
    /// <exception cref="InvalidRequestException">
    /// No stay, no version, no target state, or a state this application does not have.
    /// </exception>
    public async Task<object?> RunAsync(
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException(
                "a correction needs a stay, a state, a reason and a version");
        }

        var stayId = Bodies.Id(sheet, "stayId")
            ?? throw new InvalidRequestException("a correction needs the stay it is about");

        var version = Bodies.Version(sheet)
            ?? throw new InvalidRequestException(
                "a correction needs the version the stay was read at");

        var to = State(sheet);

        // Passed through whitespace and all: `CorrectAsync` owns the rule that
        // a reason is required, and trimming to null here would refuse a blank
        // one with this file's sentence instead of the service's, which is the
        // one that says why.
        var reason = sheet.TryGetProperty("reason", out var given)
            && given.ValueKind == JsonValueKind.String
                ? given.GetString() ?? string.Empty
                : string.Empty;

        var stay = await lifecycle.CorrectAsync(
            scope, stayId, to, reason, version, cancellationToken);

        return new
        {
            stayId = stay.Id.ToString(),
            version = stay.Version,
            lifecycle = stay.Lifecycle.ToString(),
        };
    }

    /// <summary>The state the caller is correcting the stay to.</summary>
    private static StayLifecycle State(JsonElement body)
    {
        if (!body.TryGetProperty("to", out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidRequestException(
                "a correction needs the state it is correcting the stay to");
        }

        // Case-sensitive, so `inhouse` is refused rather than accepted as a
        // spelling of `InHouse`: a caller that has drifted from the contract
        // should hear about it here, not be quietly understood.
        return Enum.TryParse<StayLifecycle>(value.GetString(), ignoreCase: false, out var parsed)
            ? parsed
            : throw new InvalidRequestException(
                $"'{value.GetString()}' is not a state a stay can be in");
    }
}
