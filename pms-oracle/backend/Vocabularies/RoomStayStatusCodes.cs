using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Vocabularies;

/// <summary>
/// The room-level stay statuses — which stays are touching a room today.
/// </summary>
/// <remarks>
/// <para>
/// A different vocabulary from the reservation-level one, on both flavours:
/// OHIP sends <c>reservationStatusList</c> as a real array, the on-site agent
/// sends the same idea comma-separated in one string. Both describe the
/// several stays that touch one room on one business day — one departed this
/// morning, one is staying over, one arrives this afternoon (R2).
/// </para>
/// <para>
/// <b><c>NotReserved</c> maps to no stay at all.</b> It is the source saying
/// nothing touches this room, so it contributes no entry rather than an entry
/// meaning "nothing" — an empty list already says that, and a
/// <c>STAY_LIFECYCLE_UNSPECIFIED</c> in the middle of a list would be a value
/// consumers had to learn to skip.
/// </para>
/// <para>
/// <b>One distinction is lost, and it is worth knowing about.</b>
/// <c>Arrived</c> and <c>StayOver</c> both mean a guest is in the room, so both
/// read as <c>CHECKED_IN</c>. A room-state fact alone therefore cannot say
/// whether two in-house entries are two arrivals or an arrival and a stayover —
/// a difference housekeeping cares about. The information is not gone from the
/// platform: the stays' own facts carry their arrival dates. Reported rather
/// than worked around, because inventing a room-level vocabulary that
/// duplicates <c>StayLifecycle</c> would be its own problem.
/// </para>
/// </remarks>
public static class RoomStayStatusCodes
{
    private static readonly Dictionary<string, StayLifecycle> Meanings = new(StringComparer.Ordinal)
    {
        // OHIP's spellings.
        ["Reserved"] = StayLifecycle.Booked,
        ["Arrived"] = StayLifecycle.CheckedIn,
        ["StayOver"] = StayLifecycle.CheckedIn,
        ["Departed"] = StayLifecycle.CheckedOut,

        // The on-site agent's spellings of the same axis.
        ["Arrival"] = StayLifecycle.Booked,
        ["Stayover"] = StayLifecycle.CheckedIn,
        ["Due Out"] = StayLifecycle.DueOut,
    };

    /// <summary>The values meaning "no stay touches this room".</summary>
    private static readonly HashSet<string> NoStay = new(StringComparer.Ordinal)
    {
        "NotReserved",
        "Not Reserved",
    };

    /// <summary>Every value this connector declares for the room-level axis.</summary>
    public static IReadOnlyCollection<string> Declared =>
        Meanings.Keys.Concat(NoStay).ToList();

    /// <summary>Read one room-level stay status.</summary>
    /// <param name="sourceValue">The value exactly as the source sent it.</param>
    /// <returns>
    /// The lifecycle; a recognised reading with no lifecycle for the
    /// "no stay" values, distinguished by <paramref name="contributesStay"/>;
    /// or an unrecognised reading carrying the value.
    /// </returns>
    /// <param name="contributesStay">
    /// <c>false</c> when the value means no stay touches the room, so the
    /// caller adds nothing to the list rather than adding an empty meaning.
    /// </param>
    public static Reading<StayLifecycle> Read(string sourceValue, out bool contributesStay)
    {
        if (NoStay.Contains(sourceValue))
        {
            contributesStay = false;
            return Reading<StayLifecycle>.Of(StayLifecycle.Unspecified);
        }

        contributesStay = true;

        return Meanings.TryGetValue(sourceValue, out var meaning)
            ? Reading<StayLifecycle>.Of(meaning)
            : Reading<StayLifecycle>.Unrecognised(sourceValue);
    }
}
