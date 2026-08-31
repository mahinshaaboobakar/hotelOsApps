namespace HotelOS.GuestOps.Domain;

/// <summary>How a timestamp came to be known — R12, R13, R14.</summary>
/// <remarks>
/// An arrival-time report built from expectations measures the reservation
/// rather than the guest, and the two differ by hours. A consumer that cannot
/// tell them apart will treat an inferred 14:00 arrival as an observed one.
/// </remarks>
public enum TimeBasis
{
    /// <summary>Nobody has recorded this time.</summary>
    Unknown = 0,

    /// <summary>Someone saw it happen — the desk checked the guest in.</summary>
    Observed = 1,

    /// <summary>The source's expectation: a scheduled or planned time.</summary>
    Expected = 2,

    /// <summary>
    /// A source date plus the property's configured clock, in the property's
    /// zone.
    /// </summary>
    /// <remarks>
    /// The zone matters and is not a detail: a derived timestamp built in UTC or
    /// from an offset carries the wrong date near midnight, and R12's whole
    /// distinction is then lost silently (R16).
    /// </remarks>
    Derived = 3,
}

/// <summary>A moment, and how it came to be known.</summary>
/// <param name="At">The instant. Null when nothing has been recorded.</param>
/// <param name="Basis">Where it came from.</param>
public sealed record StayTime(DateTimeOffset? At, TimeBasis Basis)
{
    /// <summary>Nothing recorded — distinct from a time of zero.</summary>
    /// <remarks>
    /// A value rather than <c>null</c>: the stay always has an arrival field, and
    /// what varies is whether anything is known about it. A null column pair
    /// would make *"no arrival"* and *"an arrival we cannot describe"* the same
    /// row.
    /// </remarks>
    public static StayTime None => new(null, TimeBasis.Unknown);

    /// <summary>Someone saw it.</summary>
    public static StayTime Observed(DateTimeOffset at) => new(at, TimeBasis.Observed);

    /// <summary>Whether anything is known.</summary>
    public bool IsKnown => At is not null && Basis != TimeBasis.Unknown;

    /// <summary>The date this time falls on, in its own offset.</summary>
    /// <remarks>
    /// <para>
    /// <b>The source date is not stored beside the timestamp</b> — GUEST-Q9's
    /// M6. Two columns free to disagree, with no rule saying which wins, is the
    /// defect this design warns about everywhere else; the date is a projection
    /// of the instant, computed here.
    /// </para>
    /// <para>
    /// This is exact rather than approximate <b>because of the condition
    /// specified back to the Hub</b>: a <see cref="TimeBasis.Derived"/>
    /// timestamp is constructed in the property's IANA zone, so its own date
    /// component is the date the source gave.
    /// </para>
    /// </remarks>
    public DateOnly? Date => At is { } at ? DateOnly.FromDateTime(at.DateTime) : null;
}
