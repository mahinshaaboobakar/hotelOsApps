namespace PmsOracle.Integrations.Cloud;

/// <summary>One typed identifier from an OHIP list.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Type">
/// What OHIP calls this kind of identifier.
/// </param>
/// <remarks>
/// <b>The <paramref name="Type"/> values are not known.</b> The reference
/// parsed this field on both the reservation and the guest profile and never
/// compared it against a literal, so the study cannot say what OHIP emits.
/// They come from vendor documentation or a live call. Nothing here guesses
/// them: the value is carried through as the connector-declared
/// <c>identifier_kind</c> (<c>CONN-Q8</c>), which is exactly what
/// "connector-declared" permits — the connector declares what the source
/// calls it.
/// </remarks>
public sealed record OhipIdentifier(string Id, string Type);

/// <summary>The room this stay currently sits in, as OHIP reports it.</summary>
/// <param name="RoomId">The PMS's room number.</param>
/// <param name="RoomType">The PMS's room-type code.</param>
public sealed record OhipRoomInfo(string? RoomId, string? RoomType);

/// <summary>How many people the stay is for.</summary>
/// <param name="Adults">Adults.</param>
/// <param name="Children">Children.</param>
public sealed record OhipGuestCounts(int Adults, int Children);

/// <summary>
/// The times OHIP <i>expects</i>, as distinct from the dates it states.
/// </summary>
/// <param name="ExpectedArrival">When the guest is due, formatted as OHIP formats it.</param>
/// <param name="ExpectedDeparture">When they are due to leave.</param>
/// <remarks>
/// R13, and the reason this is its own type rather than two more strings on
/// the stay. OHIP supplies these <b>for a stay already in house</b>, so a
/// connector that used them without saying what they were would publish an
/// expectation as an observation — and a report built on it measures the
/// reservation rather than the guest.
/// </remarks>
public sealed record OhipExpectedTimes(string? ExpectedArrival, string? ExpectedDeparture);

/// <summary>The stay's money, as OHIP states it.</summary>
/// <param name="AmountBeforeTax">
/// The total, <b>net of tax</b> — the name is the tax basis, and it is why the
/// integration's declared basis for <c>oracle-cloud</c> is net (R19).
/// </param>
public sealed record OhipTotal(decimal AmountBeforeTax);

/// <summary>The room-stay portion of an OHIP reservation.</summary>
/// <param name="CurrentRoomInfo">The room and its type.</param>
/// <param name="GuestCounts">Adults and children.</param>
/// <param name="ArrivalDate">The arrival date — a date, with no time (R12).</param>
/// <param name="DepartureDate">The departure date.</param>
/// <param name="ExpectedTimes">The expected arrival and departure times.</param>
/// <param name="Total">The stay's total.</param>
public sealed record OhipRoomStay(
    OhipRoomInfo? CurrentRoomInfo,
    OhipGuestCounts? GuestCounts,
    string? ArrivalDate,
    string? DepartureDate,
    OhipExpectedTimes? ExpectedTimes,
    OhipTotal? Total);

/// <summary>One of a profile's names, with the type OHIP assigned it.</summary>
/// <param name="GivenName">The given name.</param>
/// <param name="Surname">The family name.</param>
/// <param name="NameType">
/// OHIP's classification — the reference looked for <c>"Primary"</c> and
/// hard-failed when no entry carried it.
/// </param>
public sealed record OhipPersonName(string? GivenName, string? Surname, string? NameType);

/// <summary>A telephone number, with OHIP's two classifications of it.</summary>
/// <param name="PhoneNumber">The number.</param>
/// <param name="PhoneTechType">What kind of line — R11's typed choice.</param>
/// <param name="PhoneUseType">What it is used for.</param>
/// <param name="PrimaryInd">Whether OHIP marked it primary.</param>
public sealed record OhipTelephone(
    string? PhoneNumber,
    string? PhoneTechType,
    string? PhoneUseType,
    bool PrimaryInd);

/// <summary>An email address.</summary>
/// <param name="EmailAddress">The address.</param>
/// <param name="Type">OHIP's classification.</param>
/// <param name="PrimaryInd">Whether OHIP marked it primary.</param>
public sealed record OhipEmail(string? EmailAddress, string? Type, bool PrimaryInd);

/// <summary>A guest profile as OHIP carries it inside a reservation.</summary>
/// <param name="ProfileIdList">The profile's own typed identifiers.</param>
/// <param name="PersonNames">Every name on the profile, each typed.</param>
/// <param name="Telephones">Every telephone, each typed and flagged.</param>
/// <param name="Emails">Every email, each typed and flagged.</param>
public sealed record OhipProfile(
    IReadOnlyList<OhipIdentifier> ProfileIdList,
    IReadOnlyList<OhipPersonName> PersonNames,
    IReadOnlyList<OhipTelephone> Telephones,
    IReadOnlyList<OhipEmail> Emails);

/// <summary>One guest on a reservation.</summary>
/// <param name="Primary">Whether OHIP marked this the primary guest.</param>
/// <param name="Profile">The profile behind them.</param>
/// <remarks>
/// R11's shape: reaching a guest's name takes four filters — the primary guest,
/// then the name typed <c>Primary</c>, then the address, telephone and email
/// each flagged <c>primaryInd</c>. <b>Every one of those can be false
/// everywhere</b>, and the reference threw on the first two.
/// </remarks>
public sealed record OhipReservationGuest(bool Primary, OhipProfile? Profile);

/// <summary>
/// An OHIP reservation, as fetched after a business event named its key.
/// </summary>
/// <remarks>
/// <para>
/// Only the portion this connector consumes. The <c>reservationGuests</c>
/// sub-tree is here as of 2026-08-31: the contract gained a guest party once
/// the finding was ruled, so the shape has somewhere to go. Addresses remain
/// unmodelled — the contract carries phone and email contact points and no
/// postal address, and modelling one would be the dead code this comment
/// previously described.
/// </para>
/// <para>
/// Dates arrive as dates and times as <c>yyyy-MM-dd HH:mm:ss.S</c> — a
/// different format from the on-site flavours' <c>yyyy-MM-dd'T'HH:mm:ss</c>,
/// which is R15: the format is a property of the field, not of the vendor.
/// </para>
/// </remarks>
/// <param name="ReservationIdList">Every identifier OHIP gives this reservation, each typed.</param>
/// <param name="ReservationStatus">The OHIP status — read through the cloud vocabulary.</param>
/// <param name="RoomStay">The stay itself.</param>
/// <param name="CreateBusinessDate">
/// The hotel's operating day on which the reservation was created.
/// </param>
/// <param name="LastModifyDateTime">When OHIP last changed it.</param>
/// <param name="ReservationGuests">The party, each with a profile.</param>
public sealed record OhipReservation(
    IReadOnlyList<OhipIdentifier> ReservationIdList,
    string? ReservationStatus,
    OhipRoomStay? RoomStay,
    string? CreateBusinessDate,
    string? LastModifyDateTime,
    IReadOnlyList<OhipReservationGuest> ReservationGuests);
