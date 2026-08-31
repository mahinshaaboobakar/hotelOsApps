using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.OnSite;
using PmsOracle.Normalisation;
using PmsOracle.Vocabularies;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// Normalising one on-site message: the piece where the vocabulary, the clock,
/// the amount and the room-stay anchor meet.
/// </summary>
public sealed class OnSiteNormaliserTests
{
    private static OnSiteNormaliser Kochi(TaxBasis basis = TaxBasis.Net) =>
        new(new IntegrationSettings(
            IntegrationId: "oracle-onpremise",
            PropertyId: "prop-kochi",
            PropertyCode: "KOCHI01",
            Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
            Currency: "INR",
            AmountTaxBasis: basis));

    private static OnSitePush Booking() => new()
    {
        ReservationId = "R-88214",
        Status = "DUE IN",
        Surname = "RAJAN",
        FirstName = "Meera",
        ArrivalDate = "2026-08-31T00:00:00",
        DepartureDate = "2026-09-02T00:00:00",
        RoomNo = "402",
        PaxAdults = "2",
        PaxKids = "1",
        Phone1 = "+91 98470 11111",
        Email = "meera@example.com",
        Amount = "18400.00",
        NoOfRooms = "1",
        PropertyCode = "KOCHI01",
    };

    private static RoomStayFact FactFrom(OnSiteNormaliser normaliser, OnSitePush push) =>
        Assert.IsType<NormalisationOutcome.Normalised>(normaliser.Normalise(push)).Fact;

    [Fact]
    public void a_booking_becomes_a_room_stay_fact()
    {
        var fact = FactFrom(Kochi(), Booking());

        Assert.Equal("prop-kochi", fact.Header.PropertyId);
        Assert.Equal(StayLifecycle.Booked, fact.Lifecycle);
        Assert.Equal(2, fact.Adults);
        Assert.Equal(1, fact.Children);
    }

    /// <summary>
    /// The source sent dates; the property's clock completed them. Marked
    /// DERIVED so nothing downstream mistakes an inferred 14:00 for an observed
    /// arrival (R12, R13).
    /// </summary>
    [Fact]
    public void the_dates_are_completed_by_the_property_clock_and_marked_derived()
    {
        var fact = FactFrom(Kochi(), Booking());

        Assert.Equal(TimeBasis.Derived, fact.Arrival.Basis);
        Assert.Equal(TimeBasis.Derived, fact.Departure.Basis);

        // 14:00 in Asia/Kolkata is 08:30 UTC.
        Assert.Equal(
            new DateTime(2026, 8, 31, 8, 30, 0, DateTimeKind.Utc),
            fact.Arrival.At.ToDateTime());

        // 12:00 in Asia/Kolkata is 06:30 UTC.
        Assert.Equal(
            new DateTime(2026, 9, 2, 6, 30, 0, DateTimeKind.Utc),
            fact.Departure.At.ToDateTime());
    }

    /// <summary>
    /// Three fields the Hub owns, and the connector must not fill: the business
    /// date (ADR 0128 §6), the provenance, and the canonical room id.
    /// </summary>
    [Fact]
    public void the_connector_leaves_the_hub_s_fields_empty()
    {
        var fact = FactFrom(Kochi(), Booking());

        Assert.Equal(string.Empty, fact.Header.BusinessDate);
        Assert.Null(fact.Header.Provenance);
        Assert.Equal(string.Empty, fact.RoomId);
    }

    /// <summary>
    /// GUEST-Q8(a): operational identifiers are forwarded, not resolved. The
    /// kind travels with them, because it has to survive as far as the domain
    /// that mints the stay.
    /// </summary>
    [Fact]
    public void the_reservation_identifier_is_forwarded_with_its_kind()
    {
        var fact = FactFrom(Kochi(), Booking());

        var reference = Assert.Single(fact.ExternalRefs);
        Assert.Equal("oracle-onpremise", reference.IntegrationId);
        Assert.Equal(OnSiteNormaliser.ReservationNumberKind, reference.IdentifierKind);
        Assert.Equal("R-88214", reference.ExternalId);
    }

    [Fact]
    public void the_amount_carries_the_integration_s_declared_basis()
    {
        var net = FactFrom(Kochi(TaxBasis.Net), Booking());
        var gross = FactFrom(Kochi(TaxBasis.Gross), Booking());

        Assert.Equal(1_840_000, net.TotalAmount.MinorUnits);
        Assert.Equal("INR", net.TotalAmount.Currency);
        Assert.Equal(TaxBasis.Net, net.TotalAmount.TaxBasis);

        // Same message, same number, different meaning.
        Assert.Equal(TaxBasis.Gross, gross.TotalAmount.TaxBasis);
    }

    /// <summary>
    /// R9. One message describes one room while NoOfRooms says three, so the
    /// group is named and marked incomplete rather than having siblings
    /// invented for it.
    /// </summary>
    [Fact]
    public void a_multi_room_booking_names_an_incomplete_group()
    {
        var push = Booking() with { NoOfRooms = "3" };

        var fact = FactFrom(Kochi(), push);

        Assert.NotNull(fact.BookingGroup);
        Assert.Equal(3, fact.BookingGroup.ExpectedRoomStays);
        Assert.False(fact.BookingGroup.IsComplete);
    }

    [Fact]
    public void a_single_room_booking_names_no_group()
    {
        Assert.Null(FactFrom(Kochi(), Booking()).BookingGroup);
    }

