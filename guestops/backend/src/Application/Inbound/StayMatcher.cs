using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Inbound;

/// <summary>
/// Which stay a fact is about — by reference, then by candidate, then neither.
/// </summary>
/// <remarks>
/// <para>
/// The order is the whole of GUEST-Q5 and GUEST-Q8. A <b>known reference</b> is
/// an answer, because minting and mapping happened in one transaction and no
/// second fact for that reservation can find anything else. Only when nothing
/// matches does the question of a <i>candidate</i> arise, and that question is
/// never answered here.
/// </para>
/// </remarks>
public sealed class StayMatcher(GuestOpsDbContext db)
{
    /// <summary>The stay this fact names, if this application has seen it.</summary>
    public async Task<RoomStay?> ByReferenceAsync(
        InboundStayFact fact, CancellationToken cancellationToken)
    {
        foreach (var reference in fact.StayRefs)
        {
            var stay = await db.StayExternalRefs
                .Where(r => r.IntegrationId == reference.IntegrationId
                            && r.IdentifierKind == reference.IdentifierKind
                            && r.ExternalId == reference.ExternalId)
                .Select(r => r.Stay!)
                .FirstOrDefaultAsync(cancellationToken);

            if (stay is not null)
            {
                return stay;
            }
        }

        return null;
    }

    /// <summary>
    /// Stays this property created that this fact might be — same room,
    /// overlapping dates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The candidate test, exactly as ruled</b>: same room and overlapping
    /// dates. Nothing about names is used to select, because the system this
    /// replaces joined stays on
    /// <c>(companyId, siteId, surname, firstName, arrivalDate)</c> and a wrong
    /// match silently merges two guests' histories — worse than a duplicate.
    /// </para>
    /// <para>
    /// <b>Only PMS-unknown stays are candidates.</b> A stay the PMS already
    /// knows has a reference, so it was found above; offering it here would
    /// propose joining a fact to a stay that already has its own identity.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<RoomStay>> CandidatesAsync(
        InboundStayFact fact, CancellationToken cancellationToken)
    {
        if (fact.RoomId is not { } room
            || fact.Arrival.Date is not { } arrival
            || fact.Departure.Date is not { } departure)
        {
            // No room or no dates is no candidate test. Widening it to *"same
            // guest name"* is precisely the failure above, and widening it to
            // *"same property"* would propose every stay in the hotel.
            return [];
        }

        var possible = await db.Stays
            .Where(s => s.PropertyId == fact.PropertyId
                        && s.PmsUnknown
                        && s.CurrentRoomId == room)
            .ToListAsync(cancellationToken);

        return
        [
            .. possible.Where(s => Overlaps(s, arrival, departure))
        ];
    }

    /// <summary>How alike two names are — ranking only, never linking.</summary>
    /// <remarks>
    /// <para>
    /// A deliberately crude measure, and it may stay crude: its only job is to
    /// put the likeliest candidate first in a list a person reads. Improving it
    /// would not make it able to decide, because <b>no similarity is evidence
    /// that two stays are one</b> — that is the ruling, not a limitation of the
    /// algorithm.
    /// </para>
    /// <para>
    /// Kept here beside the candidate test so the two are read together: the
    /// test selects, this orders, and neither joins.
    /// </para>
    /// </remarks>
    public static double Similarity(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return 0;
        }

        var a = left.Trim().ToLowerInvariant();
        var b = right.Trim().ToLowerInvariant();

        if (a == b)
        {
            return 1;
        }

        var shared = a.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Intersect(b.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Count();

        var words = Math.Max(
            a.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            b.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

        return words == 0 ? 0 : (double)shared / words;
    }

    private static bool Overlaps(RoomStay stay, DateOnly arrival, DateOnly departure)
    {
        if (stay.ArrivalAt.Date is not { } from || stay.DepartureAt.Date is not { } to)
        {
            return false;
        }

        // Departure-exclusive on both sides: a guest leaving on the 4th and one
        // arriving on the 4th share a room and not a night, which is an ordinary
        // turnaround rather than a candidate for being the same stay.
        return from < departure && arrival < to;
    }
}
