namespace PmsOracle.Vocabularies;

/// <summary>
/// OHIP's reservation-status vocabulary, as <c>oracle-cloud</c> receives it.
/// </summary>
/// <remarks>
/// <para>
/// Five values, PascalCase, from the reservation document the connector fetches
/// after a business event names its primary key. This is the whole vocabulary
/// the reference handled for this flavour, and the study could not establish
/// whether OHIP emits others — every parser there ended by returning null, so
/// an unhandled value left no trace. Here it leaves a rejected record naming
/// itself (see <see cref="Reading{T}"/>).
/// </para>
/// <para>
/// <b>One declaration, read two ways.</b> <see cref="Read"/> and
/// <see cref="Declared"/> come from the same table, so the list shown on the
/// setup sheet cannot drift from the values the parser actually accepts. A
/// vocabulary written twice is a vocabulary that disagrees with itself the
/// first time somebody adds a value in a hurry.
/// </para>
/// </remarks>
public static class CloudStayStatus
{
    private static readonly Dictionary<string, StayLifecycle> Meanings = new(StringComparer.Ordinal)
    {
        ["Reserved"] = StayLifecycle.Booked,
        ["InHouse"] = StayLifecycle.CheckedIn,
        ["CheckedOut"] = StayLifecycle.CheckedOut,
        ["Cancelled"] = StayLifecycle.Cancelled,
        ["NoShow"] = StayLifecycle.NoShow,
    };

    /// <summary>Every source value this connector declares for OHIP.</summary>
    /// <remarks>
    /// What the setup sheet lists and what a capability declaration reports.
    /// Ordinal comparison: OHIP's casing is part of the value, not a
    /// presentation choice.
    /// </remarks>
    public static IReadOnlyCollection<string> Declared => Meanings.Keys;

    /// <summary>Read one OHIP <c>reservationStatus</c> value.</summary>
    /// <param name="sourceValue">The value exactly as OHIP sent it.</param>
    /// <returns>
    /// The meaning, or an unrecognised reading carrying
    /// <paramref name="sourceValue"/>.
    /// </returns>
    public static Reading<StayLifecycle> Read(string sourceValue) =>
        Meanings.TryGetValue(sourceValue, out var meaning)
            ? Reading<StayLifecycle>.Of(meaning)
            // No `default: null`. The value travels on, and the Hub rejects the
            // record naming it — requirement R5.
            : Reading<StayLifecycle>.Unrecognised(sourceValue);
}
