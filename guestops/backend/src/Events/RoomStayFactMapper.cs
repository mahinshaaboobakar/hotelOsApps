// The Hub's contract, aliased: seven of its type names also name domain
// concepts here (StayLifecycle, TimeBasis, StayGuest, ContactPoint, Money,
// CommercialTerms, Absence). The alias makes every line say which side of
// the boundary it is on, which is this file's whole subject.
using Wire = HotelOS.Contracts.Integration.V1;
using HotelOS.GuestOps.Application.Inbound;
using HotelOS.GuestOps.Domain;

namespace HotelOS.GuestOps.Events;

/// <summary>
/// The Hub's wire fact, read into this application's own terms.
/// </summary>
/// <remarks>
/// <para>
/// <b>The boundary, and the only place that knows the wire.</b>
/// <c>hotelos.integration.v1.RoomStayFact</c> is the Integration Hub's contract
/// and <b>DD's to change</b>; <see cref="InboundStayFact"/> says so in its own
/// summary. Everything past this file works in the domain's terms, exactly as
/// the gRPC layer maps a request into a command rather than handing a generated
/// message to a service — so a field DD adds or renames is a compile error
/// here and nowhere else.
/// </para>
/// <para>
/// <b>It reads the contract and never restates it.</b> The generated type is
/// referenced, not mirrored: there is no local record shaped like theirs, which
/// is the second copy that breaks silently the day the two drift.
/// </para>
/// <para>
/// <b>What it deliberately drops.</b> Provenance, the inbox record and the trace
/// stay in the Hub's inbox (ADR 0128 §5) — what reaches a rule is the fact. The
/// source's own commercial vocabulary (<c>source</c>, <c>travel_agent</c>,
/// <c>market_code</c>, <c>meal_plan</c>) is carried onto the terms where this
/// domain models it, and <c>source_detail</c> is not read here: it is the
/// Hub's keeping of what the model does not yet name, and reading it would make
/// this application depend on names nobody has agreed.
/// </para>
/// </remarks>
public static class RoomStayFactMapper
{
    /// <summary>Read one wire fact.</summary>
    /// <param name="fact">The Hub's normalised room-stay fact.</param>
    /// <returns>The same fact in this application's terms.</returns>
    /// <exception cref="ArgumentException">
    /// The header is absent, or an identifier the Hub resolves during Enrich is
    /// not a UUID. Both are the Hub contradicting its own contract, and a fact
    /// that cannot be read is not one to guess at.
    /// </exception>
    public static InboundStayFact Read(Wire.RoomStayFact fact)
    {
        var header = fact.Header
            ?? throw new ArgumentException("the fact carries no header", nameof(fact));

        return new InboundStayFact(
            IntegrationId: header.Provenance?.IntegrationId ?? string.Empty,
            PropertyId: Id(header.PropertyId, "header.property_id"),
            StayRefs: [.. fact.ExternalRefs.Select(Ref)],
            BookingRefs: [.. fact.BookingGroup?.ExternalRefs.Select(Ref) ?? []],

            // R9's `noOfRooms`. Zero is proto3's absent, and "a group of no
            // rooms" is not a thing a source means — so it reads as unstated
            // rather than as a count.
            ExpectedStayCount: fact.BookingGroup is { ExpectedRoomStays: > 0 } group
                ? group.ExpectedRoomStays
                : null,

            // The source's claim about its own group, never our arithmetic —
            // and null when there is no group to make a claim about, which is
            // different from a group claiming to be incomplete.
            IsComplete: fact.BookingGroup is null ? null : fact.BookingGroup.IsComplete,

            Lifecycle: Lifecycle(fact.Lifecycle),
            RoomTypeId: Id(fact.RoomTypeId, "room_type_id"),
            RoomId: OptionalId(fact.RoomId, "room_id"),
            Arrival: Time(fact.Arrival),
            Departure: Time(fact.Departure),
            BusinessDate: Date(header.BusinessDate),
            WalkIn: fact.WalkIn,
            Guests: [.. fact.Guests.Select(Guest)],
            Terms: Terms(fact),
            Absences: [.. header.Absences.Select(Absence)]);
    }

    /// <summary>An identifier the Hub resolved, or a refusal naming the field.</summary>
    /// <remarks>
    /// <c>room_type_id</c> and <c>property_id</c> are resolved to Master Data
    /// during Enrich, so an unparseable one is the Hub sending a code it failed
    /// to resolve. Refused loudly: a stay anchored on a guessed room type is
    /// worse than a fact that did not arrive.
    /// </remarks>
    private static Guid Id(string value, string field)
        => Guid.TryParse(value, out var parsed) && parsed != Guid.Empty
            ? parsed
            : throw new ArgumentException($"{field} is not a resolved identifier: '{value}'");

