using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.Cloud;
using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// Normalising an OHIP reservation — where the status decides which clock is
/// read, and an expectation is never published as an observation.
/// </summary>
public sealed class CloudNormaliserTests
{
    private static CloudNormaliser Kochi() =>
        new(new IntegrationSettings(
            IntegrationId: "oracle-cloud",
            PropertyId: "prop-kochi",
            PropertyCode: "KOCHI01",
            Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
            Currency: "INR",
            // OHIP's field is literally `amountBeforeTax`.
            AmountTaxBasis: TaxBasis.Net));

    private static OhipReservation Reservation(string status) => new(
        ReservationIdList: [new OhipIdentifier("R-88214", "Reservation")],
        ReservationStatus: status,
        RoomStay: new OhipRoomStay(
            CurrentRoomInfo: new OhipRoomInfo("402", "DLX"),
            GuestCounts: new OhipGuestCounts(2, 1),
            ArrivalDate: "2026-08-31",
            DepartureDate: "2026-09-02",
            ExpectedTimes: new OhipExpectedTimes(
                ExpectedArrival: "2026-08-31 09:12:04.0",
                ExpectedDeparture: "2026-09-02 10:45:00.0"),
            Total: new OhipTotal(18400.00m)),
        CreateBusinessDate: "2026-08-30",
        LastModifyDateTime: "2026-08-31 09:12:04.0",
        ReservationGuests: [Guest(primary: true)]);

    private static OhipReservationGuest Guest(
        bool primary,
        string? nameType = "Primary",
        bool phonePrimary = true) =>
        new(primary, new OhipProfile(
            ProfileIdList: [new OhipIdentifier("P-4412", "Profile")],
            PersonNames: [new OhipPersonName("Meera", "Rajan", nameType)],
            Telephones:
            [
                new OhipTelephone("+91 98470 11111", "MOBILE", "HOME", phonePrimary),
                new OhipTelephone("+91 48420 22222", "LANDLINE", "WORK", false),
            ],
            Emails: [new OhipEmail("meera@example.com", "PERSONAL", true)]));

    private static RoomStayFact FactFrom(OhipReservation reservation) =>
        Assert.IsType<NormalisationOutcome.StayNormalised>(Kochi().Normalise(reservation)).Fact;

    [Fact]
    public void a_reserved_booking_becomes_a_room_stay_fact()
    {
        var fact = FactFrom(Reservation("Reserved"));

        Assert.Equal(StayLifecycle.Booked, fact.Lifecycle);
        Assert.Equal("prop-kochi", fact.Header.PropertyId);
        Assert.Equal(2, fact.Adults);
        Assert.Equal(1, fact.Children);
    }

    /// <summary>
    /// R14. A booking has not arrived, so its arrival is the date completed by
    /// the property's check-in time — 14:00 in Kochi, 08:30 UTC.
    /// </summary>
    [Fact]
    public void a_booking_takes_its_arrival_from_the_property_clock()
    {
        var fact = FactFrom(Reservation("Reserved"));

        Assert.Equal(TimeBasis.Derived, fact.Arrival.Basis);
        Assert.Equal(
            new DateTime(2026, 8, 31, 8, 30, 0, DateTimeKind.Utc),
            fact.Arrival.At.ToDateTime());
    }

    /// <summary>
    /// R13, and the reason <c>TIME_BASIS_EXPECTED</c> exists. An in-house stay
    /// has an expected arrival time OHIP recorded, which is nearer the truth
    /// than 14:00 — and is still an expectation. Publishing it unmarked would
    /// make every arrival-time report measure the reservation rather than the
    /// guest.
    /// </summary>
    [Fact]
    public void an_in_house_stay_takes_its_arrival_from_the_expected_time_and_says_so()
    {
        var fact = FactFrom(Reservation("InHouse"));

        Assert.Equal(TimeBasis.Expected, fact.Arrival.Basis);

        // 09:12:04 in Asia/Kolkata is 03:42:04 UTC.
        Assert.Equal(
            new DateTime(2026, 8, 31, 3, 42, 4, DateTimeKind.Utc),
            fact.Arrival.At.ToDateTime());
    }

    /// <summary>
    /// The same reservation under two statuses yields two different arrival
    /// times, from two different fields, marked two different ways. That is
    /// R14 in one assertion.
    /// </summary>
    [Fact]
    public void the_status_decides_which_clock_the_arrival_comes_from()
    {
        var booked = FactFrom(Reservation("Reserved"));
        var inHouse = FactFrom(Reservation("InHouse"));

        Assert.NotEqual(booked.Arrival.At, inHouse.Arrival.At);
        Assert.NotEqual(booked.Arrival.Basis, inHouse.Arrival.Basis);
    }

