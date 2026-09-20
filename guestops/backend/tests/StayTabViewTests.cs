using System.Text.Json;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The two tabs the Stay screen reads, and the field names it reads them by.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neither view had ever been executed.</b> `ActivityView` and `PaymentView`
/// have queried <c>GuestOpsDbContext</c> since they were written, were served by
/// <c>ModuleSurface</c> the whole time, and no test named either — the screen
/// drew a fixture instead, so nothing ever called them and nothing ever found
/// out whether they ran.
/// </para>
/// <para>
/// <b>Asserted as JSON, because JSON is the contract.</b> Both return
/// <c>object?</c>, and what crosses the module envelope is the serialised form.
/// A test that inspected the anonymous type would assert against C# property
/// names while the screen reads whatever <c>System.Text.Json</c> emitted — the
/// two agree today by convention, and a convention is not what the screen
/// depends on.
/// </para>
/// <para>
/// <b>The field names are spelled out rather than derived from the TypeScript.</b>
/// There is no shared declaration to derive from — the model lives in
/// <c>ui/book/model/tabs.ts</c> and this is .NET — so the names are written
/// here, and this file is the place a rename is caught. That is a weaker guard
/// than a generated contract and it is the honest one available: what it cannot
/// do is notice that the screen changed, which is why the names appear in a
/// test whose failure message says where else they live.
/// </para>
/// </remarks>
public sealed class StayTabViewTests
{
    /// <summary>What `ActivityEntry` in `ui/book/model/tabs.ts` reads.</summary>
    private static readonly string[] EntryFields =
        ["at", "who", "what", "detail", "disagrees"];

    /// <summary>Serialise the way the envelope does, and read it back.</summary>
    private static JsonElement Wire(object? answer)
        => JsonSerializer.SerializeToElement(answer);