    /// <summary>An identifier that is allowed to be absent.</summary>
    /// <remarks>
    /// A room is an assignment made at check-in (GUEST-Q2's addendum), so an
    /// empty <c>room_id</c> is the ordinary case three weeks out — but a
    /// non-empty one that will not parse is still the Hub contradicting itself.
    /// </remarks>
    private static Guid? OptionalId(string value, string field)
        => string.IsNullOrEmpty(value) ? null : Id(value, field);

    private static InboundRef Ref(Wire.ExternalRef reference)
        => new(reference.IntegrationId, reference.IdentifierKind, reference.ExternalId);

    /// <summary>Where the source says the stay has reached.</summary>
    /// <remarks>
    /// <para>
    /// <b><c>DUE_OUT</c> becomes <see cref="StayLifecycle.InHouse"/>.</b> The
    /// wire keeps it as its own value because Room Care works from it — due out,
    /// dirty and not sold again tonight is a strip-the-linen decision. This
    /// domain has no such state and should not gain one: a guest due out is
    /// <i>in house</i>, and inventing a seventh lifecycle to hold another
    /// application's planning signal would put Room Care's vocabulary in the
    /// reservation book.
    /// </para>
    /// <para>
    /// <b>Unspecified is refused, not defaulted.</b> Proto3's zero is "never
    /// sent", and quietly reading it as <c>Booked</c> would create a stay the
    /// source never described.
    /// </para>
    /// </remarks>
    private static StayLifecycle Lifecycle(Wire.StayLifecycle lifecycle)
        => lifecycle switch
        {
            Wire.StayLifecycle.Booked => StayLifecycle.Booked,
            Wire.StayLifecycle.CheckedIn => StayLifecycle.InHouse,
            Wire.StayLifecycle.DueOut => StayLifecycle.InHouse,
            Wire.StayLifecycle.CheckedOut => StayLifecycle.Departed,
            Wire.StayLifecycle.Cancelled => StayLifecycle.Cancelled,
            Wire.StayLifecycle.NoShow => StayLifecycle.NoShow,
            _ => throw new ArgumentException($"the fact carries no lifecycle: {lifecycle}"),
        };

    /// <summary>A moment and how the source knows it — never one without the other.</summary>
    /// <remarks>
    /// R12/R13: a time without its basis is a time somebody later has to guess
    /// about. An absent <see cref="Wire.FactTime"/> is <see cref="StayTime.None"/>
    /// rather than a zero instant, because "not sent" and "midnight" are
    /// different facts.
    /// </remarks>
    private static StayTime Time(Wire.FactTime? time)
    {
        if (time?.At is null)
        {
            return StayTime.None;
        }

        return new StayTime(time.At.ToDateTimeOffset(), Basis(time.Basis));
    }

    private static TimeBasis Basis(Wire.TimeBasis basis)
        => basis switch
        {
            Wire.TimeBasis.Observed => TimeBasis.Observed,
            Wire.TimeBasis.Expected => TimeBasis.Expected,
            Wire.TimeBasis.Derived => TimeBasis.Derived,
            _ => TimeBasis.Unknown,
        };

    /// <summary>The Hub's business date. Attached there, never computed here.</summary>
    private static DateOnly? Date(string value)
        => DateOnly.TryParse(value, out var parsed) ? parsed : null;

    /// <summary>
    /// A member of the party, as the source reported it.
    /// </summary>
    /// <remarks>
    /// The contacts are flattened to one phone and one email because that is
    /// what this domain stores; the source's <c>tech_type</c> and <c>use_type</c>
    /// are the Hub's to keep. First of each kind wins, which is what a source
    /// listing two numbers means by order.
    /// </remarks>
    private static InboundGuest Guest(Wire.StayGuest guest)
    {
        var name = guest.Name;

        return new InboundGuest(
            NameAsGiven: name?.AsGiven ?? string.Empty,
            NameGiven: Text(name?.Given),
            NameFamily: Text(name?.Family),
            Phone: Contact(guest, Wire.ContactPoint.Types.Kind.Phone),
            Email: Contact(guest, Wire.ContactPoint.Types.Kind.Email),
            IsPrimary: guest.HasIsPrimary ? guest.IsPrimary : null);
    }

    private static string? Contact(Wire.StayGuest guest, Wire.ContactPoint.Types.Kind kind)
        => guest.Contacts.FirstOrDefault(contact => contact.Kind == kind)?.Value is { Length: > 0 } value
            ? value
            : null;

