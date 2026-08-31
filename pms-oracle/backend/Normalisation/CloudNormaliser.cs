using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.Cloud;
using PmsOracle.Vocabularies;

namespace PmsOracle.Normalisation;

/// <summary>
/// Turns one OHIP reservation into a normalised room-stay fact, or a rejection.
/// </summary>
/// <remarks>
/// <para>
/// Two rules make this different from the on-site path, and both are why R13
/// and R14 exist. <b>Which clock to read depends on the status</b>: a booking's
/// arrival is the arrival date completed by the property's check-in time, while
/// an in-house stay's arrival is OHIP's <i>expected</i> arrival time — a
/// different field, of a different kind. And <b>an expected time is marked
/// expected</b>, so nothing downstream mistakes what the PMS planned for what
/// it saw.
/// </para>
/// <para>
/// There is no <see cref="NormalisationOutcome.AwaitingJoin"/> here. OHIP sends
/// a whole reservation in one document; the two-part check-in is the on-site
/// agent's shape alone.
/// </para>
/// </remarks>
public sealed class CloudNormaliser
{
    /// <summary>OHIP's timestamp format — study §5.3(d).</summary>
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.F";

    /// <summary>OHIP's date format.</summary>
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IntegrationSettings _settings;

    /// <summary>Construct for one configured integration.</summary>
    /// <param name="settings">That integration's identity and property configuration.</param>
    public CloudNormaliser(IntegrationSettings settings) => _settings = settings;

    /// <summary>Normalise one fetched reservation.</summary>
    /// <param name="reservation">The document OHIP returned.</param>
    /// <returns>A fact, or a rejection naming what it could not use.</returns>
    public NormalisationOutcome Normalise(OhipReservation reservation)
    {
        if (string.IsNullOrWhiteSpace(reservation.ReservationStatus))
        {
            return Reject(RejectionReason.MissingRequiredField, "reservationStatus", null);
        }

        var status = CloudStayStatus.Read(reservation.ReservationStatus);
        if (!status.TryGet(out var lifecycle))
        {
            return Reject(RejectionReason.UnknownStatus, "reservationStatus", status.UnrecognisedValue);
        }

        var stay = reservation.RoomStay;
        if (stay is null)
        {
            return Reject(RejectionReason.MissingRequiredField, "roomStay", null);
        }

        var arrivalDate = ReadDate(stay.ArrivalDate);
        if (arrivalDate is null)
        {
            return Reject(RejectionReason.UnreadableValue, "roomStay.arrivalDate", stay.ArrivalDate);
        }

        var departureDate = ReadDate(stay.DepartureDate);
        if (departureDate is null)
        {
            return Reject(RejectionReason.UnreadableValue, "roomStay.departureDate", stay.DepartureDate);
        }

        var refs = UsableIdentifiers(reservation.ReservationIdList);
        if (refs.Count == 0)
        {
            return Reject(RejectionReason.MissingRequiredField, "reservationIdList", null);
        }

        var fact = new RoomStayFact
        {
            Header = new FactHeader { PropertyId = _settings.PropertyId },
            Lifecycle = lifecycle,
            Arrival = ArrivalFor(lifecycle, arrivalDate.Value, stay.ExpectedTimes),
            Departure = DepartureFor(lifecycle, departureDate.Value, stay.ExpectedTimes),
            Adults = stay.GuestCounts?.Adults ?? 0,
            Children = stay.GuestCounts?.Children ?? 0,
        };

        fact.ExternalRefs.Add(refs);

        if (!string.IsNullOrWhiteSpace(stay.CurrentRoomInfo?.RoomType))
        {
            fact.RoomTypeId = stay.CurrentRoomInfo.RoomType;
        }

        fact.Guests.Add(BuildParty(reservation.ReservationGuests));

        var amount = ReadAmount(stay.Total);
        if (amount is not null)
        {
            fact.TotalAmount = amount;
        }

        return new NormalisationOutcome.Normalised(fact);
    }

