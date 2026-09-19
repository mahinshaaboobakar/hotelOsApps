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

        var answer = Wire(await new PaymentView(harness.Db)
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

    [Fact]
    public async Task Payment_refuses_a_stay_at_another_property()
    {
        // Scoped before found, which is the order that decides whether a
        // neighbouring property's folio can be read by asking for its id.
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stayId = await StayAsync(harness);
        var elsewhere = new RequestScope { PropertyId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(() => new PaymentView(harness.Db)
            .AnswerAsync(elsewhere, stayId, CancellationToken.None));
    }
}
