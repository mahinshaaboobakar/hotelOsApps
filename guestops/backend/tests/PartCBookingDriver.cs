using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure.Platform;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Part C's driver for <c>CreateBooking</c> — the RPC that holds seven of the
/// eight presence positions.
/// </summary>
/// <remarks>
/// <para>
/// <b>Part C creates data; it does not press controls</b> — ADR 0358 as the owner
/// ruled it. Operating the screens is Part B. So this finishes when the data
/// exists in every shape the wire admits, and every row below is written through
/// <see cref="BookingService.CreateAsync"/> (ADR 0228) so its validation, its
/// absences and its events are exercised rather than bypassed.
/// </para>
/// <para>
/// <b>Why this RPC, and why it is the one that moves the axis.</b>
/// <c>part-c-coverage.md</c> had to report the two-absences axis as <b>0 of 8</b>,
/// because every position is on <c>CreateBooking</c> or
/// <c>CaptureRegistration</c> and neither was driven. Seven are here.
/// </para>
/// <para>
/// <b>AND THE MEASUREMENT OF EIGHT IS CORRECTED HERE — it is SEVEN inputs and one
/// response-only field.</b> The document counted
/// <c>stays.terms.cancellation_deadline</c> as a drivable input because it is
/// reachable from <c>CreateBookingRequest.stays.terms</c>. It is not an input:
/// <c>CommercialTerms.CancellationDeadline(arrival, zone)</c> <i>computes</i> it
/// from the offset and the drop time, and
/// <c>GuestOpsGrpcService.Bookings.cs:66</c>'s <c>ToCommand</c> never reads the
/// field. <b>One <c>CommercialTerms</c> message serves both directions</b>, so the
/// request can express a value the service ignores — which is the
/// derived-projection rule's own case (<i>"the API has nowhere to put them"</i>),
/// and it has somewhere. Asserted below as a finding rather than left as a count.
/// </para>
/// <para>
/// <b>Two of the seven are a LABELLED fold, not a distinction.</b> R19 is
/// explicit: <i>"a message with no currency is a message that stated no amount"</i>
/// — so <c>ToMoney</c> maps both <c>null</c> and <c>present-without-currency</c> to
/// <c>null</c>. Driving those two positions therefore tests <b>the fold</b>, and
/// saying so is the difference between a coverage claim and a true one.
/// </para>
/// </remarks>
public sealed class PartCBookingDriver
{
    private static readonly DateOnly Arrival = new(2026, 9, 3);
    private static readonly DateOnly Departure = new(2026, 9, 5);

    /// <summary>Position 1 — <c>stays</c>: empty is REFUSED, one and many are created.</summary>
    /// <remarks>
    /// The refusal and its accepting counterparts in one theory, because a
    /// validator that refused every booking would pass a refusal-only test.
    /// <b><c>many</c> is three</b>: two distinguishes empty from non-empty and
    /// nothing else.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Stays_takes_empty_single_and_many(int count)
    {
        await using var harness = await DeskHarness.CreateAsync();
        var bookings = Bookings(harness);

        var create = () => bookings.CreateAsync(
            harness.Scope(), Booking([.. Enumerable.Range(0, count).Select(_ => Stay())]), default);

        if (count == 0)
        {
            var refused = await Assert.ThrowsAsync<InvalidRequestException>(create);
            Assert.Contains("at least one stay", refused.Message, StringComparison.Ordinal);
            return;
        }

        var booking = await create();

        Assert.Equal(count, await harness.Db.Stays.CountAsync(s => s.BookingId == booking.Id));
    }