    private static async Task<Guid> StayAsync(DeskHarness harness)
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = DeskHarness.Property,
            Origin = RecordOrigin.Staff,
            CreatedAt = harness.Clock.GetUtcNow(),
        };

        var stay = new RoomStay
        {
            Id = Guid.CreateVersion7(),
            BookingId = booking.Id,
            PropertyId = DeskHarness.Property,
            RoomTypeId = DeskHarness.RoomType,
            Lifecycle = StayLifecycle.InHouse,
            BusinessDate = new DateOnly(2026, 9, 1),
            ArrivalAt = StayTime.Observed(harness.Clock.GetUtcNow()),
        };

        harness.Db.Bookings.Add(booking);
        harness.Db.Stays.Add(stay);
        await harness.Db.SaveChangesAsync();

        return stay.Id;
    }

    /// <summary>One real row in the platform's own event store.</summary>
    /// <remarks>
    /// Written through the context rather than through the appender: the
    /// appender in these tests is a recording double, so a stay's history has to
    /// be put where the view actually looks — which is the platform's table, on
    /// a scratch database that now has one.
    /// </remarks>
    private static async Task RecordAsync(DeskHarness harness, Guid stayId, string type)
    {
        harness.Db.Set<StoredEvent>().Add(new StoredEvent
        {
            EventId = Guid.CreateVersion7(),
            EventType = type,
            AggregateType = "stay",
            AggregateId = stayId,
            PropertyId = DeskHarness.Property,
            EntityVersion = 1,
            OccurredAt = harness.Clock.GetUtcNow(),
            ActorType = 1,
            Source = "guestops",

            // Not optional, and the constraint is the platform's own: the
            // Kernel's migration declares `payload` NOT NULL, and a scratch
            // database provisioned from that migration rejects a row without
            // one exactly as a property would. The first version of this test
            // omitted it and found that out.
            Payload = JsonDocument.Parse("{}"),
        });

        await harness.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Activity_answers_with_the_fields_the_tab_reads()
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stayId = await StayAsync(harness);
        await RecordAsync(harness, stayId, "stay.arrived");

        var answer = Wire(await new ActivityView(harness.Db)
            .AnswerAsync(harness.Scope(), stayId, CancellationToken.None));

        Assert.True(answer.TryGetProperty("filters", out var filters));
        Assert.NotEmpty(filters.EnumerateArray());

        var entries = answer.GetProperty("entries").EnumerateArray().ToList();
        Assert.Single(entries);

        foreach (var field in EntryFields)
        {
            Assert.True(
                entries[0].TryGetProperty(field, out _),
                $"an activity entry has no '{field}' — `ActivityEntry` in "
                + "ui/book/model/tabs.ts reads it, and the Stay screen's Activity tab "
                + "would render undefined where this belongs");
        }
    }

    [Fact]
    public async Task Activity_reports_a_disagreement_as_one()
    {
        // The one field the view derives rather than copies, so it is the one
        // that can be wrong while every other name is right.
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stayId = await StayAsync(harness);
        await RecordAsync(harness, stayId, "stay.room.disagreed");

        var answer = Wire(await new ActivityView(harness.Db)
            .AnswerAsync(harness.Scope(), stayId, CancellationToken.None));

        var entry = answer.GetProperty("entries").EnumerateArray().Single();
        Assert.True(entry.GetProperty("disagrees").GetBoolean());
    }

    [Fact]
    public async Task Activity_shows_only_this_stays_history()
    {
        // A history that leaked another stay's rows would be the same defect the
        // fixture had, arriving through a real query instead of a hardcoded one.
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var mine = await StayAsync(harness);
        var theirs = await StayAsync(harness);

        await RecordAsync(harness, mine, "stay.arrived");
        await RecordAsync(harness, theirs, "stay.arrived");

        var answer = Wire(await new ActivityView(harness.Db)
            .AnswerAsync(harness.Scope(), mine, CancellationToken.None));

        Assert.Single(answer.GetProperty("entries").EnumerateArray());
    }

    /// <summary>What `Payment` in `ui/book/model/tabs.ts` reads.</summary>
    private static readonly string[] PaymentFields = ["terms", "note", "folio", "folioNote"];

    [Fact]
    public async Task Payment_answers_with_the_fields_the_tab_reads()
    {
        // No commercial terms are seeded, deliberately: `terms` is nullable in
        // the query and a stay without them is the ordinary case at a property
        // whose PMS has not sent a rate. The tab must still answer.
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stayId = await StayAsync(harness);

        var answer = Wire(await new PaymentView(harness.Db, new StubBusinessDay(new DateOnly(2026, 9, 1)))
            .AnswerAsync(harness.Scope(), stayId, CancellationToken.None));

        foreach (var field in PaymentFields)
        {
            Assert.True(
                answer.TryGetProperty(field, out _),
                $"the payment tab has no '{field}' — `Payment` in "
                + "ui/book/model/tabs.ts reads it, and the Stay screen's Payment tab "
                + "would render undefined where this belongs");
        }
    }

    /// <summary>
    /// <b>A characterisation test: it asserts today's WRONG behaviour on
    /// purpose.</b> The rate line divides minor units by one hundred whatever
    /// the currency — right for the rupee, wrong for the dinar and the yen — and
    /// a green here means the defect is still present, never that the amount is
    /// correct.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This asserts what the view does today, and it is not what the view
    /// should do.</b> Found on 2026-09-20 while applying <c>NUM-Q2</c>, and not
    /// part of that ruling: NUM-Q2 settles the wire (a decimal string and an ISO
    /// 4217 code, ADR 0175), while the exponent is wrong independently of it —
    /// <c>PaymentView.Money()</c> has always assumed two decimal places.
    /// </para>
    /// <para>
    /// <b>The three currencies are chosen so the two rules disagree</b>, which
    /// is the whole reason this test is worth writing: 1&#160;000&#160;000 minor
    /// units is 10,000.00 under either rule for the rupee, so a fixture in INR
    /// alone would pass under the defect and under its fix and could tell you
    /// nothing. The dinar has three decimal places and the yen none, so each
    /// names a different wrong answer.
    /// </para>
    /// <code>
    /// currency  exponent  minor units  rendered today  correct
    /// INR       2         999          9.99            9.99     agree
    /// KWD       3         999          9.99            0.999    differ
    /// JPY       0         999          9.99          999        differ
    /// </code>
    /// <para>
    /// <b>999, so no case needs a grouping separator.</b> The rendered text is
    /// <c>N2</c> in the server's culture, so a four-figure fixture would assert
    /// this machine's grouping and fail on a machine set to another — and the
    /// decimal separator is culture's too, which is the second half of why this
    /// line does not belong on a contract at all. The test asserts the value
    /// the view produces, and the culture-dependence is the defect rather than
    /// something the test should pin.
    /// </para>
    /// <para>
    /// <b>It is written to fail when the migration lands</b>, which is the point
    /// of recording a defect rather than describing one: whoever makes the
    /// amount exponent-aware has to come here and state the new expectation,
    /// rather than discovering months later that a folio in Kuwait was out by a
    /// factor of ten. Until then the audit carries it as owed work (U1/U2).
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("INR", "INR 9.99", "INR 9.99")]
    [InlineData("KWD", "KWD 9.99", "KWD 0.999")]
    [InlineData("JPY", "JPY 9.99", "JPY 999")]
    public async Task Characterisation_the_rate_assumes_two_decimal_places_for_every_currency(
        string currency, string renderedToday, string correctForThatCurrency)
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stayId = await StayAsync(harness);

        harness.Db.Terms.Add(new CommercialTerms
        {
            StayId = stayId,
            Amount = new Money(999, currency, TaxBasis.Gross),
        });
        await harness.Db.SaveChangesAsync();

        var answer = Wire(await new PaymentView(harness.Db, new StubBusinessDay(new DateOnly(2026, 9, 1)))
            .AnswerAsync(harness.Scope(), stayId, CancellationToken.None));

        var rate = answer.GetProperty("terms").EnumerateArray()
            .First(row => row.GetProperty("label").GetString() == "Rate")
            .GetProperty("strong").GetString();

        Assert.Equal(renderedToday, rate);

        // The fixture's own proof that it can tell the two rules apart: where
        // the expectations coincide the case establishes nothing, and this says
        // so for the two that do not.
        if (currency != "INR")
        {
            Assert.NotEqual(correctForThatCurrency, rate);
        }
    }

    [Fact]
    public async Task Payment_refuses_a_stay_at_another_property()
    {
        // Scoped before found, which is the order that decides whether a
        // neighbouring property's folio can be read by asking for its id.
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stayId = await StayAsync(harness);
        var elsewhere = new RequestScope { PropertyId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(() => new PaymentView(harness.Db, new StubBusinessDay(new DateOnly(2026, 9, 1)))
            .AnswerAsync(elsewhere, stayId, CancellationToken.None));
    }
}
