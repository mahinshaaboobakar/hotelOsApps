using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Recording that the guest has left — gold frame 3's <i>Check out</i>.
/// </summary>
/// <remarks>
/// <para>
/// <b>It announces a departure and asserts nothing about cleaning.</b> Room
/// Care decides for itself whether a vacated room becomes work, because
/// cleaning is policy-driven and a checked-out room becoming a task is a hotel
/// policy rather than an automatic consequence (APPS-Q1). This publishes
/// <c>stay.departed</c> and stops there.
/// </para>
/// <para>
/// <b>There is no override control here, and that is the design.</b> A staff
/// write on a stay the PMS owns is applied <i>and recorded as an override</i>
/// by the service, with who, when, and what the PMS said at that moment
/// (GUEST-Q1's amendment). So an override is a state that can contradict a
/// PMS, never a button somebody presses to set one — and a screen offering to
/// "override" would be offering a second answer where the platform keeps
/// exactly one (GUEST-Q3).
/// </para>
/// <para>
/// <b>No confirmation, because the frame draws none.</b> Frame 3 gives
/// <i>Cancel…</i> an ellipsis and <i>Check out</i> none: the ellipsis is the
/// affordance's own statement that it opens a dialog. A confirmation invented
/// here would be richer than the approved design, and the correction for a
/// departure recorded in error is <c>CorrectAsync</c> — which exists precisely
/// because *"the guest checked out in error at 07:00 and still asleep in the
/// room is a real morning"*.
/// </para>
/// </remarks>
/// <param name="lifecycle">Where the stay's transitions live.</param>
public sealed class CheckOutCommand(StayLifecycleService lifecycle)
{
    /// <summary>Record the departure.</summary>
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
            throw new InvalidRequestException("a check-out needs a stay and its version");
        }

        var stayId = Bodies.Id(sheet, "stayId")
            ?? throw new InvalidRequestException("a check-out needs the stay it is about");

        // **Refused rather than defaulted to zero.** A missing version read as
        // zero fails the concurrency check with a message about somebody else
        // having changed the stay — a claim about the world, when what happened
        // is that the caller never said which stay it read.
        var version = Bodies.Version(sheet)
            ?? throw new InvalidRequestException(
                "a check-out needs the version the stay was read at");

        var stay = await lifecycle.CheckOutAsync(scope, stayId, version, cancellationToken);

        return new
        {
            stayId = stay.Id.ToString(),
            version = stay.Version,
            lifecycle = stay.Lifecycle.ToString(),
        };
    }
}
