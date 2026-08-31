using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.OnSite;
using PmsOracle.Vocabularies;

namespace PmsOracle.Normalisation;

/// <summary>
/// Turns one on-site OPERA message into a normalised room-stay fact, half of
/// one, or a rejection.
/// </summary>
/// <remarks>
/// <para>
/// The connector's half of ADR 0128 §5: the mapping rules are here, the
/// pipeline that runs them is the Hub's. Three things the source determines are
/// filled in; three the Hub owns are deliberately left empty — the business
/// date, the provenance, and the canonical room id.
/// </para>
/// <para>
/// The source's identifiers are <b>forwarded, not resolved</b>. Under
/// <c>GUEST-Q8(a)</c> the Hub maps master entities and forwards operational
/// ones, so the reservation's own id travels on the fact as an
/// <see cref="ExternalRef"/> and the domain that mints the stay answers which
/// stay it is about.
/// </para>
/// </remarks>
public sealed class OnSiteNormaliser
{
    /// <summary>The on-site wire format for dates — study §5.3(d).</summary>
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss";

    /// <summary>
    /// The identifier kind for an on-site reservation number.
    /// </summary>
    /// <remarks>
    /// Connector-declared, per <c>CONN-Q8</c>. This one is known because the
    /// agent sends exactly one identifier and it is the PMS's reservation
    /// number. <b>OHIP's kinds are not known</b> — it sends a list of typed
    /// ids whose <c>type</c> values the reference parsed and never read — and
    /// they come from vendor documentation, never from the study.
    /// </remarks>
    public const string ReservationNumberKind = "reservation-number";

    private readonly IntegrationSettings _settings;

    /// <summary>Construct for one configured integration.</summary>
    /// <param name="settings">That integration's identity and property configuration.</param>
    public OnSiteNormaliser(IntegrationSettings settings) => _settings = settings;

    /// <summary>Normalise one message.</summary>
    /// <param name="push">The message as it arrived.</param>
    /// <returns>A fact, a half awaiting its partner, or a rejection naming what it could not use.</returns>
    public NormalisationOutcome Normalise(OnSitePush push)
    {
        if (!string.IsNullOrWhiteSpace(push.PropertyCode)
            && !string.Equals(push.PropertyCode, _settings.PropertyCode, StringComparison.Ordinal))
        {
            // Claimed, not believed. The ingress knows which integration this
            // arrived on; a body that says otherwise is refused rather than
            // routed.
            return Reject(RejectionReason.PropertyMismatch, "PropertyCode", push.PropertyCode);
        }

        if (string.IsNullOrWhiteSpace(push.Status))
        {
            return Reject(RejectionReason.MissingRequiredField, "Status", push.Status);
        }

        var status = OnSiteStayStatus.Read(push.Status);
        if (!status.TryGet(out var meaning))
        {
            return Reject(RejectionReason.UnknownStatus, "Status", status.UnrecognisedValue);
        }

        var arrival = ReadDate(push.ArrivalDate);

        // A half is not a fact, and is not a failure either. It waits.
        if (meaning.Part is not OnSiteMessagePart.Whole)
        {
            var key = OnSiteJoinKey.For(push.Surname, push.FirstName, arrival);

            return key is null
                ? Reject(RejectionReason.MissingRequiredField, "Surname/FirstName/ArrivalDate", null)
                : new NormalisationOutcome.AwaitingJoin(meaning.Part, key.Value);
        }

        if (string.IsNullOrWhiteSpace(push.ReservationId))
        {
            return Reject(RejectionReason.MissingRequiredField, "ReservationId", push.ReservationId);
        }

        if (arrival is null)
        {
            return Reject(RejectionReason.UnreadableValue, "ArrivalDate", push.ArrivalDate);
        }

        var departure = ReadDate(push.DepartureDate);
        if (departure is null)
        {
            return Reject(RejectionReason.UnreadableValue, "DepartureDate", push.DepartureDate);
        }

        return new NormalisationOutcome.StayNormalised(
            BuildFact(push, meaning.Lifecycle, arrival.Value, departure.Value));
    }

