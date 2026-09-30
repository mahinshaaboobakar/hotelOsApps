namespace HotelOS.GuestOps.Domain;

/// <summary>
/// Who a property's reporting obligation covers.
/// </summary>
public enum ReportingScope
{
    /// <summary>Only guests whose nationality is not the property's home country.</summary>
    FromOutside = 1,

    /// <summary>Every guest, wherever they are from.</summary>
    EveryGuest = 2,
}

/// <summary>
/// This application's own configuration, per property — §2.8.
/// </summary>
/// <remarks>
/// <para>
/// <b>An application is a bundle</b> — *UI + backend + schema + migrations +
/// permissions + events + configuration + lifecycle* (ADR 0051). This is
/// GuestOps's configuration and it is deliberately <b>not</b> Master Data's:
/// none of it describes what a property <i>is</i>. ADR 0051's test settles it —
/// uninstall every application but Core Administration and a registration
/// required-set describes nothing.
/// </para>
/// <para>
/// <b>Nothing here names a country, and that is a hard rule.</b> This
/// application is sold into India and the GCC and will be sold further; a hotel
/// in Kochi and a hotel in Dubai run the same build, each treating the other's
/// nationals as guests from outside. So *"foreign"* is never a fixed meaning in
/// the product — it is <c>nationality != HomeCountry</c>, and every list that
/// would otherwise encode one country's practice is the property's to set.
/// </para>
/// <para>
/// <b>One row per property, and it is created rather than defaulted in code.</b>
/// A property with no row has not been configured, which is a different thing
/// from a property configured to require nothing — and a service that invented
/// defaults would make those two indistinguishable at exactly the moment an
/// inspector asks why a card was blank.
/// </para>
/// </remarks>
public class GuestOpsSettings
{
    public Guid PropertyId { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2. What decides who counts as "from outside".
    /// </summary>
    /// <remarks>
    /// <b>Configuration, never a literal.</b> The one field that makes the same
    /// build serve both markets; writing a country into code here is the defect
    /// this whole type exists to prevent.
    /// </remarks>
    public string HomeCountry { get; set; } = string.Empty;

    /// <summary>Fields required of a guest whose nationality is the home country.</summary>
    /// <remarks>
    /// Stored as the property's chosen set rather than derived: what a
    /// jurisdiction demands differs by country and by property, so the product
    /// proposes a shape (<see cref="Registration"/>) and never a legal minimum.
    /// </remarks>
    public List<string> RequiredForHomeCountry { get; set; } = [];

    /// <summary>Fields required of a guest from anywhere else — set separately.</summary>
    /// <remarks>
    /// <b>Two sets, not one set plus a rule.</b> A property that asks a passport
    /// of everyone and one that asks it of visitors only are both ordinary, and
    /// a single set with an "and also, if foreign" modifier cannot say the
    /// first without saying the second.
    /// </remarks>
    public List<string> RequiredForVisitors { get; set; } = [];

    /// <summary>The property's accepted identity documents, in its own words.</summary>
    /// <remarks>
    /// Never a fixed enum in the product: Aadhaar and PAN are one country's
    /// vocabulary, an Emirates ID another's, and a passport everyone's.
    /// </remarks>
    public List<string> AcceptedIdTypes { get; set; } = [];

    /// <summary>Whether the card must be signed.</summary>
    /// <remarks>
    /// <b>`true`, because that is the value the manifest declared and an
    /// administrator approved</b> — GUEST-Q15, 2026-09-30. It read `false` (the
    /// C# default) while `manifest.yaml` declared `default: true`, so the two
    /// homes for one value disagreed; when the eight declarations left the
    /// manifest as application state, taking the C# default would have silently
    /// changed the approved answer. The declared value moved here rather than
    /// being lost with its declaration.
    /// </remarks>
    public bool SignatureRequired { get; set; } = true;

    /// <summary>Whether the card prints as part of check-in.</summary>
    public bool PrintOnCheckIn { get; set; }

    /// <summary>The registration series' prefix — the hotelier reference's <c>grcNo</c>.</summary>
    /// <remarks>
    /// <b>`"GRC-"` for the same reason as <see cref="SignatureRequired"/>:</b>
    /// the manifest declared it and this held `string.Empty`. The second of the
    /// two that disagreed, and the pair is why the defaults were migrated
    /// deliberately rather than inherited.
    /// </remarks>
    public string CardNumberPrefix { get; set; } = "GRC-";

    /// <summary>The next number in the property's own series.</summary>
    /// <remarks>
    /// Held here rather than in a database sequence because the series is the
    /// property's record-keeping artefact: it has a prefix, a reset rule and an
    /// audit trail, none of which a sequence can express — and a gap in it is a
    /// question a property gets asked.
    /// </remarks>
    public long NextCardNumber { get; set; } = 1;

    /// <summary>Whether this property files guest information with an authority.</summary>
    /// <remarks>
    /// <b>Off is a real answer.</b> A property with no obligation configures it
    /// off and no screen mentions it — the obligation is a property policy,
    /// never a country's law compiled into the product.
    /// </remarks>
    public bool ReportingRequired { get; set; }

    public ReportingScope ReportingAppliesTo { get; set; } = ReportingScope.FromOutside;

    /// <summary>Which authority, as the property names it.</summary>
    public string? ReportingAuthority { get; set; }

    /// <summary>The deadline, as hours after arrival — R18.</summary>
    /// <remarks>
    /// <b>An offset, never a stored date.</b> *"Within 24 hours of arrival"*
    /// survives the arrival moving and a stored date does not — the arrival
    /// moves often, and a deadline that silently kept pointing at the old one
    /// would be wrong in the direction that matters.
    /// </remarks>
    public int ReportingDueHours { get; set; } = 24;

    public long Version { get; set; }

    /// <summary>Whether this came from a row, or is the defaults standing in for one.</summary>
    /// <remarks>
    /// <para>
    /// <b>Not a column — EF ignores it</b> (<c>SettingsConfiguration</c>). It is
    /// the difference between *this property configured itself this way* and
    /// *nobody has configured it and these are the declared defaults*, and those
    /// are two facts with different remedies: one is settled, the other is
    /// waiting for somebody.
    /// </para>
    /// <para>
    /// <b>It exists because the alternative was inferring it from
    /// <c>Version == 0</c>.</b> That invariant is real — only <c>SaveAsync</c>
    /// creates a row and it increments on every save — but a reader meeting
    /// `Version == 0` has to reconstruct the argument, and a later change to how
    /// the version is minted would break the inference silently. Stated instead.
    /// </para>
    /// </remarks>
    public bool Stored { get; set; }

    /// <summary>The declared defaults for a property that has not configured itself.</summary>
    /// <param name="propertyId">The property they stand for.</param>
    /// <returns>An untracked instance carrying every default above, with <see cref="Stored"/> false.</returns>
    /// <remarks>
    /// <para>
    /// <b>This closes a deadlock, and the deadlock is why it exists</b> —
    /// GUEST-Q15 and ADR 0356. Reading an unconfigured property used to throw
    /// <c>NotFound</c>, which the module envelope answers 404. Five call sites
    /// read it, so ONE absent row 404'd the Setup screen, the registration card,
    /// the capture write and the card read-back — and the Setup screen is the
    /// only surface that can create the row. The one remedy was inside the blast
    /// radius.
    /// </para>
    /// <para>
    /// <b>The save was always an upsert</b> (<c>SettingsService</c> creates the
    /// row when absent), so only the read blocked: the door was unlocked and the
    /// handle was on the inside. That is why the remedy is a factory and not a
    /// migration.
    /// </para>
    /// <para>
    /// <b>These are defaults, not an invention.</b> Every value here was declared
    /// in <c>manifest.yaml</c> and approved at install; GUEST-Q15 ruled them
    /// application state, so they live here now. A screen that draws them says
    /// they are defaults — <i>rendering them as this property's settings would be
    /// the stand-in a failed read must never produce.</i>
    /// </para>
    /// </remarks>
    public static GuestOpsSettings DefaultsFor(Guid propertyId)
        => new() { PropertyId = propertyId, Stored = false };
}
