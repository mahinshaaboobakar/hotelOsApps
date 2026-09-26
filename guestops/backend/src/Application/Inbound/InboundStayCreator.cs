using System.Text.Json;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Inbound;

/// <summary>
/// Creating a stay a source has told us about and we have never seen.
/// </summary>
/// <remarks>
/// <para>
/// <b>One purpose, and it needs no "and": a fact about a stay we do NOT
/// hold.</b> <see cref="InboundFactService"/> keeps the other half — matching
/// a fact to a stay we do, reconciling it, and recording a contradiction when
/// the source disagrees with us. The two are different questions about one
/// arriving fact, and they were one file at 301 code lines.
/// </para>
/// <para>
/// <b>Extracted 2026-09-26 along a boundary that already existed</b> — ADR
/// 0036, whose other half is that the split follows a seam rather than
/// cutting where the line count fell. `CreateAsync`, `EnsureBookingAsync` and
/// `HoldAsync` were already a contiguous run calling only each other.
/// </para>
/// <para>
/// <b>It went unmeasured for twenty-three days.</b> The 300-line ceiling had
/// never been enforced on an installable application until ADR 0259 gave the
/// standards check explicit roots; this file crossed on 2026-09-03 and nothing
/// could see it. *The extraction is ordinary; the reason it was owed is not.*
/// </para>
/// <para>
/// <b>It takes no matcher.</b> Matching is what the other half does, and a
/// stay being created is by definition one nothing matched — so the dependency
/// is absent rather than passed and ignored.
/// </para>
/// </remarks>
/// <param name="db">This application's own schema.</param>
/// <param name="events">Where the facts it establishes are announced.</param>
/// <param name="clock">The platform's clock, never the machine's.</param>
public sealed class InboundStayCreator(
    GuestOpsDbContext db,
    IEventAppender events,
    TimeProvider clock)
{
    /// <summary>A stay nobody here has seen — created from the fact itself.</summary>
    /// <remarks>
    /// <para>
    /// <b>The stay and its references are minted in one transaction</b> —
    /// GUEST-Q8. A crash between them would leave a stay nothing could ever
    /// match again, and the next fact would create a duplicate.
    /// </para>
    /// <para>
    /// <b>A check-out for a stay never seen creates it in <c>Departed</c></b>
    /// with its arrival absent and an absence recording that nobody observed
    /// one — R7, and the intermediate states are never invented.
    /// </para>
    /// </remarks>
    public async Task CreateAsync(
        RequestScope scope, InboundStayFact fact, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var booking = await EnsureBookingAsync(fact, now, cancellationToken);

        var stay = new RoomStay
        {
            Id = Guid.CreateVersion7(),
            BookingId = booking.Id,
            PropertyId = fact.PropertyId,
            RoomTypeId = fact.RoomTypeId,
            CurrentRoomId = fact.RoomId,
            Lifecycle = fact.Lifecycle,
            ArrivalAt = fact.Arrival,
            DepartureAt = fact.Departure,
            BusinessDate = fact.BusinessDate,
            WalkIn = fact.WalkIn,

            // The PMS sent it, so the PMS knows it. The flag says who knows,
            // never how the guest arrived — those are two facts.
            PmsUnknown = false,

            Origin = RecordOrigin.Pms,
            CreatedAt = now,
            Version = 1,
            Terms = fact.Terms,
        };

        foreach (var reference in fact.StayRefs)
        {
            stay.ExternalRefs.Add(new StayExternalRef
            {
                Id = Guid.CreateVersion7(),
                StayId = stay.Id,
                IntegrationId = reference.IntegrationId,
                IdentifierKind = reference.IdentifierKind,
                ExternalId = reference.ExternalId,
            });
        }

        foreach (var absence in fact.Absences)
        {
            absence.Id = Guid.CreateVersion7();
            absence.StayId = stay.Id;
            absence.RecordedAt = now;
            stay.Absences.Add(absence);
        }

        if (fact.RoomId is null)
        {
            stay.Absences.Add(Absent(stay.Id, AbsentFields.Assignment, now));
        }

        if (!fact.Arrival.IsKnown && fact.Lifecycle is StayLifecycle.InHouse or StayLifecycle.Departed)
        {
            stay.Absences.Add(Absent(stay.Id, AbsentFields.ArrivalTime, now));
        }

        // Where the business came from, kept the moment it arrives.
        //
        // **A fact not recorded when it arrives is unrecoverable** — the same
        // argument the walk-in flag makes, applied to the rest of the row. A
        // channel, an agent reference, a market code and a meal plan are what
        // every hotel reports on, and no later call can reconstruct what the
        // PMS said on the night it said it.
        //
        // Written even when the source sent no segment, because the party count
        // is on the same record and a stay always has one.
        db.Sources.Add(new StaySource
        {
            StayId = stay.Id,
            Channel = fact.Segment.Channel,
            TravelAgent = fact.Segment.TravelAgent,
            MarketCode = fact.Segment.MarketCode,
            MealPlan = fact.Segment.MealPlan,
            Adults = fact.Segment.Adults,
            Children = fact.Segment.Children,
        });

        db.Stays.Add(stay);

        events.Append(scope, "stay.created", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            booking_id = booking.Id,
            property_id = stay.PropertyId,
            room_type_id = stay.RoomTypeId,
            lifecycle = stay.Lifecycle.ToString(),
            business_date = stay.BusinessDate?.ToString("yyyy-MM-dd"),
            walk_in = stay.WalkIn,
            pms_unknown = stay.PmsUnknown,
        });
    }

    /// <summary>The group this stay belongs to, found or created.</summary>
    /// <remarks>
    /// <b>The expectation is the source's and so is the completeness.</b> A
    /// source that says three rooms and sends one is telling us the group is
    /// incomplete, which is a fact about the booking rather than arithmetic we
    /// can do (R9).
    /// </remarks>
    private async Task<Booking> EnsureBookingAsync(
        InboundStayFact fact, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var reference in fact.BookingRefs)
        {
            var existing = await db.BookingExternalRefs
                .Where(r => r.IntegrationId == reference.IntegrationId
                            && r.IdentifierKind == reference.IdentifierKind
                            && r.ExternalId == reference.ExternalId)
                .Select(r => r.Booking!)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                // A later sibling arriving tells us more about the group than
                // the first one did — R9's "three expected" becoming known.
                existing.ExpectedStayCount ??= fact.ExpectedStayCount;
                existing.IsComplete ??= fact.IsComplete;
                return existing;
            }
        }

        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = fact.PropertyId,
            ExpectedStayCount = fact.ExpectedStayCount,
            IsComplete = fact.IsComplete,
            Origin = RecordOrigin.Pms,
            CreatedAt = now,
            Version = 1,
        };

        foreach (var reference in fact.BookingRefs)
        {
            booking.ExternalRefs.Add(new BookingExternalRef
            {
                Id = Guid.CreateVersion7(),
                BookingId = booking.Id,
                IntegrationId = reference.IntegrationId,
                IdentifierKind = reference.IdentifierKind,
                ExternalId = reference.ExternalId,
            });
        }

        db.Bookings.Add(booking);
        return booking;
    }


    private static StayAbsence Absent(Guid stayId, string field, DateTimeOffset now)
        => new()
        {
            Id = Guid.CreateVersion7(),
            StayId = stayId,
            Field = field,
            Reason = AbsenceReason.NotSupplied,
            RecordedAt = now,
        };
}
