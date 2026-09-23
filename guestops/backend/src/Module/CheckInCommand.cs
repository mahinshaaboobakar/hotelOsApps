using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Checking the guest in — the second half of the registration card's button.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is here because frame 15 draws it here.</b> The card's primary action
/// reads <i>Save and check in</i>, and the only approved route to that card is
/// the stay screen's <i>Check in</i>. A card whose primary action captured
/// nothing was the defect this round is closing; one whose primary action did
/// half of what it says would be the same defect, quieter.
/// </para>
/// <para>
/// <b>The rest of the lifecycle is not here, and its absence is deliberate.</b>
/// Check-out, no-show and the correction of a recorded arrival are their own
/// frame's work and will map beside this method under the same capability —
/// <c>stay.override</c>, the permission <c>StayLifecycleService</c> already
/// requires of every one of them.
/// </para>
/// <para>
/// <b>Two calls, not one compound write.</b> The screen captures the card and
/// then calls this. A single method doing both could half-succeed — the card
/// stored, the guest not in — and would have to explain that in a return value
/// nobody would read; two calls give each half its own failure, and let the
/// screen say <i>the card was saved and the guest was not checked in</i>, which
/// is a true sentence about what happened rather than a generic failure.
/// </para>
/// </remarks>
/// <param name="lifecycle">Where the stay's transitions live.</param>
public sealed class CheckInCommand(StayLifecycleService lifecycle)
{
    /// <summary>Record the arrival.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The stay, and the version it was read at.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>The stay's new state and version, as the screen redraws from.</returns>
    /// <exception cref="InvalidRequestException">No stay, or no version.</exception>
    public async Task<object?> RunAsync(
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException("a check-in needs a stay and its version");
        }

        var stayId = Id(sheet, "stayId")
            ?? throw new InvalidRequestException("a check-in needs the stay it is about");

        // **Refused rather than defaulted to zero.** A missing version read as
        // zero would fail the concurrency check for a reason that names the
        // wrong thing — the caller would be told somebody else had changed the
        // stay, when what happened is that it never said which stay it read.
        var version = Version(sheet)
            ?? throw new InvalidRequestException(
                "a check-in needs the version the stay was read at");

        var stay = await lifecycle.CheckInAsync(scope, stayId, version, cancellationToken);

        return new
        {
            stayId = stay.Id.ToString(),
            version = stay.Version,
            lifecycle = stay.Lifecycle.ToString(),
        };
    }

    private static Guid? Id(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var id)
                ? id
                : null;

    private static long? Version(JsonElement body)
        => body.TryGetProperty("version", out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var version)
                ? version
                : null;
}