    private RoomStayFact BuildFact(
        OnSitePush push,
        StayLifecycle lifecycle,
        DateOnly arrival,
        DateOnly departure)
    {
        var fact = new RoomStayFact
        {
            Header = BuildHeader(push),
            Lifecycle = lifecycle,

            // Derived: the source sent a date, and the property's clock times
            // completed it. Marked as such so a consumer never mistakes an
            // inferred 14:00 arrival for an observed one (R12, R13).
            Arrival = Derived(_settings.Clock.ArrivalOn(arrival)),
            Departure = Derived(_settings.Clock.DepartureOn(departure)),

            Adults = ReadCount(push.PaxAdults),
            Children = ReadCount(push.PaxKids),
        };

        fact.ExternalRefs.Add(new ExternalRef
        {
            IntegrationId = _settings.IntegrationId,
            IdentifierKind = ReservationNumberKind,
            ExternalId = push.ReservationId,
        });

        // The room type is the anchor and the room number an assignment
        // (GUEST-Q2's addendum), so both are forwarded for Enrich to resolve.
        if (!string.IsNullOrWhiteSpace(push.RoomType))
        {
            fact.RoomTypeId = push.RoomType;
        }

        var guest = BuildGuest(push);
        if (guest is not null)
        {
            fact.Guests.Add(guest);
        }

        var amount = AmountReading.Read(push.Amount, _settings.Currency, _settings.AmountTaxBasis);
        if (amount is not null)
        {
            fact.TotalAmount = amount;
        }

        // One message describes one room, and NoOfRooms may say more (R9). The
        // group is named and marked incomplete rather than invented.
        var expected = ReadCount(push.NoOfRooms);
        if (expected > 1)
        {
            fact.BookingGroup = new BookingGroup
            {
                ExpectedRoomStays = expected,
                IsComplete = false,
            };
            fact.BookingGroup.ExternalRefs.Add(new ExternalRef
            {
                IntegrationId = _settings.IntegrationId,
                IdentifierKind = ReservationNumberKind,
                ExternalId = push.ReservationId,
            });
        }

        return fact;
    }

    /// <summary>
    /// The party this message describes, or <c>null</c> when it names nobody.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The on-site agent sends one guest per message and never marks a primary,
    /// so <c>is_primary</c> is <b>left absent rather than set true</b>. Absent
    /// says the source said nothing; <c>true</c> would be this connector
    /// answering a question the PMS never asked, and GuestOps distinguishes the
    /// two deliberately.
    /// </para>
    /// <para>
    /// The name is split because the agent splits it — <c>Surname</c> and
    /// <c>FirstName</c> are separate fields on the wire — and
    /// <c>as_given</c> stays empty because there is no unsplit form to carry.
    /// </para>
    /// </remarks>
    private static StayGuest? BuildGuest(OnSitePush push)
    {
        var hasName = !string.IsNullOrWhiteSpace(push.Surname)
            || !string.IsNullOrWhiteSpace(push.FirstName);

        var contacts = Contacts(push);

        if (!hasName && contacts.Count == 0)
        {
            return null;
        }

        var guest = new StayGuest();

        if (hasName)
        {
            guest.Name = new GuestName
            {
                Given = push.FirstName ?? string.Empty,
                Family = push.Surname ?? string.Empty,
            };
        }

        guest.Contacts.Add(contacts);

        return guest;
    }

    private static List<ContactPoint> Contacts(OnSitePush push)
    {
        var contacts = new List<ContactPoint>();

        // Phone1 before Phone2, which is the order the agent means them in —
        // but neither is marked primary, because the agent does not say.
        foreach (var number in new[] { push.Phone1, push.Phone2 })
        {
            if (!string.IsNullOrWhiteSpace(number))
            {
                contacts.Add(new ContactPoint
                {
                    Kind = ContactPoint.Types.Kind.Phone,
                    Value = number,
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(push.Email))
        {
            contacts.Add(new ContactPoint
            {
                Kind = ContactPoint.Types.Kind.Email,
                Value = push.Email,
            });
        }

        return contacts;
    }

    private FactHeader BuildHeader(OnSitePush push)
    {
        var header = new FactHeader
        {
            PropertyId = _settings.PropertyId,

            // business_date and provenance stay empty: the Hub attaches the
            // first from the property's operating-day boundary (ADR 0128 §6)
            // and the second from the inbox row.
        };

        // Absent contact details are recorded rather than dropped or invented —
        // the reference did both, on different flavours (R25).
        if (string.IsNullOrWhiteSpace(push.Phone1) && string.IsNullOrWhiteSpace(push.Phone2))
        {
            header.Absences.Add(new Absence
            {
                Field = "guest.phone",
                Reason = Absence.Types.Reason.NotSupplied,
            });
        }

        if (string.IsNullOrWhiteSpace(push.Email))
        {
            header.Absences.Add(new Absence
            {
                Field = "guest.email",
                Reason = Absence.Types.Reason.NotSupplied,
            });
        }

        return header;
    }

    private static NormalisationOutcome Reject(RejectionReason reason, string field, string? raw) =>
        new NormalisationOutcome.Rejected(reason, field, raw);

    private static FactTime Derived(DateTimeOffset at) => new()
    {
        At = Timestamp.FromDateTimeOffset(at),
        Basis = TimeBasis.Derived,
    };

    private static DateOnly? ReadDate(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && DateTime.TryParseExact(
            value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? DateOnly.FromDateTime(parsed)
            : null;

    private static int ReadCount(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
        && count >= 0
            ? count
            : 0;
}