    /// <summary>
    /// GUEST-Q2's addendum: the room type is the anchor, the room number an
    /// assignment. Both are on the wire for Enrich to resolve.
    /// </summary>
    [Fact]
    public void the_room_type_is_carried_as_the_anchor()
    {
        var push = Booking() with { RoomType = "DLX" };

        Assert.Equal("DLX", FactFrom(Kochi(), push).RoomTypeId);
    }

    [Fact]
    public void the_party_is_carried_with_its_name_and_contacts()
    {
        var guest = Assert.Single(FactFrom(Kochi(), Booking()).Guests);

        Assert.Equal("Meera", guest.Name.Given);
        Assert.Equal("RAJAN", guest.Name.Family);

        Assert.Contains(guest.Contacts, c =>
            c.Kind == ContactPoint.Types.Kind.Phone && c.Value == "+91 98470 11111");
        Assert.Contains(guest.Contacts, c =>
            c.Kind == ContactPoint.Types.Kind.Email && c.Value == "meera@example.com");
    }

    /// <summary>
    /// The agent never marks a primary, so the connector does not answer for
    /// it. Absent says the source said nothing; true would be this connector
    /// inventing an answer.
    /// </summary>
    [Fact]
    public void the_on_site_agent_marks_no_primary_so_the_flag_stays_absent()
    {
        var guest = Assert.Single(FactFrom(Kochi(), Booking()).Guests);

        Assert.False(guest.HasIsPrimary);
        Assert.All(guest.Contacts, c => Assert.False(c.HasIsPrimary));
    }

    /// <summary>
    /// A message naming nobody and carrying no contact detail has no party —
    /// and the absences on the header still say what is missing.
    /// </summary>
    [Fact]
    public void a_message_with_no_party_carries_none()
    {
        var push = Booking() with
        {
            Surname = null, FirstName = null, Phone1 = null, Phone2 = null, Email = null,
        };

        var fact = FactFrom(Kochi(), push);

        Assert.Empty(fact.Guests);
        Assert.Contains(fact.Header.Absences, a => a.Field == "guest.phone");
    }

    /// <summary>
    /// R25. The reference dropped such a stay on one flavour and invented an
    /// email address for it on another. Recording the absence makes both
    /// unnecessary.
    /// </summary>
    [Fact]
    public void missing_contact_details_are_recorded_rather_than_dropped_or_invented()
    {
        var push = Booking() with { Phone1 = null, Phone2 = null, Email = null };

        var fact = FactFrom(Kochi(), push);

        Assert.Contains(fact.Header.Absences, a => a.Field == "guest.phone");
        Assert.Contains(fact.Header.Absences, a => a.Field == "guest.email");
        Assert.All(
            fact.Header.Absences,
            a => Assert.Equal(Absence.Types.Reason.NotSupplied, a.Reason));
    }

    /// <summary>
    /// R6. A half is neither a fact nor a failure — it waits for its partner,
    /// and alerting on it would page someone about a message behaving exactly
    /// as designed.
    /// </summary>
    [Theory]
    [InlineData("Checked In", OnSiteMessagePart.ContactHalf)]
    [InlineData("CHECKED IN", OnSiteMessagePart.RoomHalf)]
    public void half_of_a_check_in_awaits_its_partner(string status, OnSiteMessagePart expected)
    {
        var push = Booking() with { Status = status };

        var waiting = Assert.IsType<NormalisationOutcome.AwaitingJoin>(Kochi().Normalise(push));

        Assert.Equal(expected, waiting.Part);
        Assert.Equal("RAJAN", waiting.JoinKey.Surname);
        Assert.Equal("Meera", waiting.JoinKey.FirstName);
        Assert.Equal(new DateOnly(2026, 8, 31), waiting.JoinKey.ArrivalDate);
    }

    [Fact]
    public void an_unknown_status_is_rejected_carrying_the_value()
    {
        var push = Booking() with { Status = "NO SHOW" };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(push));

        Assert.Equal(RejectionReason.UnknownStatus, rejected.Reason);
        Assert.Equal("Status", rejected.Field);
        Assert.Equal("NO SHOW", rejected.RawValue);
    }

    /// <summary>
    /// The reference took the property from the body and believed it, on an
    /// endpoint with no authentication — so a body was enough to write into any
    /// property.
    /// </summary>
    [Fact]
    public void a_message_claiming_another_property_is_rejected()
    {
        var push = Booking() with { PropertyCode = "TRIVANDRUM01" };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(push));

        Assert.Equal(RejectionReason.PropertyMismatch, rejected.Reason);
        Assert.Equal("TRIVANDRUM01", rejected.RawValue);
    }

    [Fact]
    public void an_unreadable_arrival_date_is_rejected_carrying_the_value()
    {
        var push = Booking() with { ArrivalDate = "31/08/2026" };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(push));

        Assert.Equal(RejectionReason.UnreadableValue, rejected.Reason);
        Assert.Equal("31/08/2026", rejected.RawValue);
    }

    [Fact]
    public void a_whole_message_without_a_reservation_id_is_rejected()
    {
        var push = Booking() with { ReservationId = null };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(push));

        Assert.Equal(RejectionReason.MissingRequiredField, rejected.Reason);
        Assert.Equal("ReservationId", rejected.Field);
    }

    /// <summary>
    /// A join key with a blank name would match every other blank-named
    /// message, joining unrelated guests — so it is refused rather than built.
    /// </summary>
    [Fact]
    public void a_half_without_a_usable_join_key_is_rejected()
    {
        var push = Booking() with { Status = "Checked In", Surname = null };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(push));

        Assert.Equal(RejectionReason.MissingRequiredField, rejected.Reason);
    }
}
