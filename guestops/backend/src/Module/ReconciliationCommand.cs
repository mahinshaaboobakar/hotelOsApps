using System.Text.Json;
using HotelOS.GuestOps.Application.Reconciliation;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Settling a disagreement between a staff override and a PMS fact — the
/// band's two controls on gold frame 3, ruled in GUEST-Q3 (2).
/// </summary>
/// <remarks>
/// <para>
/// <b>The service this calls was already complete, ruled and tested, and
/// reachable by nothing.</b> <c>ReconciliationService.ClearAsync</c> carries the
/// whole ruling — either side, who and when, both values kept, and the PMS side
/// publishing the same correction a room move does — and three tests drive it.
/// No door served it, so the band drew both controls off and told the desk the
/// capability was missing. <i>A capability with no adapter is invisible to every
/// check: the suite is green, the screen is honest, and the thing cannot be
/// done.</i>
/// </para>
/// <para>
/// <b>No permission of its own, and that is the ruling rather than an
/// omission.</b> GUEST-Q3 (2) refused author-only and supervisor-only clearing
/// by name: the same permission that made the override clears it, so
/// <c>ClearAsync</c> requires <c>stay.override</c> on the stay and this door
/// adds nothing. A <c>disagreement.clear</c> permission here would reintroduce
/// the escalation the ruling rejected, at the one layer nobody re-reads.
/// </para>
/// <para>
/// <b>No version, because the service takes none.</b> A correction carries the
/// version the stay was read at; a clear names a disagreement row, and the
/// service refuses a row that has already been decided in as many words. Adding
/// a version check here would be a concurrency rule invented at a door.
/// </para>
/// <para>
/// <b>An unknown side is refused by name.</b> Defaulting it would decide a
/// reconciliation the caller did not ask for — and the PMS side is the one that
/// publishes, so a wrong default would announce a room change to Room Care.
/// </para>
/// </remarks>
/// <param name="reconciliation">Where the two decisions only a person makes live.</param>
public sealed class ReconciliationCommand(ReconciliationService reconciliation)
{
    /// <summary>Decide a standing disagreement.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The disagreement row, and which side stands.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>What the row now says, as the band redraws from.</returns>
    /// <exception cref="InvalidRequestException">
    /// No row, no side, or a side this application does not have.
    /// </exception>
    public async Task<object?> RunAsync(
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException(
                "settling a disagreement needs the row and the side that stands");
        }

        var disagreementId = Bodies.Id(sheet, "disagreementId")
            ?? throw new InvalidRequestException(
                "settling a disagreement needs the row it is about");

        var row = await reconciliation.ClearAsync(
            scope, disagreementId, Side(sheet), cancellationToken);

        return new
        {
            disagreementId = row.Id.ToString(),
            state = row.State.ToString(),

            // **Both values, after the decision as before it.** The screen
            // redraws the history from these, and a response that returned only
            // the winner would make the losing value unrecoverable at exactly
            // the moment somebody wants to explain the choice.
            ours = row.OurValue,
            pms = row.PmsValue,
        };
    }

    /// <summary>Which value the person kept.</summary>
    /// <remarks>
    /// Matched against the wire's own spelling rather than the enum's, because
    /// the two are different vocabularies: <c>ours</c> and <c>pms</c> are what
    /// the band's actions carry, and accepting <c>Ours</c> as well would make
    /// the contract whatever each caller happened to send.
    /// </remarks>
    private static ClearSide Side(JsonElement body)
    {
        if (!body.TryGetProperty("side", out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidRequestException(
                "settling a disagreement needs the side that stands");
        }

        return value.GetString() switch
        {
            "ours" => ClearSide.Ours,
            "pms" => ClearSide.Pms,
            var given => throw new InvalidRequestException(
                $"'{given}' is not a side of a disagreement \u2014 'ours' or 'pms'"),
        };
    }
}
