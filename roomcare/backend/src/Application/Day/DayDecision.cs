using HotelOS.RoomCare.Domain;

namespace HotelOS.RoomCare.Application.Day;

/// <summary>The decision — one function: a room's facts and the property's rules in, a service or pending out (S0).</summary>
/// <remarks>
/// <para>
/// Pure: no clock, no database, no event. The run that calls it stamps what it
/// saw into <see cref="DecisionInputs"/>, so a past day is never re-derived from
/// rules that have changed since.
/// </para>
/// <para>
/// <b>Nothing is dropped.</b> A room outside every rule answers
/// <see cref="Decided.Pending"/> with a sentence — "a room outside every rule
/// is a room the supervisor can see" — and a room that simply needs nothing
/// today answers null.
/// </para>
/// </remarks>
public static class DayDecision
{
    /// <summary>Decide one room for one window.</summary>
    public static Decided? Decide(DecisionFacts facts)
    {
        var room = facts.Room;
        var policy = facts.Policy;

        if (room is { IsPseudoRoom: true } || facts.Blocked)
        {
            return null;
        }

        if (facts.Window == ServiceWindowName.Evening)
        {
            return policy.TurndownEnabled && room?.Occupancy == Occupancy.Occupied
                ? Service(Domain.Service.Turndown, PriorityBand.Daily, "occupied, and the property offers turndown")
                : null;
        }

        if (room is null)
        {
            return Pending(Domain.Service.DailyService, PriorityBand.Daily, "Room Care has heard nothing about this room yet");
        }

        var soldTonight = room.NextSoldAt is { } sold && sold < facts.DayEndsAt;
        var departed = room.StayStatuses.Contains(StayStatus.CheckedOut)
            || (room.Occupancy == Occupancy.Vacant && room.Condition == Condition.Dirty);

        if (departed && room.Condition == Condition.Dirty)
        {
            if (soldTonight)
            {
                return Service(Domain.Service.DepartureClean, PriorityBand.SoldTonight, "departed and sold tonight");
            }

            return policy.UnsoldDeparture == UnsoldDeparture.MayWait
                ? Pending(Domain.Service.DepartureClean, PriorityBand.Departure, UnsoldMayWait)
                : Service(Domain.Service.DepartureClean, PriorityBand.Departure, "departed");
        }

        if (room.Occupancy == Occupancy.Occupied)
        {
            var dueOut = room.StayStatuses.Contains(StayStatus.DueOut);
            return dueOut
                ? Service(Domain.Service.DepartureClean, soldTonight ? PriorityBand.SoldTonight : PriorityBand.Departure, "due out today — clean on departure")
                : Service(Domain.Service.DailyService, PriorityBand.Daily, "occupied");
        }

        if (room.Occupancy == Occupancy.Vacant && room.Condition != Condition.Dirty)
        {
            var idle = facts.Day.DayNumber - DateOnly.FromDateTime(room.ConditionSetAt.UtcDateTime).DayNumber;
            return !soldTonight && idle >= policy.RefreshAfterDays
                ? Service(Domain.Service.Refresh, PriorityBand.Refresh, $"vacant and unsold for {idle} days")
                : null;
        }

        return room.Condition == Condition.Dirty
            ? Pending(Domain.Service.DepartureClean, soldTonight ? PriorityBand.SoldTonight : PriorityBand.Departure, "dirty, and whether anyone is in the room is not known")
            : null;
    }

    /// <summary>The linen the service must deal with — the property's rule over the room's date (S5 c4, c11).</summary>
    public static string Linen(string service, DateOnly? lastChanged, DateOnly day, PropertyPolicy policy)
    {
        if (service == Domain.Service.DepartureClean)
        {
            return LinenDue.Must;
        }

        if (service != Domain.Service.DailyService)
        {
            return LinenDue.NotDue;
        }

        var days = lastChanged is { } changed ? day.DayNumber - changed.DayNumber : int.MaxValue;
        if (days < policy.LinenEveryDays)
        {
            return LinenDue.NotDue;
        }

        return policy.LinenRuleKind == LinenRuleKind.MustByN ? LinenDue.Must : LinenDue.Due;
    }

    /// <summary>Where a band sits on the property's ladder — lower is sooner.</summary>
    public static int Rank(string band, PropertyPolicy policy)
    {
        var at = policy.PriorityLadder.IndexOf(band);
        return at < 0 ? policy.PriorityLadder.Count : at;
    }

    /// <summary>The reason an unsold departure waits for a click — named so the Pending widget can say it short.</summary>
    public const string UnsoldMayWait = "departed, not sold tonight — may wait for tomorrow";

    private static Decided Service(string service, string band, string reason) => new(service, band, false, reason);

    private static Decided Pending(string service, string band, string reason) => new(service, band, true, reason);
}

/// <summary>What the decision is given about one room.</summary>
public sealed record DecisionFacts(RoomState? Room, PropertyPolicy Policy, string Window, DateOnly Day, DateTimeOffset DayEndsAt)
{
    /// <summary>A deep clean holds the room out of the day (S0).</summary>
    public bool Blocked { get; init; }
}

/// <summary>The answer: a service at a band, or the same held pending with its reason.</summary>
public sealed record Decided(string Service, string Band, bool Pending, string Reason);