    /// <summary>
    /// The whole party, each guest as OHIP described them — R11.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every guest is forwarded, not only the primary.</b> The reference took
    /// the primary and threw away the rest, and threw an exception outright when
    /// none was marked. Here the party travels whole and the owning domain
    /// decides what to do with it.
    /// </para>
    /// <para>
    /// <b>"Nobody is marked primary" is preserved as a state.</b> If OHIP flags
    /// nobody, <c>is_primary</c> is left absent on every guest rather than set
    /// false — absent says the source said nothing, false says it said no, and
    /// GuestOps' own nullable column exists to tell them apart.
    /// </para>
    /// </remarks>
    private static List<StayGuest> BuildParty(IReadOnlyList<OhipReservationGuest> guests)
    {
        var anyMarked = guests.Any(g => g.Primary);

        return guests.Select(g => BuildGuest(g, anyMarked)).ToList();
    }

    private static StayGuest BuildGuest(OhipReservationGuest source, bool anyMarkedPrimary)
    {
        var guest = new StayGuest();

        if (anyMarkedPrimary)
        {
            guest.IsPrimary = source.Primary;
        }

        var profile = source.Profile;
        if (profile is null)
        {
            return guest;
        }

        guest.ExternalRefs.Add(profile.ProfileIdList
            .Where(i => !string.IsNullOrWhiteSpace(i.Id) && !string.IsNullOrWhiteSpace(i.Type))
            .Select(i => new ExternalRef { IdentifierKind = i.Type, ExternalId = i.Id }));

        var name = ChooseName(profile.PersonNames);
        if (name is not null)
        {
            guest.Name = name;
        }

        guest.Contacts.Add(Contacts(profile));

        return guest;
    }

    /// <summary>
    /// The name typed <c>Primary</c>, or the first one there is.
    /// </summary>
    /// <remarks>
    /// The reference threw when no name was typed <c>Primary</c>, discarding a
    /// reservation over a classification. A profile that has a name but no
    /// typed one still has a name.
    /// </remarks>
    private static GuestName? ChooseName(IReadOnlyList<OhipPersonName> names)
    {
        var chosen = names.FirstOrDefault(n => n.NameType == "Primary")
            ?? names.FirstOrDefault(n =>
                !string.IsNullOrWhiteSpace(n.GivenName) || !string.IsNullOrWhiteSpace(n.Surname));

        return chosen is null
            ? null
            : new GuestName
            {
                Given = chosen.GivenName ?? string.Empty,
                Family = chosen.Surname ?? string.Empty,
            };
    }

    /// <summary>
    /// Every telephone and email, each with the two classifications OHIP gives
    /// it and its primary flag — R11's "typed choice among several".
    /// </summary>
    private static List<ContactPoint> Contacts(OhipProfile profile)
    {
        var contacts = new List<ContactPoint>();

        var anyPhoneMarked = profile.Telephones.Any(t => t.PrimaryInd);
        foreach (var phone in profile.Telephones.Where(t => !string.IsNullOrWhiteSpace(t.PhoneNumber)))
        {
            var contact = new ContactPoint
            {
                Kind = ContactPoint.Types.Kind.Phone,
                Value = phone.PhoneNumber,
                TechType = phone.PhoneTechType ?? string.Empty,
                UseType = phone.PhoneUseType ?? string.Empty,
            };

            if (anyPhoneMarked)
            {
                contact.IsPrimary = phone.PrimaryInd;
            }

            contacts.Add(contact);
        }

        var anyEmailMarked = profile.Emails.Any(e => e.PrimaryInd);
        foreach (var email in profile.Emails.Where(e => !string.IsNullOrWhiteSpace(e.EmailAddress)))
        {
            var contact = new ContactPoint
            {
                Kind = ContactPoint.Types.Kind.Email,
                Value = email.EmailAddress,
                UseType = email.Type ?? string.Empty,
            };

            if (anyEmailMarked)
            {
                contact.IsPrimary = email.PrimaryInd;
            }

            contacts.Add(contact);
        }

        return contacts;
    }

