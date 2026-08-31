using System.Text.Json;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Inbound;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Reconciliation;

/// <summary>Which side of a disagreement a person kept.</summary>
public enum ClearSide
{
    /// <summary>The staff value stands.</summary>
    Ours = 1,

    /// <summary>The PMS's value is taken, and the correction is published.</summary>
    Pms = 2,
}

/// <summary>
/// The two decisions only a person makes — GUEST-Q3 and GUEST-Q5.
/// </summary>
/// <remarks>
/// <para>
/// <b>Both take the stay's write permission</b>, and neither has one of its
/// own. GUEST-Q3 ruled it explicitly: author-only clearing fails across shifts,
/// supervisor-only escalates a routine reconciliation, and a
/// <c>disagreement.clear</c> permission would re-introduce the escalation the
/// ruling refused. The same permission that made the override clears it.
/// </para>
/// <para>
/// <b>Neither publishes a process event.</b> Clearing to the PMS's side emits
/// the same correction a room move does, so Room Care re-plans from the event
/// stream as always and no consumer needs a special case; clearing to ours
/// publishes nothing at all, because nothing about the hotel changed.
/// </para>
/// </remarks>
public sealed class ReconciliationService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    IEventAppender events,
    TimeProvider clock)
{
    /// <summary>Decide a standing disagreement.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="disagreementId">The row being decided.</param>
    /// <param name="side">Which value stands.</param>
    /// <param name="cancellationToken">The call's token.</param>
    public async Task<StayDisagreement> ClearAsync(
        RequestScope scope,
        Guid disagreementId,
        ClearSide side,
        CancellationToken cancellationToken)
    {
        var row = await db.Disagreements
            .Include(d => d.Stay)
            .FirstOrDefaultAsync(d => d.Id == disagreementId, cancellationToken)
            ?? throw new NotFoundException("disagreement", disagreementId);

        var stay = row.Stay
            ?? throw new NotFoundException("stay", row.StayId);

        if (stay.PropertyId != scope.PropertyId)
        {
            // Another property's row is not visible, and saying so would say
            // something about another property — ADR 0009's NotFound.
            throw new NotFoundException("disagreement", disagreementId);
        }

        await authorizer.RequireAsync(
            scope, Permissions.StayWrite, ResourceTypes.Stay, stay.Id, cancellationToken);

        if (row.State is DisagreementState.ClearedOurs or DisagreementState.ClearedPms)
        {
            throw new InvalidRequestException("this disagreement has already been decided");
        }

        var now = clock.GetUtcNow();

        row.ClearedBy = scope.UserId;
        row.ClearedAt = now;

        if (side == ClearSide.Ours)
        {
            row.State = DisagreementState.ClearedOurs;
            return row;
        }

        row.State = DisagreementState.ClearedPms;

        // **Both values are kept.** The row still carries what the desk held
        // and what the PMS said, because the record of a decision that discards
        // the losing value cannot explain itself six months later.
        ApplyPmsValue(scope, row, stay);

        return row;
    }

    /// <summary>Decide whether a held fact is a stay this property created.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="candidateId">The proposed join.</param>
    /// <param name="sameStay">
    /// Whether it is the same stay. <b>False produces two stays and a
    /// double-booked room, because that is then the truth</b> (GUEST-Q5).
    /// </param>
    /// <param name="cancellationToken">The call's token.</param>
    public async Task DecideCandidateAsync(
        RequestScope scope,
        Guid candidateId,
        bool sameStay,
        CancellationToken cancellationToken)
    {
        var candidate = await db.LinkCandidates
            .Include(c => c.LocalStay)
            .FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken)
            ?? throw new NotFoundException("candidate", candidateId);

        var stay = candidate.LocalStay
            ?? throw new NotFoundException("stay", candidate.LocalStayId);

        if (stay.PropertyId != scope.PropertyId)
        {
            throw new NotFoundException("candidate", candidateId);
        }

        await authorizer.RequireAsync(
            scope, Permissions.StayWrite, ResourceTypes.Stay, stay.Id, cancellationToken);

        if (candidate.State != CandidateState.Proposed)
        {
            throw new InvalidRequestException("this candidate has already been decided");
        }

        var held = await db.HeldFacts
            .FirstOrDefaultAsync(f => f.Id == candidate.HeldFactId, cancellationToken)
            ?? throw new NotFoundException("held fact", candidate.HeldFactId);

        var now = clock.GetUtcNow();

        candidate.State = sameStay ? CandidateState.Confirmed : CandidateState.Rejected;
        candidate.DecidedBy = scope.UserId;
        candidate.DecidedAt = now;

        var fact = JsonSerializer.Deserialize<InboundStayFact>(held.Payload)
            ?? throw new InvalidOperationException(
                $"held fact {held.Id} could not be read back; it was written by this service");

        if (sameStay)
        {
            MapOntoLocalStay(scope, stay, fact, now);
        }

        // The other proposals for this fact fall away either way: it is one
        // fact, and it is now either the local stay's or its own.
        var siblings = await db.LinkCandidates
            .Where(c => c.HeldFactId == held.Id && c.Id != candidate.Id
                        && c.State == CandidateState.Proposed)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings)
        {
            sibling.State = CandidateState.Rejected;
            sibling.DecidedBy = scope.UserId;
            sibling.DecidedAt = now;
        }

        held.ResolvedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// The stay becomes the one the PMS is talking about — GUEST-Q5's "same
    /// stay".
    /// </summary>
    /// <remarks>
    /// <b>The local stay survives.</b> Its id is what Room Care, Jobs, the
    /// folio and the registration already name, so the PMS's identifiers are
    /// mapped <i>onto</i> it and the flag that said the PMS did not know it is
    /// cleared. Creating the PMS's stay and merging the other way would
    /// invalidate every reference already given out.
    /// </remarks>
    private void MapOntoLocalStay(
        RequestScope scope, RoomStay stay, InboundStayFact fact, DateTimeOffset now)
    {
        foreach (var reference in fact.StayRefs)
        {
            db.StayExternalRefs.Add(new StayExternalRef
            {
                Id = Uuid7.NewUuid7(),
                StayId = stay.Id,
                IntegrationId = reference.IntegrationId,
                IdentifierKind = reference.IdentifierKind,
                ExternalId = reference.ExternalId,
            });
        }

        stay.PmsUnknown = false;
        stay.UpdatedBy = scope.UserId;
        stay.Version += 1;

        // The lifecycle the fact carried is applied through the same rule every
        // other inbound fact obeys — a confirmation does not license skipping
        // it, and a held check-out must not silently move a booked stay past
        // its arrival.
        if (InboundFactRule.Decide(stay.Lifecycle, fact.Lifecycle) == FactOutcome.Applied)
        {
            stay.Lifecycle = fact.Lifecycle;
        }

        events.Append(scope, "stay.amended", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            lifecycle = stay.Lifecycle.ToString(),
            linked_at = now,
        });
    }

    /// <summary>Take the PMS's value, and publish the correction.</summary>
    /// <remarks>
    /// <b>The same fact a room move publishes</b>, deliberately: a consumer that
    /// already handles a room change needs nothing new, and inventing a
    /// <c>disagreement.cleared</c> subject would make every consumer learn a
    /// second way to hear the same thing.
    /// </remarks>
    private void ApplyPmsValue(RequestScope scope, StayDisagreement row, RoomStay stay)
    {
        stay.UpdatedBy = scope.UserId;
        stay.Version += 1;

        switch (row.Aspect)
        {
            case DisagreementAspect.Assignment:
                var from = stay.CurrentRoomId;
                stay.CurrentRoomId = Guid.TryParse(row.PmsValue, out var room) ? room : null;

                events.Append(scope, "stay.room_changed", "stay", stay.Id, stay.Version, new
                {
                    stay_id = stay.Id,
                    property_id = stay.PropertyId,
                    from_room_id = from,
                    to_room_id = stay.CurrentRoomId,
                    reason = AssignmentReason.Correction.ToString(),
                });
                break;

            default:
                if (Enum.TryParse<StayLifecycle>(row.PmsValue, out var lifecycle))
                {
                    stay.Lifecycle = lifecycle;
                }

                events.Append(scope, "stay.corrected", "stay", stay.Id, stay.Version, new
                {
                    stay_id = stay.Id,
                    property_id = stay.PropertyId,
                    to = stay.Lifecycle.ToString(),
                    reason = "disagreement cleared to the PMS's value",
                });
                break;
        }
    }
}
