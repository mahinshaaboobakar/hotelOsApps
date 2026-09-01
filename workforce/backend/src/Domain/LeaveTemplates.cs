namespace HotelOS.Workforce.Domain;

/// <summary>One leave type a template proposes.</summary>
/// <param name="Code">Stable within the property — what a ledger entry names.</param>
/// <param name="Name">What people read.</param>
/// <param name="AccrualPerMonth">The rate, or null when it is granted rather than accrued.</param>
public sealed record LeaveTypeTemplate(string Code, string Name, decimal? AccrualPerMonth);

/// <summary>
/// The leave types a property starts with, chosen by where it is.
/// </summary>
/// <remarks>
/// <para>
/// <b>Per-property seed templates keyed off the property's own setting — never a
/// literal.</b> Ruled 2026-08-31 under the standing rule that the product is
/// sold into India <i>and</i> the GCC and no country is written into it. Chapter
/// 01 seeded <i>"the Indian-hotel defaults"</i> directly; that is what this
/// replaces.
/// </para>
/// <para>
/// The setting is <c>Property.Country</c>, which Master Data already carries and
/// this application reads. Nothing new was added to hold it — the finding was
/// that a literal had been written where a setting already existed.
/// </para>
/// <para>
/// <b>A template is a starting point, not a policy.</b> Every type is editable
/// afterwards and the property may add or retire its own; what the template
/// decides is only what a hotel sees on the first day rather than an empty
/// screen.
/// </para>
/// </remarks>
public static class LeaveTemplates
{
    /// <summary>The owner's four — the Indian-subcontinent vocabulary.</summary>
    /// <remarks>
    /// <i>"Casual, sick, earned, comp-off — all"</i>, owner 2026-08-31, with the
    /// accrual he described: <i>"monthly 2"</i>. Comp-off carries no rate because
    /// <c>WF-Q13</c> makes it granted by HR in v1 — the rota and the ledger stay
    /// uncoupled until device attendance exists.
    /// </remarks>
    private static readonly LeaveTypeTemplate[] Subcontinent =
    [
        new("CASUAL", "Casual leave", 2m),
        new("SICK", "Sick leave", 1m),
        new("EARNED", "Earned leave", 1.25m),
        new("COMPOFF", "Comp-off", null),
    ];

    /// <summary>The Gulf vocabulary, which is not the same list.</summary>
    /// <remarks>
    /// A Gulf property expects <i>Annual</i> and <i>Sick</i>; <i>Casual</i> and
    /// <i>Earned</i> are one region's words, and seeding them everywhere is the
    /// country-in-the-product mistake this exists to prevent.
    /// </remarks>
    private static readonly LeaveTypeTemplate[] Gulf =
    [
        new("ANNUAL", "Annual leave", 2.5m),
        new("SICK", "Sick leave", 1m),
        new("COMPOFF", "Comp-off", null),
    ];

    /// <summary>What a property with no country configured starts with.</summary>
    /// <remarks>
    /// <b>Neutral rather than nearest.</b> Guessing a region from a currency or a
    /// timezone would be the same mistake wearing a different field, and a hotel
    /// that has not said where it is has not said which vocabulary it uses.
    /// </remarks>
    private static readonly LeaveTypeTemplate[] Neutral =
    [
        new("ANNUAL", "Annual leave", null),
        new("SICK", "Sick leave", null),
    ];

    /// <summary>The template for a country, or the neutral one.</summary>
    /// <param name="country">The property's country — ISO alpha-2, or null.</param>
    /// <returns>The types to seed.</returns>
    public static IReadOnlyList<LeaveTypeTemplate> For(string? country) =>
        (country?.Trim().ToUpperInvariant()) switch
        {
            "IN" or "LK" or "BD" or "NP" => Subcontinent,
            "AE" or "SA" or "QA" or "OM" or "BH" or "KW" => Gulf,
            _ => Neutral,
        };
}