    /// <summary>
    /// R14 — the status decides which clock the arrival comes from.
    /// </summary>
    /// <remarks>
    /// A booking has not arrived, so its arrival is the date completed by the
    /// property's check-in time. A stay that is in house or departed has an
    /// expected arrival time OHIP recorded, and that is nearer the truth than
    /// 14:00 — but it is still an expectation, and says so.
    /// </remarks>
    private FactTime ArrivalFor(StayLifecycle lifecycle, DateOnly date, OhipExpectedTimes? expected)
    {
        if (lifecycle is StayLifecycle.CheckedIn or StayLifecycle.CheckedOut)
        {
            var at = ReadTimestamp(expected?.ExpectedArrival);
            if (at is not null)
            {
                return Expected(at.Value);
            }
        }

        return Derived(_settings.Clock.ArrivalOn(date));
    }

    /// <summary>
    /// The departure's clock, by the same rule — but only a departed stay has
    /// an expected departure time worth preferring.
    /// </summary>
    private FactTime DepartureFor(StayLifecycle lifecycle, DateOnly date, OhipExpectedTimes? expected)
    {
        if (lifecycle is StayLifecycle.CheckedOut)
        {
            var at = ReadTimestamp(expected?.ExpectedDeparture);
            if (at is not null)
            {
                return Expected(at.Value);
            }
        }

        return Derived(_settings.Clock.DepartureOn(date));
    }

    /// <summary>
    /// Carry every typed identifier OHIP gave, each under the kind OHIP called
    /// it — <c>CONN-Q8</c>, and the reason the kind is connector-declared.
    /// </summary>
    /// <remarks>
    /// An entry with no type is skipped rather than given an invented kind: a
    /// kind is what makes the mapping key bijective, and one made up here would
    /// collide with the real one the day it arrives.
    /// </remarks>
    private List<ExternalRef> UsableIdentifiers(IReadOnlyList<OhipIdentifier> identifiers) =>
        identifiers
            .Where(i => !string.IsNullOrWhiteSpace(i.Id) && !string.IsNullOrWhiteSpace(i.Type))
            .Select(i => new ExternalRef
            {
                IntegrationId = _settings.IntegrationId,
                IdentifierKind = i.Type,
                ExternalId = i.Id,
            })
            .ToList();

    private Money? ReadAmount(OhipTotal? total) =>
        total is null
            ? null
            : AmountReading.Read(
                total.AmountBeforeTax.ToString(CultureInfo.InvariantCulture),
                _settings.Currency,
                _settings.AmountTaxBasis);

    private static NormalisationOutcome Reject(RejectionReason reason, string field, string? raw) =>
        new NormalisationOutcome.Rejected(reason, field, raw);

    private static FactTime Derived(DateTimeOffset at) => new()
    {
        At = Timestamp.FromDateTimeOffset(at),
        Basis = TimeBasis.Derived,
    };

    private static FactTime Expected(DateTimeOffset at) => new()
    {
        At = Timestamp.FromDateTimeOffset(at),
        Basis = TimeBasis.Expected,
    };

    private static DateOnly? ReadDate(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && DateOnly.TryParseExact(
            value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    /// <summary>
    /// Read an OHIP timestamp into the property's zone.
    /// </summary>
    /// <remarks>
    /// OHIP sends a local wall time with no offset, so it means nothing until
    /// the property's zone is applied — which is the same reason
    /// <see cref="PropertyClock"/> refuses to exist without one.
    /// </remarks>
    private DateTimeOffset? ReadTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !DateTime.TryParseExact(
                value, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return null;
        }

        var offset = _settings.Clock.Zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }
}
