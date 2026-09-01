namespace HotelOS.Workforce.Domain;

/// <summary>
/// Exchanging what two people work, on the days they already hold.
/// </summary>
/// <remarks>
/// <para>
/// <b>One implementation, two callers</b>, and that is the whole reason it is
/// here rather than inline. A manager's rearrangement and an approved staff
/// proposal produce <i>the same rota</i> — they differ in who decided and in
/// what had to happen first, never in what the cells end up saying. Two copies
/// would drift, and the drift would be invisible: both would still look like a
/// swap.
/// </para>
/// <para>
/// <b>What moves is the shift.</b> The owner and the day do not: exchanging the
/// people instead would move a shift onto a day that person may already be
/// rostered on, which is a different operation with a different failure.
/// </para>
/// <para>
/// It performs no authorization and touches no database — the caller has already
/// decided who may do this, and the caller's own transaction is what makes both
/// cells change together. A half-applied swap leaves one person covering two
/// shifts and the other none.
/// </para>
/// </remarks>
public static class ShiftExchange
{
    /// <summary>Exchange two cells' shifts in place.</summary>
    /// <param name="first">One cell.</param>
    /// <param name="second">The other.</param>
    /// <param name="now">When the exchange happened.</param>
    public static void Apply(ShiftAssignment first, ShiftAssignment second, DateTimeOffset now)
    {
        (first.CatalogueEntryId, second.CatalogueEntryId) =
            (second.CatalogueEntryId, first.CatalogueEntryId);

        // The one-off span travels with the shift it belongs to. Leaving it
        // behind would give somebody another person's hours under their own
        // shift, which is neither cell's truth.
        (first.OverrideStartsAt, second.OverrideStartsAt) =
            (second.OverrideStartsAt, first.OverrideStartsAt);
        (first.OverrideEndsAt, second.OverrideEndsAt) =
            (second.OverrideEndsAt, first.OverrideEndsAt);

        first.UpdatedAt = now;
        first.Version += 1;
        second.UpdatedAt = now;
        second.Version += 1;
    }
}