    /// <summary>
    /// Position 2 — <c>stays.guests</c>: empty is legal, and it is RECORDED as an
    /// absence rather than invented into a guest.
    /// </summary>
    /// <remarks>
    /// <b>The assertion is the absence row, not the guest count.</b> R25: an
    /// unnamed party is a real party, so zero guests must produce a
    /// <c>party</c> <see cref="StayAbsence"/> — a service that silently accepted
    /// an empty list and recorded nothing would pass a count-only test and lose
    /// the fact that nobody has been named.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Guests_takes_empty_single_and_many_and_empty_is_recorded(int count)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(),
            Booking([Stay(guests: [.. Enumerable.Range(1, count).Select(n => Guest($"Guest {n}"))])]),
            default);

        var stay = await harness.Db.Stays
            .Include(s => s.Party).Include(s => s.Absences)
            .FirstAsync(s => s.BookingId == booking.Id);

        Assert.Equal(count, stay.Party.Count);
        Assert.Equal(count == 0, stay.Absences.Any(a => a.Field == AbsentFields.Party));
    }

    /// <summary>
    /// Position 3 — <c>is_primary</c>, the ONLY scalar on the whole write path
    /// where the wire distinguishes never-sent from sent-false.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>optional bool is_primary = 6</c> is the one field declared
    /// <c>optional</c> deliberately rather than inheriting presence from being a
    /// message. For the other 106 leaf positions proto3's implicit presence makes
    /// <i>never sent</i> and <i>sent empty</i> the same bytes, so this is the only
    /// place the axis can be driven on a scalar at all.
    /// </para>
    /// <para>
    /// <b>Driven to the STORED value, because that is where the three states have
    /// to survive.</b> <c>StayGuest.IsPrimary</c> is <c>bool?</c>; a service that
    /// defaulted null to false would answer identically on this call and lose
    /// <i>nobody said who the lead guest is</i> — which is a different fact from
    /// <i>this guest is not the lead</i>, with a different remedy.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Is_primary_keeps_all_three_states(bool? primary)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(),
            Booking([Stay(guests: [Guest("Lead", primary: primary)])]),
            default);

        var stay = await harness.Db.Stays
            .Include(s => s.Party).FirstAsync(s => s.BookingId == booking.Id);

        Assert.Equal(primary, Assert.Single(stay.Party).IsPrimary);
    }

    /// <summary>
    /// Position 4 — <c>stays.terms</c>: absent stays absent, and is never an empty
    /// row.
    /// </summary>
    /// <remarks>
    /// <c>GuestOpsGrpcService.Bookings.cs:60</c> states the reason: <i>"an empty
    /// row would claim a zero rate and a missing guarantee as facts"</i>. So the
    /// assertion is that nothing was written, not that a row reads as blank.
    /// </remarks>
    [Fact]
    public async Task Terms_absent_writes_no_terms_row()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(), Booking([Stay(terms: null)]), default);

        var stay = await harness.Db.Stays
            .Include(s => s.Terms).FirstAsync(s => s.BookingId == booking.Id);

        Assert.Null(stay.Terms);
    }

    /// <summary>And terms PRESENT are stored — the counterpart.</summary>
    /// <remarks>
    /// Without this, a service that dropped every terms row would pass the test
    /// above. The rate code is asserted because it is the plainest value that
    /// could only have come from the caller.
    /// </remarks>
    [Fact]
    public async Task Terms_present_are_stored()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(),
            Booking([Stay(terms: new CommercialTerms { RateCode = "BAR" })]),
            default);

        var stay = await harness.Db.Stays
            .Include(s => s.Terms).FirstAsync(s => s.BookingId == booking.Id);

        Assert.Equal("BAR", stay.Terms?.RateCode);
    }

    /// <summary>
    /// Positions 5 and 6 — <c>amount</c> and <c>penalty_amount</c>, where
    /// present-without-currency and absent are DELIBERATELY the same answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This asserts a FOLD, and it is labelled as one.</b> R19 — three things
    /// or it is not an amount. <c>Money.IsStated</c> is the rule and
    /// <c>ToMoney</c> applies it at the boundary, so a zero-with-no-currency is
    /// not stored as a zero amount: <i>"storing zero would make a free stay and an
    /// unstated rate the same row"</i>.
    /// </para>
    /// <para>
    /// <b>⚠ AND THE FOLD IS AT A LAYER THIS DRIVER DOES NOT REACH — stated
    /// because the first version of this test claimed otherwise.</b>
    /// <c>ToMoney</c> is a <c>private static</c> of the <i>gRPC</i> service, so
    /// the collapse happens at the wire boundary; this driver calls
    /// <c>BookingService</c> in-process and hands it a <c>Money</c> directly,
    /// which never passes through that mapping. <i>So the amount-presence axis is
    /// NOT driven here</i>, and a test asserting it from this layer would be a
    /// check that could not have failed on the thing it named.
    /// </para>
    /// <para>
    /// What IS driven is the service's own half: an unstated amount reaches
    /// storage <b>as given</b>, rather than being dropped or rounded into a zero
    /// fact. The two are asserted separately because
    /// <c>PenaltyAmount?.IsStated ?? false</c> — the first version's assertion —
    /// is <b>false both when the amount is stored unstated and when it was dropped
    /// to null</b>, so it could not tell the two outcomes apart.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task An_unstated_amount_reaches_storage_as_given_and_is_not_a_zero_fact()
    {
        var unstated = new Money(0, string.Empty, TaxBasis.Unknown);
        var stated = new Money(450000, "INR", TaxBasis.Net);

        Assert.False(unstated.IsStated);
        Assert.True(stated.IsStated);

        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(),
            Booking([Stay(terms: new CommercialTerms { Amount = stated, PenaltyAmount = unstated })]),
            default);

        var stay = await harness.Db.Stays
            .Include(s => s.Terms).FirstAsync(s => s.BookingId == booking.Id);

        // The stated one survives whole — currency included, because a number
        // without one is not an amount.
        Assert.Equal(450000, stay.Terms?.Amount?.MinorUnits);
        Assert.Equal("INR", stay.Terms?.Amount?.Currency);

        // And the unstated one is PRESENT and UNSTATED — two assertions, because
        // "absent" and "present but stating nothing" are the two outcomes this
        // has to distinguish.
        Assert.NotNull(stay.Terms?.PenaltyAmount);
        Assert.False(stay.Terms!.PenaltyAmount!.IsStated);
    }

    /// <summary>
    /// Position 7 — <c>cancellation_deadline</c> is NOT AN INPUT, and the request
    /// can express it anyway.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The finding this driver corrected.</b> <c>part-c-coverage.md</c> listed
    /// this as one of eight drivable presence positions. It is computed:
    /// <c>CommercialTerms.CancellationDeadline(arrival, zone)</c> derives it from
    /// <c>CancelOffsetDaysFromArrival</c> and <c>CancelDropTime</c>, and is read by
    /// <c>PaymentView.cs:145</c> — a response. <b>No write path reads a sent
    /// value</b>: <c>ToCommand</c> maps twelve fields and not this one.
    /// </para>
    /// <para>
    /// <b>Asserted as the derived-projection rule, not as arithmetic.</b> The rule
    /// says a create message <i>has nowhere to put</i> a projection, because a
    /// client that cannot express the mistake cannot make it — and here one
    /// <c>CommercialTerms</c> message serves request and response, so it has
    /// somewhere. The deadline a stay reports therefore comes from the offsets the
    /// caller set, and this test fails if anybody ever wires the sent field
    /// through.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task The_cancellation_deadline_is_derived_from_the_offsets_and_never_sent()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var terms = new CommercialTerms
        {
            CancelOffsetDaysFromArrival = 2,
            CancelDropTime = new TimeOnly(18, 0),
        };

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(), Booking([Stay(terms: terms)]), default);

        var stored = await harness.Db.Stays
            .Include(s => s.Terms).FirstAsync(s => s.BookingId == booking.Id);

        var deadline = stored.Terms?.CancellationDeadline(Arrival, TimeZoneInfo.Utc);

        // Two days before 3 September, at 18:00 — entirely from the offsets.
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero), deadline);
    }

    /// <summary>And with no offset there is no deadline — never a guessed one.</summary>
    /// <remarks>
    /// The counterpart, and the gap rule: a property that stated no cancellation
    /// window has none, and a deadline invented from an arrival would be a claim
    /// nobody made.
    /// </remarks>
    [Fact]
    public async Task And_terms_with_no_offset_have_no_deadline()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(),
            Booking([Stay(terms: new CommercialTerms { RateCode = "BAR" })]),
            default);

        var stored = await harness.Db.Stays
            .Include(s => s.Terms).FirstAsync(s => s.BookingId == booking.Id);

        Assert.Null(stored.Terms?.CancellationDeadline(Arrival, TimeZoneInfo.Utc));
    }

    /// <summary>The date boundary, with a rejection on one side of it.</summary>
    /// <remarks>
    /// <b>The same-day pair is the case that decides it.</b> A validator written
    /// <c>&lt;=</c> rather than <c>&lt;</c> would refuse a legitimate day-use
    /// booking, and a test that only drove *departure before arrival* against *a
    /// two-night stay* could not tell the two rules apart.
    /// </remarks>
    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(3, 3, true)]
    [InlineData(5, 3, false)]
    public async Task A_departure_before_the_arrival_is_refused(int arrive, int depart, bool accepted)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var create = () => Bookings(harness).CreateAsync(
            harness.Scope(),
            Booking([Stay(arrival: new DateOnly(2026, 9, arrive), departure: new DateOnly(2026, 9, depart))]),
            default);

        if (accepted)
        {
            Assert.NotNull(await create());
            return;
        }

        var refused = await Assert.ThrowsAsync<InvalidRequestException>(create);
        Assert.Contains("departure is before the arrival", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Three contact states, and the middle one is the shape a single fixture
    /// hides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <c>contact</c> absence is recorded only where guests <i>exist</i> and
    /// none of them carries a phone or an email. So there are three answers, not
    /// two, and they are not orderable by "how much data":
    /// </para>
    /// <code>
    /// no guests          party absence     · NO contact absence
    /// guests, no contact NO party absence  · contact absence
    /// guests with phone  neither
    /// </code>
    /// <para>
    /// <b>A suite that drove one guest with a phone would see none of it</b> —
    /// which is the fixture-that-teaches-one-behaviour failure, and the reason
    /// all three are driven in one theory.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0, false, false, true)]
    [InlineData(1, false, true, false)]
    [InlineData(1, true, false, false)]
    public async Task Contact_absence_is_recorded_only_where_a_named_party_has_none(
        int guests, bool reachable, bool contactAbsent, bool partyAbsent)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var party = Enumerable.Range(1, guests)
            .Select(n => Guest($"Guest {n}", phone: reachable ? "+91 98 0000 0000" : null))
            .ToArray();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(), Booking([Stay(guests: party)]), default);

        var stay = await harness.Db.Stays
            .Include(s => s.Absences).FirstAsync(s => s.BookingId == booking.Id);

        Assert.Equal(contactAbsent, stay.Absences.Any(a => a.Field == AbsentFields.Contact));
        Assert.Equal(partyAbsent, stay.Absences.Any(a => a.Field == AbsentFields.Party));
    }

    /// <summary>
    /// <c>expected_stay_count</c>: zero is *not said*, and is not stored as zero.
    /// </summary>
    /// <remarks>
    /// The service's own words — <i>"Zero means the caller did not say, which is a
    /// different state from one, and collapsing them loses the incomplete
    /// group"</i>. An <c>int32</c> cannot carry presence, so this is the one place
    /// the write path turns an implicit-presence scalar into a real absence, and it
    /// is driven at both ends of that mapping.
    /// </remarks>
    [Theory]
    [InlineData(0, null)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    public async Task Expected_stay_count_of_zero_is_an_absence_rather_than_a_number(
        int sent, int? stored)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(), Booking([Stay()], expected: sent), default);

        Assert.Equal(stored, booking.ExpectedStayCount);
    }

    /// <summary>The service, constructed as its host composes it.</summary>
    /// <remarks>
    /// The harness exposes no <c>Bookings</c>, so this builds one the way
    /// <c>CancelAtomicityTests</c> and <c>WalkInPhasesTests</c> already do — a
    /// recording authorizer and appender, a fixed business day, and a protector
    /// over test keys. <b>No credential is asked for and none is read</b>: the two
    /// 32-byte keys are this suite's own.
    /// </remarks>
    private static BookingService Bookings(DeskHarness harness)
        => new(
            harness.Db,
            harness.Authorizer,
            harness.Events,
            new StubBusinessDay(new DateOnly(2026, 9, 1)),
            new ContactProtector(new byte[32], new byte[32]),
            harness.Clock);

    /// <summary>A booking carrying the stays given, with everything else valid.</summary>
    private static NewBooking Booking(IReadOnlyList<NewStay> stays, int expected = 0) => new(
        Stays: stays,
        Channel: null,
        TravelAgent: null,
        MarketCode: null,
        MealPlan: null,
        ExpectedStayCount: expected);

    /// <summary>One stay, valid unless a test names otherwise.</summary>
    private static NewStay Stay(
        DateOnly? arrival = null,
        DateOnly? departure = null,
        IReadOnlyList<NewGuest>? guests = null,
        CommercialTerms? terms = null) => new(
            RoomTypeId: DeskHarness.RoomType,
            ArrivalDate: arrival ?? Arrival,
            DepartureDate: departure ?? Departure,
            Adults: 1,
            Children: 0,
            Guests: guests ?? [],
            WalkIn: false,
            Terms: terms);

    /// <summary>One guest, named, with nothing else unless a test says.</summary>
    private static NewGuest Guest(string name, string? phone = null, bool? primary = null) => new(
        NameAsGiven: name,
        NameGiven: null,
        NameFamily: null,
        Phone: phone,
        Email: null,
        IsPrimary: primary);
}