    /// <summary>
    /// What the stay was sold on, where the source sent terms — R18, GUEST-Q6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Offsets, not dates.</b> The wire carries a cancellation offset from
    /// arrival and a deposit offset from booking, and they are stored as
    /// offsets: an offset survives the arrival moving and a stored date does
    /// not.
    /// </para>
    /// <para>
    /// <b>The source's own vocabulary is not read here, and that is a gap worth
    /// naming.</b> The wire carries <c>source</c>, <c>travel_agent</c>,
    /// <c>market_code</c> and <c>meal_plan</c> — the segment every hotel reports
    /// on — and this domain models them on <c>StaySource</c>, which
    /// <see cref="InboundStayFact"/> does not carry. Dropping them silently
    /// would lose four fields the wire kept *by decision*; carrying them onto
    /// commercial terms, where they do not belong, would be worse. They need a
    /// field on the inbound fact, which is a change to that record and its
    /// rules, not to this mapper.
    /// </para>
    /// <para>
    /// <b>The wire half of that is now done — <c>CONN-Q12</c>, Stream DD.</b>
    /// The four are no longer flat fields 16-19: they are
    /// <c>RoomStayFact.commercial_segment</c>, a <c>CommercialSegment</c>
    /// message with its own rules — every value the source's own code carried
    /// verbatim, and empty meaning "not sent" rather than "none". They are
    /// deliberately <i>not</i> on <c>CommercialTerms</c>, which is what the stay
    /// was sold on. This mapper is otherwise untouched: carrying the segment
    /// onto <c>StaySource</c> is still this domain's, and still not this
    /// method's.
    /// </para>
    /// </remarks>
    private static CommercialTerms? Terms(Wire.RoomStayFact fact)
    {
        var terms = fact.CommercialTerms;

        if (terms is null
            && fact.TotalAmount is null
            && string.IsNullOrEmpty(fact.CommercialSegment?.Source))
        {
            return null;
        }

        return new CommercialTerms
        {
            RateCode = Text(terms?.RateCode),
            RateName = Text(terms?.RateName),
            Amount = Amount(fact.TotalAmount),
            GuaranteeCode = Text(terms?.GuaranteeCode),
            GuaranteeDescription = Text(terms?.GuaranteeDescription),
            OnHold = terms?.OnHold ?? false,
            ReservesInventory = terms?.ReservesInventory ?? false,
            IsDefault = terms?.IsDefault ?? false,
            DepositOffsetDaysFromBooking = Offset(terms?.DepositOffsetDaysFromBooking),
            CancelOffsetDaysFromArrival = Offset(terms?.CancelOffsetDaysFromArrival),
            CancelDropTime = TimeOnly.TryParse(terms?.CancelDropTime, out var drop) ? drop : null,
            PenaltyAmount = Amount(terms?.PenaltyAmount),
        };
    }

    /// <summary>Zero is proto3's absent, and a zero-day offset is a real value.</summary>
    /// <remarks>
    /// Read as unstated, deliberately: a source that sends no cancellation
    /// policy and one that cancels on the day of arrival are indistinguishable
    /// on this wire, and inventing the second is the more expensive mistake.
    /// This is the Hub's contract to sharpen if a property needs the difference.
    /// </remarks>
    private static int? Offset(int? days) => days is > 0 ? days : null;

    /// <summary>
    /// An amount with its currency and basis, never a bare number.
    /// </summary>
    /// <remarks>
    /// R19: net and gross written into one field is silent revenue corruption,
    /// so a money without a stated basis is not read as either.
    /// </remarks>
    private static Money? Amount(Wire.Money? money)
        => money is null || string.IsNullOrEmpty(money.Currency)
            ? null
            : new Money(money.MinorUnits, money.Currency, Tax(money.TaxBasis));

    private static TaxBasis Tax(Wire.TaxBasis basis)
        => basis switch
        {
            Wire.TaxBasis.Net => TaxBasis.Net,
            Wire.TaxBasis.Gross => TaxBasis.Gross,
            _ => TaxBasis.Unknown,
        };

    /// <summary>What the source did not supply, and why — R25.</summary>
    /// <remarks>
    /// Carried rather than dropped: an absence the source explained is evidence,
    /// and a stay whose arrival is missing because the PMS never sent one is a
    /// different record from one nobody asked about.
    /// </remarks>
    private static StayAbsence Absence(Wire.Absence absence)
        => new()
        {
            Field = absence.Field,
            Reason = absence.Reason switch
            {
                Wire.Absence.Types.Reason.NotSupplied
                    => AbsenceReason.NotSupplied,
                Wire.Absence.Types.Reason.NotAvailableFromSource
                    => AbsenceReason.NotAvailableFromSource,
                _ => AbsenceReason.Unreadable,
            },
            RawValue = Text(absence.RawValue),
        };

    /// <summary>Proto3 has no null: the empty string is absent.</summary>
    private static string? Text(string? value)
        => string.IsNullOrEmpty(value) ? null : value;
}
