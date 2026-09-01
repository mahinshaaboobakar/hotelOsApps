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

    public bool SignatureRequired { get; set; }

    /// <summary>Whether the card prints as part of check-in.</summary>
    public bool PrintOnCheckIn { get; set; }

    /// <summary>The registration series' prefix — the hotelier reference's <c>grcNo</c>.</summary>
    public string CardNumberPrefix { get; set; } = string.Empty;

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
}
