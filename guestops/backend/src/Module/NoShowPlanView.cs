using System.Globalization;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What recording a no-show would do — the owner's N1 dialog, ruled 2026-09-24.
/// </summary>
/// <remarks>
/// <para>
/// <b>A read, so a person may see the consequence without being able to cause
/// it.</b> It computes a forfeit and names what happens afterwards and writes
/// nothing; only the button needs <c>stay.override</c>. Same split as
/// <see cref="CancelPlanView"/>, for the same reason.
/// </para>
/// <para>
/// <b>The forfeit is read from the booking's own terms, never assumed.</b>
/// Where no terms are stored the dialog says so in words rather than showing a
/// zero — <i>zero is a forfeit of nothing, and no terms is nobody having
/// agreed one</i>. A screen that printed a confident amount nobody agreed is
/// the gap rule at the one figure a guest may later be charged.
/// </para>
/// <para>
/// <b>It does not re-decide whether the action is offered.</b> The stay page
/// and the arrivals list decide that through <see cref="NoShowRule"/>; asking
/// again here would be a third opinion, and a dialog that refused what the
/// screen had just offered would be telling the desk its own screen was wrong.
/// </para>
/// </remarks>
/// <param name="db">This application's own schema.</param>
public sealed class NoShowPlanView(GuestOpsDbContext db)
{
    /// <summary>What would happen if nobody came.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="stayId">The stay the desk is looking at.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>The dialog's subject, its forfeit and what follows.</returns>
    /// <exception cref="NotFoundException">No such stay at this property.</exception>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        // Scoped by property first, so another property's stay is NOT FOUND
        // rather than forbidden — ADR 0054's boundary class.
        var stay = await db.Stays
            .Where(s => s.PropertyId == scope.PropertyId && s.Id == stayId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        // The primary guest, or the state of not having one. "Not yet named"
        // is a state and not a placeholder: a stay a feed sent before the
        // guest was known has no name, and inventing one would attribute a
        // forfeited night to a person who does not exist.
        var guest = await db.Party
            .Where(p => p.StayId == stay.Id)
            .OrderByDescending(p => p.IsPrimary == true)
            .Select(p => p.Guest!.NameAsGiven)
            .FirstOrDefaultAsync(cancellationToken);

        var reference = await db.StayExternalRefs
            .Where(r => r.StayId == stay.Id
                && r.IdentifierKind.ToLower() == "booking")
            .Select(r => r.ExternalId)
            .FirstOrDefaultAsync(cancellationToken);

        var terms = await db.Terms
            .Where(t => t.StayId == stay.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new
        {
            stayId = stay.Id.ToString(),

            // The version the desk read at, so the confirm can carry it. Without
            // it the dialog would have to be told the version by its opener, and
            // a screen that assembles a write from two reads is a screen that can
            // send a version belonging to neither.
            version = stay.Version,

            // **The parts of the head's second line, never the line.** The
            // screen composes `Thomas George · BK-4361 · 19 – 20 Aug`: a
            // compressed range puts the month at one end BECAUSE `en-GB` puts
            // the month after the day, and that is a grammar rather than a
            // format (ADR 0175).
            guest = string.IsNullOrWhiteSpace(guest) ? "Not yet named" : guest,
            reference,
            arrive = stay.ArrivalAt.At?.ToString("O"),
            depart = stay.DepartureAt.At?.ToString("O"),

            forfeit = Forfeit(terms),

            afterwards = "the stay leaves Arrivals and stays in the list, as a no-show",
        };
    }

    /// <summary>
    /// What the terms say is forfeited, in the parts a screen composes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ADR 0175 §NUM-Q2, ruled 2026-09-20: the amount is a DECIMAL STRING
    /// and the currency an ISO 4217 code, and no N2-formatted text appears on a
    /// service contract.</b> So this sends the parts and the screen renders
    /// them — a currency symbol, a grouping separator and a spelled night count
    /// are the reader's, and a service that joined them has written one
    /// locale's grammar into a projection.
    /// </para>
    /// <para>
    /// <b>Written this way because the neighbour is wrong and is known to
    /// be.</b> <see cref="CancelPlanView"/> renders <c>N2</c> text at a line
    /// that carries its own note naming this ruling as owed work. Copying it
    /// for symmetry would add a second instance of a defect the platform has
    /// already ruled against, in code written after the ruling.
    /// </para>
    /// <para>
    /// <b>Invariant, because a decimal string is a wire value.</b> Under the
    /// server's culture this is <c>4800,00</c> in half of Europe, and the
    /// screen would parse it as four-point-eight — ADR 0174 §NUM-Q4.
    /// </para>
    /// <para>
    /// Null where nothing was agreed: an absent forfeit and a forfeit of zero
    /// are different facts, and the screen says which.
    /// </para>
    /// </remarks>
    private static object? Forfeit(CommercialTerms? terms)
    {
        if (terms?.PenaltyAmount is not { } money)
        {
            return null;
        }

        // An amount with no currency is not an amount — `Money.IsStated` exists
        // for this. Minor units with nothing beside them is a number a guest
        // could be charged in the wrong denomination.
        if (!money.IsStated)
        {
            return new { unstated = "recorded without a currency" };
        }

        return new
        {
            amount = (money.MinorUnits / 100m).ToString(CultureInfo.InvariantCulture),
            currency = money.Currency,
            basis = money.Basis.ToString(),

            // Omitted rather than guessed where the terms named none — the
            // screen then says the forfeit without saying how many nights it is.
            nights = terms.PenaltyNights,
        };
    }
}
