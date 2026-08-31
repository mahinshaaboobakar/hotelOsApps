namespace HotelOS.GuestOps.Domain;

/// <summary>Where a room-stay has reached.</summary>
/// <remarks>
/// <para>
/// Seven states, and two of them are <b>pre-confirmation</b> — GUEST-Q9. A
/// waitlisted booking is a first-class reservation state in every major PMS and
/// the desk must see it as one; showing it as <see cref="Booked"/> would put a
/// confirmed booking on the board that nobody confirmed, and refusing the fact
/// would lose a real record (R25).
/// </para>
/// <para>
/// <b>There is no <c>DueOut</c>, deliberately.</b> The wire carries one
/// (<c>STAY_LIFECYCLE_DUE_OUT</c>) and this model composes it — in house, with a
/// departure of today. `CONN-Q11` ruled exactly this one level up: a second
/// vocabulary for an axis two existing fields already state is ADR 0020's drift,
/// one field over.
/// </para>
/// <para>
/// <b>And <c>Cancelled</c> and <c>NoShow</c> are business facts, not the
/// platform's lifecycle.</b> ADR 0062's <c>active</c> / <c>deleted_at</c> answer
/// whether a record exists, and a cancelled reservation exists — it keeps its
/// time, its reason and its penalty, and it stays in the list.
/// </para>
/// </remarks>
public enum StayLifecycle
{
    /// <summary>A queue position. The hotel is full and this holds no room.</summary>
    Waitlisted = 1,

    /// <summary>Taken, awaiting confirmation — a deposit, an approval.</summary>
    Pending = 2,

    /// <summary>Confirmed and expected.</summary>
    Booked = 3,

    /// <summary>The guest is in the room.</summary>
    InHouse = 4,

    /// <summary>The guest has left.</summary>
    Departed = 5,

    /// <summary>Cancelled before arrival.</summary>
    Cancelled = 6,

    /// <summary>Never arrived, and the day has rolled.</summary>
    NoShow = 7,
}

/// <summary>The ordering R7's one rule needs, and nothing else.</summary>
/// <remarks>
/// <para>
/// <b>Rank is not the enum's value.</b> Two states share rank 0 — a waitlist and
/// a pending booking neither of which precedes the other — and the terminals sit
/// outside the progression entirely. Deriving order from declaration order would
/// make a rename or an insertion silently change the rule.
/// </para>
/// </remarks>
public static class Lifecycle
{
    /// <summary>How far along a state is. Terminals have none.</summary>
    /// <remarks>
    /// <c>Cancelled</c> and <c>NoShow</c> are exits from the pre-arrival states
    /// rather than points on the line, so they are not ranked: asking whether a
    /// cancellation is "later than" a check-in is asking the wrong question, and
    /// the rule below answers it as a contradiction instead of a comparison.
    /// </remarks>
    public static int? RankOf(StayLifecycle state) => state switch
    {
        StayLifecycle.Waitlisted => 0,
        StayLifecycle.Pending => 0,
        StayLifecycle.Booked => 1,
        StayLifecycle.InHouse => 2,
        StayLifecycle.Departed => 3,
        _ => null,
    };

    /// <summary>Whether the stay has left the pre-arrival world for good.</summary>
    public static bool IsTerminal(StayLifecycle state)
        => state is StayLifecycle.Cancelled or StayLifecycle.NoShow;

    /// <summary>Whether a stay in this state is holding a room — GUEST-Q9.</summary>
    /// <remarks>
    /// <para>
    /// Availability subtracts *"stays holding that type on the date"*, and that
    /// phrase was unambiguous with five states and is not with seven. So it is
    /// written down once, here, rather than left to whoever writes the query.
    /// </para>
    /// <para>
    /// <b><see cref="StayLifecycle.Waitlisted"/> holds nothing, and that is the
    /// whole point of a waitlist</b> — the hotel is full, and the booking is a
    /// queue position rather than a room. Counting one would make a full hotel
    /// look oversold and hide the room a cancellation gives back, which is the
    /// exact moment the waitlist exists for.
    /// </para>
    /// <para>
    /// <b><see cref="StayLifecycle.Pending"/> holds one</b>, on the conservative
    /// reading: under-selling by one room is recoverable at the desk and
    /// over-selling is not. The source can settle it properly — R18's guarantee
    /// carries a <c>reserves_inventory</c> flag, which is precisely *"does this
    /// booking hold a room"* asked of the system that knows — and
    /// <see cref="CommercialTerms"/> carries it, so a stay with stated terms
    /// answers from them rather than from this default.
    /// </para>
    /// </remarks>
    public static bool HoldsInventory(StayLifecycle state)
        => state is StayLifecycle.Pending or StayLifecycle.Booked or StayLifecycle.InHouse;
}