    /// <summary>
    /// Only a departed stay has an expected departure worth preferring; an
    /// in-house one is still due out at the property's check-out time.
    /// </summary>
    [Fact]
    public void only_a_departed_stay_takes_its_departure_from_the_expected_time()
    {
        var inHouse = FactFrom(Reservation("InHouse"));
        var departed = FactFrom(Reservation("CheckedOut"));

        Assert.Equal(TimeBasis.Derived, inHouse.Departure.Basis);
        Assert.Equal(TimeBasis.Expected, departed.Departure.Basis);
    }

    /// <summary>
    /// An expected time that is absent falls back to the property clock rather
    /// than to nothing — and the fallback is marked DERIVED, so the difference
    /// stays visible.
    /// </summary>
    [Fact]
    public void an_absent_expected_time_falls_back_to_the_clock_and_is_marked_derived()
    {
        var reservation = Reservation("InHouse") with
        {
            RoomStay = Reservation("InHouse").RoomStay! with
            {
                ExpectedTimes = new OhipExpectedTimes(null, null),
            },
        };

        var fact = FactFrom(reservation);

        Assert.Equal(TimeBasis.Derived, fact.Arrival.Basis);
    }

    /// <summary>
    /// CONN-Q8. Every typed identifier is carried under the kind OHIP called
    /// it — nothing here invents a vocabulary for a system we do not own.
    /// </summary>
    [Fact]
    public void every_typed_identifier_is_carried_under_its_own_kind()
    {
        var reservation = Reservation("Reserved") with
        {
            ReservationIdList =
            [
                new OhipIdentifier("R-88214", "Reservation"),
                new OhipIdentifier("CNF-771", "Confirmation"),
            ],
        };

        var fact = FactFrom(reservation);

        Assert.Equal(2, fact.ExternalRefs.Count);
        Assert.Contains(fact.ExternalRefs, r => r.IdentifierKind == "Reservation" && r.ExternalId == "R-88214");
        Assert.Contains(fact.ExternalRefs, r => r.IdentifierKind == "Confirmation" && r.ExternalId == "CNF-771");
        Assert.All(fact.ExternalRefs, r => Assert.Equal("oracle-cloud", r.IntegrationId));
    }

    /// <summary>
    /// An identifier with no type is skipped rather than given an invented
    /// kind, which would collide with the real one the day it arrives.
    /// </summary>
    [Fact]
    public void an_identifier_without_a_kind_is_skipped()
    {
        var reservation = Reservation("Reserved") with
        {
            ReservationIdList =
            [
                new OhipIdentifier("R-88214", "Reservation"),
                new OhipIdentifier("X-1", ""),
            ],
        };

        Assert.Single(FactFrom(reservation).ExternalRefs);
    }

    [Fact]
    public void a_reservation_with_no_usable_identifier_is_rejected()
    {
        var reservation = Reservation("Reserved") with { ReservationIdList = [] };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(reservation));

