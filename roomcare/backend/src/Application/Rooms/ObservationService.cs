using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Rooms;

/// <summary>Every inbound fact about a room becomes a row, and the ordering clause decides what it changes (S4).</summary>
/// <remarks>
/// <para>The clause, as the walkthrough's worked example reads it:</para>
/// <list type="number">
/// <item>older than a deliberate act recorded here → kept as history, not applied, no flag;</item>
/// <item>agrees with the condition, or the condition was not set by a deliberate act → applied;</item>
/// <item>the property lets the PMS lead → applied, and the overwrite recorded;</item>
/// <item>a departure — the fact takes the room from occupied to vacant, or newly says checked out — → applied:
/// "checkouts, arrivals, room moves are simply applied";</item>
/// <item>otherwise, newer than a deliberate act and contradicting it → a disagreement, flagged for a supervisor.</item>
/// </list>
/// <para>
/// Occupancy, stays and the next sale are observed, never argued with: the
/// newest fact about them stands. Nothing here saves; the caller commits.
/// </para>
/// </remarks>
public sealed class ObservationService(
    RoomCareDbContext db, ConditionWriter writer, StandardReader standard, IEventAppender events)
{
    /// <summary>Record one fact and apply what the clause allows; a redelivered event changes nothing.</summary>
    public async Task<RoomObservation?> ObserveAsync(RequestScope scope, ObservedFact fact, CancellationToken cancellationToken)
    {
        if (fact.EventId is { } eventId
            && await db.Observations.AnyAsync(o => o.EventId == eventId && o.RoomId == fact.RoomId, cancellationToken))
        {
            return null;
        }

        var room = await writer.RoomAsync(scope.PropertyId, fact.RoomId, fact.OccurredAt, cancellationToken);
        var departure = IsDeparture(room, fact);
        var observation = new RoomObservation
        {
            Id = Guid.CreateVersion7(),
            RoomId = fact.RoomId,
            PropertyId = scope.PropertyId,
            Source = fact.Source,
            OccurredAt = fact.OccurredAt,
            OperatingDay = fact.OperatingDay,
            Occupancy = fact.Occupancy,
            Condition = fact.Condition,
            StayStatuses = fact.StayStatuses?.ToList(),
            NextSoldAt = fact.NextSoldAt,
            IsPseudoRoom = fact.IsPseudoRoom,
            EventId = fact.EventId,
            ByUserId = fact.ByUserId,
            Via = fact.Via,
            RecordedAt = fact.RecordedAt,
        };

        ApplyObserved(room, fact);
        if (departure)
        {
            // The stay ended, so the supervisor's standing call on it ends too —
            // "never cleared while the stay continues" (S5 c9), and it has not.
            room.SupervisedSince = null;
            room.DaysWithoutService = 0;
        }

        observation.Outcome = await ConditionOutcomeAsync(scope, room, fact, departure, cancellationToken);
        observation.Applied = observation.Outcome is not ObservationOutcome.OlderThanAct
            and not ObservationOutcome.DisagreementFlagged;
        db.Observations.Add(observation);
        return observation;
    }

    private async Task<string> ConditionOutcomeAsync(
        RequestScope scope, RoomState room, ObservedFact fact, bool departure, CancellationToken cancellationToken)
    {
        if (fact.Condition is not { } said || !Condition.All.Contains(said))
        {
            return ObservationOutcome.Applied;
        }

        var deliberate = ConditionSource.Deliberate.Contains(room.ConditionSource);
        if (deliberate && room.ConditionSetAt > fact.OccurredAt && fact.Source != ObservationSource.Manual)
        {
            return ObservationOutcome.OlderThanAct;
        }

        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        var actor = fact.ByUserId is { } person
            ? new Actor(ActorKind.User, person, fact.Via ?? Via.App)
            : new Actor(ActorKind.Application, null, Via.App);
        var change = new ConditionChange(said, SourceOf(fact.Source), actor, fact.OccurredAt)
        {
            Day = fact.OperatingDay,
            Reason = departure ? "departure" : null,
        };

        if (said == room.Condition || !deliberate || departure || fact.Source == ObservationSource.Manual)
        {
            writer.Set(scope, room, change);
            return ObservationOutcome.Applied;
        }

        if (policy.WhoLeads == WhoLeads.Pms)
        {
            writer.Set(scope, room, change with { Reason = $"the PMS leads; overwrote {room.ConditionSource.ToLowerInvariant()}" });
            return ObservationOutcome.AppliedPmsLeads;
        }

        Flag(scope, room, fact, said);
        return ObservationOutcome.DisagreementFlagged;
    }

    private void Flag(RequestScope scope, RoomState room, ObservedFact fact, string said)
    {
        room.DisagreementObservedCondition = said;
        room.DisagreementObservedAt = fact.OccurredAt;
        room.DisagreementSource = fact.Source;
        room.DisagreementClearedAt = null;
        room.DisagreementClearedBy = null;
        room.DisagreementClearedKept = null;
        room.Touch(fact.RecordedAt);

        events.Append(scope, EventTypes.DisagreementFlagged, EventTypes.RoomAggregate, room.RoomId, room.Version,
            new DisagreementAnnouncement
            {
                RoomId = room.RoomId,
                PropertyId = room.PropertyId,
                Ours = room.Condition,
                Theirs = said,
                Source = fact.Source,
                OccurredAt = fact.OccurredAt,
            });
    }

    private static void ApplyObserved(RoomState room, ObservedFact fact)
    {
        if (room.LastObservedAt is { } last && last > fact.OccurredAt)
        {
            return;
        }

        room.Occupancy = fact.Occupancy ?? room.Occupancy;
        room.StayStatuses = fact.StayStatuses?.ToList() ?? room.StayStatuses;
        room.NextSoldAt = fact.SaysNextSold ? fact.NextSoldAt : room.NextSoldAt;
        room.IsPseudoRoom = fact.IsPseudoRoom ?? room.IsPseudoRoom;
        room.LastObservedAt = fact.OccurredAt;
        room.Touch(fact.RecordedAt);
    }

    private static bool IsDeparture(RoomState room, ObservedFact fact) =>
        (room.Occupancy == Occupancy.Occupied && fact.Occupancy == Occupancy.Vacant)
        || (fact.StayStatuses?.Contains(StayStatus.CheckedOut) == true && !room.StayStatuses.Contains(StayStatus.CheckedOut));

    private static string SourceOf(string observationSource) => observationSource switch
    {
        ObservationSource.Pms => ConditionSource.Pms,
        ObservationSource.GuestOps => ConditionSource.GuestOps,
        ObservationSource.Manual => ConditionSource.Manual,
        _ => ConditionSource.System,
    };
}

/// <summary>A fact about a room, from wherever it came — the Hub, GuestOps, a person, the tick.</summary>
public sealed record ObservedFact(Guid RoomId, string Source, DateTimeOffset OccurredAt, DateTimeOffset RecordedAt)
{
    public DateOnly? OperatingDay { get; init; }

    public string? Occupancy { get; init; }

    public string? Condition { get; init; }

    public IReadOnlyList<string>? StayStatuses { get; init; }

    public DateTimeOffset? NextSoldAt { get; init; }

    /// <summary>Whether the fact speaks about the next sale at all — when it does, a null next sale clears it.</summary>
    public bool SaysNextSold { get; init; }

    public bool? IsPseudoRoom { get; init; }

    public Guid? EventId { get; init; }

    public Guid? ByUserId { get; init; }

    public string? Via { get; init; }
}
