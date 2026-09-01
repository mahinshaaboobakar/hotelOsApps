namespace HotelOS.Workforce.Domain;

/// <summary>
/// A shift the property offers — its identity, and how it reads.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q11</c>, owner 2026-08-31: <i>"shifts are property-created entities,
/// free-form — name, times, and a colour as a first-class attribute."</i> The
/// catalogue is the property's own and of whatever length; nothing is preset
/// beyond the starting template.
/// </para>
/// <para>
/// <b>The hours are not here.</b> They live in <see cref="ShiftHours"/>, because
/// they are effective-dated and these are not — see below. One entry, a series
/// of hours over time.
/// </para>
/// <para>
/// <b>Three attributes, three jobs</b> (<c>N2</c>): the <see cref="Name"/> is
/// what a person reads; the <see cref="ShortCode"/> is what fits a rota cell and
/// survives a monochrome photocopy; the <see cref="Colour"/> is how a week reads
/// at a glance on screen. The short code is <b>typed by the property, never
/// derived</b> — a derived initial collides the day <i>Morning</i> meets
/// <i>Mid-shift</i>, and a collision in a cell is two shifts that look identical
/// on paper.
/// </para>
/// </remarks>
public class ShiftCatalogueEntry
{
    /// <summary>The catalogue entry's stable identity.</summary>
    /// <remarks>
    /// What an assignment references — never a particular set of hours. That is
    /// what makes <c>WF-Q15</c> true by construction: an assignment for a date
    /// resolves the hours that were in force on <i>that</i> date.
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>What people read — <i>Morning</i>, <i>Split — Banquet</i>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What fits a rota cell, and survives losing every colour.</summary>
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>How the week reads at a glance. The shift's own attribute.</summary>
    /// <remarks>
    /// First-class, chosen by the property — never inferred from the code. The
    /// inverse (a colour derived from a code) is what revision 1 of the mockup
    /// drew with three hardcoded CSS classes, and it cannot express a catalogue
    /// the property invents.
    /// </remarks>
    public string Colour { get; set; } = string.Empty;

    /// <summary>Retired entries stop being offered and keep their history.</summary>
    /// <remarks>
    /// Not a delete: rotas were worked under this entry, and removing the row
    /// would leave every one of them pointing at nothing. ADR 0062's vocabulary
    /// as design language — <c>PKG-Q40</c> — with the mechanism this
    /// application's own, because the platform provides none to a package.
    /// </remarks>
    public bool Active { get; set; } = true;

    /// <summary>When it was added to the catalogue.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When its display attributes last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }
}