        Assert.Equal(RejectionReason.MissingRequiredField, rejected.Reason);
        Assert.Equal("reservationIdList", rejected.Field);
    }

    /// <summary>
    /// OHIP's field is <c>amountBeforeTax</c>, so this integration declares net
    /// — and the fact says so rather than leaving a reader to assume.
    /// </summary>
    [Fact]
    public void the_amount_is_carried_net_because_that_is_what_ohip_sends()
    {
        var fact = FactFrom(Reservation("Reserved"));

        Assert.Equal(1_840_000, fact.TotalAmount.MinorUnits);
        Assert.Equal(TaxBasis.Net, fact.TotalAmount.TaxBasis);
    }

    [Fact]
    public void an_unknown_status_is_rejected_carrying_the_value()
    {
        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(
            Kochi().Normalise(Reservation("Waitlisted")));

        Assert.Equal(RejectionReason.UnknownStatus, rejected.Reason);
        Assert.Equal("Waitlisted", rejected.RawValue);
    }

    /// <summary>
    /// GUEST-Q2's addendum: the stay is anchored on the room type, and both
    /// flavours send the PMS's own code for Enrich to resolve.
    /// </summary>
    [Fact]
    public void the_room_type_is_carried_as_the_anchor()
    {
        Assert.Equal("DLX", FactFrom(Reservation("Reserved")).RoomTypeId);
    }

    [Fact]
    public void the_party_is_carried_with_its_names_and_typed_contacts()
    {
        var guest = Assert.Single(FactFrom(Reservation("Reserved")).Guests);

        Assert.Equal("Meera", guest.Name.Given);
        Assert.Equal("Rajan", guest.Name.Family);

        var mobile = Assert.Single(guest.Contacts, c => c.Value == "+91 98470 11111");
        Assert.Equal(ContactPoint.Types.Kind.Phone, mobile.Kind);
        Assert.Equal("MOBILE", mobile.TechType);
        Assert.Equal("HOME", mobile.UseType);
        Assert.True(mobile.IsPrimary);

        var email = Assert.Single(guest.Contacts, c => c.Kind == ContactPoint.Types.Kind.Email);
        Assert.Equal("meera@example.com", email.Value);
    }

    /// <summary>
    /// The guest's own identifiers are forwarded like the reservation's —
    /// GUEST-Q8(a), guests being the same class as stays.
    /// </summary>
    [Fact]
    public void the_guest_profile_identifier_is_forwarded_with_its_kind()
    {
        var guest = Assert.Single(FactFrom(Reservation("Reserved")).Guests);

        var reference = Assert.Single(guest.ExternalRefs);
        Assert.Equal("Profile", reference.IdentifierKind);
        Assert.Equal("P-4412", reference.ExternalId);
    }

    /// <summary>
    /// R11's hardest case, and the one the reference threw an exception on:
    /// OHIP produces reservations where nobody is marked primary. Absent says
    /// the source said nothing; false would say it said no.
    /// </summary>
    [Fact]
    public void when_nobody_is_marked_primary_the_flag_is_absent_rather_than_false()
    {
        var reservation = Reservation("Reserved") with
        {
            ReservationGuests = [Guest(primary: false), Guest(primary: false)],
        };

        var fact = FactFrom(reservation);

        Assert.Equal(2, fact.Guests.Count);
        Assert.All(fact.Guests, g => Assert.False(g.HasIsPrimary));
    }

    [Fact]
    public void when_somebody_is_marked_primary_every_guest_carries_the_answer()
    {
        var reservation = Reservation("Reserved") with
        {
            ReservationGuests = [Guest(primary: true), Guest(primary: false)],
        };

        var fact = FactFrom(reservation);

        Assert.All(fact.Guests, g => Assert.True(g.HasIsPrimary));
        Assert.True(fact.Guests[0].IsPrimary);
        Assert.False(fact.Guests[1].IsPrimary);
    }

    /// <summary>
    /// The whole party travels, not only the primary — the reference kept one
    /// guest and discarded the rest.
    /// </summary>
    [Fact]
    public void every_guest_in_the_party_is_carried()
    {
        var reservation = Reservation("Reserved") with
        {
            ReservationGuests = [Guest(primary: true), Guest(primary: false), Guest(primary: false)],
        };

        Assert.Equal(3, FactFrom(reservation).Guests.Count);
    }

    /// <summary>
    /// A profile with a name but no <c>Primary</c> type still has a name. The
    /// reference discarded the reservation over the classification.
    /// </summary>
    [Fact]
    public void a_name_without_the_primary_type_is_still_used()
    {
        var reservation = Reservation("Reserved") with
        {
            ReservationGuests = [Guest(primary: true, nameType: "Alternate")],
        };

        var guest = Assert.Single(FactFrom(reservation).Guests);
        Assert.Equal("Meera", guest.Name.Given);
    }

    /// <summary>
    /// The same tri-state applies one level down: a phone list with nothing
    /// flagged says nothing, rather than saying no.
    /// </summary>
    [Fact]
    public void when_no_phone_is_flagged_primary_the_contacts_say_nothing()
    {
        var reservation = Reservation("Reserved") with
        {
            ReservationGuests = [Guest(primary: true, phonePrimary: false)],
        };

        var guest = Assert.Single(FactFrom(reservation).Guests);
        var phones = guest.Contacts.Where(c => c.Kind == ContactPoint.Types.Kind.Phone);

        Assert.All(phones, p => Assert.False(p.HasIsPrimary));
    }

    /// <summary>
    /// The Hub's three fields stay empty here too — the boundary is the same
    /// whichever integration produced the fact.
    /// </summary>
    [Fact]
    public void the_connector_leaves_the_hub_s_fields_empty()
    {
        var fact = FactFrom(Reservation("Reserved"));

        Assert.Equal(string.Empty, fact.Header.BusinessDate);
        Assert.Null(fact.Header.Provenance);
        Assert.Equal(string.Empty, fact.RoomId);
    }
}
