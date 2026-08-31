using System.Text.Json;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Inbound;

/// <summary>
/// What the Integration Hub's facts do when they arrive.
/// </summary>
/// <remarks>
/// <para>
/// The Hub has been normalising reservation facts since the connector shipped
/// and holding them <b>deferred</b>, because their owning domain did not exist
/// (ADR 0128 §12). This application is that domain, and this is where the
/// backlog lands: in event order, through R7's one rule, idempotent by
/// construction.
/// </para>
/// <para>
/// <b>Nothing here computes a business date.</b> It arrives attached, from the
/// Hub's <c>operating_day(occurred_at, boundary)</c> — ADR 0128 §6.
/// </para>
/// <para>
/// <b>And nothing here decides a link.</b> A fact that might be a stay this
/// property created is held for a person (GUEST-Q5).
/// </para>
/// </remarks>
public sealed class InboundFactService(
    GuestOpsDbContext db,
    StayMatcher matcher,
    IEventAppender events,
    TimeProvider clock)
{
    /// <summary>Apply one normalised fact, or record why it was not applied.</summary>
    /// <param name="scope">The service scope this fact is processed under.</param>
    /// <param name="fact">The fact, in this application's terms.</param>
    /// <param name="cancellationToken">The call's token.</param>
    public async Task<InboundOutcome> ApplyAsync(
        RequestScope scope, InboundStayFact fact, CancellationToken cancellationToken)
    {
        var known = await matcher.ByReferenceAsync(fact, cancellationToken);

        if (known is not null)
        {
            var outcome = await ApplyToAsync(scope, known, fact, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return outcome;
        }

        var candidates = await matcher.CandidatesAsync(fact, cancellationToken);

        if (candidates.Count > 0)
        {
            await HoldAsync(fact, candidates, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return InboundOutcome.Held;
        }

        await CreateAsync(scope, fact, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return InboundOutcome.Created;
    }

    /// <summary>A fact about a stay this application already holds.</summary>
    private async Task<InboundOutcome> ApplyToAsync(
        RequestScope scope,
        RoomStay stay,
        InboundStayFact fact,
        CancellationToken cancellationToken)
    {
        // The override is consulted **before** the rule, because it decides
        // whether the rule's answer may be written at all. GUEST-Q3: while a
        // disagreement stands the standing override is the answer everywhere,
        // and an inbound fact does not overwrite it.
        var override_ = await db.Disagreements
            .Where(d => d.StayId == stay.Id
                        && d.Aspect == DisagreementAspect.Lifecycle
                        && (d.State == DisagreementState.Overridden
                            || d.State == DisagreementState.Standing))
            .FirstOrDefaultAsync(cancellationToken);

        if (override_ is not null)
        {
            return Reconcile(override_, stay, fact);
        }

        var decision = InboundFactRule.Decide(stay.Lifecycle, fact.Lifecycle);

        switch (decision)
        {
            case FactOutcome.Idempotent:
                return InboundOutcome.Settled;

            case FactOutcome.Contradiction:
                RecordContradiction(stay, fact);
                return InboundOutcome.Contradicted;

            default:
                Move(scope, stay, fact);
                return InboundOutcome.Applied;
        }
    }

    /// <summary>
    /// What a fact does to a stay somebody has overridden — GUEST-Q3, GUEST-Q4.
    /// </summary>
    /// <remarks>
    /// The whole of the mode, in two branches. <b>Matching settles silently</b>:
    /// agreement arriving late is not work, and flagging it would bury the real
    /// reconciliations. <b>Differing raises a disagreement and applies
    /// nothing</b>: a person decides, and until they do the desk's value is what
    /// every consumer sees.
    /// </remarks>
    private InboundOutcome Reconcile(
        StayDisagreement standing, RoomStay stay, InboundStayFact fact)
    {
        var arriving = fact.Lifecycle.ToString();

        if (standing.OurValue == arriving)
        {
            standing.State = DisagreementState.Confirmed;
            standing.PmsValue = arriving;
            standing.ClearedAt = clock.GetUtcNow();
            return InboundOutcome.Settled;
        }

        standing.State = DisagreementState.Standing;
        standing.PmsValue = arriving;
        standing.RaisedAt = clock.GetUtcNow();

        return InboundOutcome.Disagreed;
    }

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
    private async Task CreateAsync(
        RequestScope scope, InboundStayFact fact, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var booking = await EnsureBookingAsync(fact, now, cancellationToken);

        var stay = new RoomStay
        {
            Id = Uuid7.NewUuid7(),
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
                Id = Uuid7.NewUuid7(),
                StayId = stay.Id,
                IntegrationId = reference.IntegrationId,
                IdentifierKind = reference.IdentifierKind,
                ExternalId = reference.ExternalId,
            });
        }

        foreach (var absence in fact.Absences)
        {
            absence.Id = Uuid7.NewUuid7();
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
            Id = Uuid7.NewUuid7(),
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
                Id = Uuid7.NewUuid7(),
                BookingId = booking.Id,
                IntegrationId = reference.IntegrationId,
                IdentifierKind = reference.IdentifierKind,
                ExternalId = reference.ExternalId,
            });
        }

        db.Bookings.Add(booking);
        return booking;
    }

    /// <summary>Hold the fact and propose the join — never decide it.</summary>
    private async Task HoldAsync(
        InboundStayFact fact,
        IReadOnlyList<RoomStay> candidates,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var held = new HeldFact
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = fact.PropertyId,
            IntegrationId = fact.IntegrationId,
            Payload = JsonSerializer.Serialize(fact),
            Lifecycle = fact.Lifecycle,
            Reason = HeldReason.CandidateLink,
            ReceivedAt = now,
        };

        db.HeldFacts.Add(held);

        var incoming = fact.Guests.FirstOrDefault()?.NameAsGiven;

        foreach (var candidate in candidates)
        {
            var name = await db.Party
                .Where(p => p.StayId == candidate.Id)
                .Select(p => p.Guest!.NameAsGiven)
                .FirstOrDefaultAsync(cancellationToken);

            db.LinkCandidates.Add(new StayLinkCandidate
            {
                Id = Uuid7.NewUuid7(),
                LocalStayId = candidate.Id,
                HeldFactId = held.Id,

                // Ranks the list a person reads. It joins nothing, and no
                // threshold anywhere turns it into a decision.
                RankScore = StayMatcher.Similarity(name, incoming),
                State = CandidateState.Proposed,
                RaisedAt = now,
            });
        }
    }

    private void Move(RequestScope scope, RoomStay stay, InboundStayFact fact)
    {
        stay.Lifecycle = fact.Lifecycle;
        stay.Version += 1;

        // A fact that carries a time we did not have fills it; one that carries
        // an expectation never overwrites something observed, because a report
        // built on expectations measures the reservation and not the guest.
        if (!stay.ArrivalAt.IsKnown && fact.Arrival.IsKnown)
        {
            stay.ArrivalAt = fact.Arrival;
        }

        if (!stay.DepartureAt.IsKnown && fact.Departure.IsKnown)
        {
            stay.DepartureAt = fact.Departure;
        }

        var type = fact.Lifecycle switch
        {
            StayLifecycle.InHouse => "stay.arrived",
            StayLifecycle.Departed => "stay.departed",
            StayLifecycle.Cancelled => "stay.cancelled",
            StayLifecycle.NoShow => "stay.no_show",
            _ => "stay.amended",
        };

        events.Append(scope, type, "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            room_id = stay.CurrentRoomId,
            lifecycle = stay.Lifecycle.ToString(),
            business_date = stay.BusinessDate?.ToString("yyyy-MM-dd"),
        });
    }

    /// <summary>
    /// A fact that cannot move this stay — recorded for a person, applied to
    /// nothing.
    /// </summary>
    /// <remarks>
    /// S26's cancelled-in-house is the worked case. It is <b>not</b> a
    /// disagreement: there is no override and no second party — one source is
    /// contradicting itself, and GUEST-Q3's precedence rule has only one side
    /// here. It is recorded on the same row type because the desk clears it the
    /// same way, and two clearing mechanisms would drift.
    /// </remarks>
    private void RecordContradiction(RoomStay stay, InboundStayFact fact)
    {
        db.Disagreements.Add(new StayDisagreement
        {
            Id = Uuid7.NewUuid7(),
            StayId = stay.Id,
            Aspect = DisagreementAspect.Lifecycle,
            OurValue = stay.Lifecycle.ToString(),
            PmsValue = fact.Lifecycle.ToString(),
            RaisedAt = clock.GetUtcNow(),
            State = DisagreementState.Standing,
        });
    }

    private static StayAbsence Absent(Guid stayId, string field, DateTimeOffset now)
        => new()
        {
            Id = Uuid7.NewUuid7(),
            StayId = stayId,
            Field = field,
            Reason = AbsenceReason.NotSupplied,
            RecordedAt = now,
        };
}
