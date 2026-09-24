using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Recording that nobody came — the owner's N1 and N2, ruled 2026-09-24.
/// </summary>
/// <remarks>
/// <para>
/// <b>A no-show is a fact the property establishes, not a cancellation.</b>
/// The guest did not arrive and did not cancel, and the two have different
/// commercial consequences — which is why this is its own transition rather
/// than <c>CancelCommand</c> with a different reason. The drawn end state
/// keeps both readable: the state reads <i>No-show</i>, the source reads
/// <i>yours</i> rather than the PMS's, and the forfeited night is recorded
/// without being charged.
/// </para>
/// <para>
/// <b>The service decides whether the stay could have been one.</b>
/// <c>RecordNoShowAsync</c> refuses anything that is not still waiting to
/// arrive — <i>"only a stay that never arrived can be a no-show"</i> — so a
/// stay already in house is refused there rather than filtered here. The two
/// surfaces that reach it therefore agree about what the transition means.
/// </para>
/// <para>
/// <b>Which day a stay is late on is not this command's judgement.</b> The
/// drawn list offers the action only on a row whose <i>arrival</i> day has
/// passed, because a stay arriving today is not a no-show at four in the
/// afternoon — and that is a property of the list, which knows the business
/// day. A command that re-derived it would be a second answer to a question
/// the screen has already asked.
/// </para>
/// </remarks>
/// <param name="lifecycle">Where the stay's transitions live.</param>
public sealed class NoShowCommand(StayLifecycleService lifecycle)
{
    /// <summary>Record that the guest never arrived.</summary>
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
            throw new InvalidRequestException("a no-show needs a stay and its version");
        }

        var stayId = Bodies.Id(sheet, "stayId")
            ?? throw new InvalidRequestException("a no-show needs the stay it is about");

        // Refused rather than defaulted to zero, as every other lifecycle write
        // here is: zero reaches the concurrency check and comes back saying
        // somebody else changed the stay, which is a claim about the world when
        // what happened is that the caller never said which stay it read.
        var version = Bodies.Version(sheet)
            ?? throw new InvalidRequestException(
                "a no-show needs the version the stay was read at");

        var stay = await lifecycle.RecordNoShowAsync(scope, stayId, version, cancellationToken);

        return new
        {
            stayId = stay.Id.ToString(),
            version = stay.Version,
            lifecycle = stay.Lifecycle.ToString(),
        };
    }
}
