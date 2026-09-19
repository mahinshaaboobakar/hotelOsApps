using System.Globalization;
using System.Text.Json;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The service sends values; the screen formats them for the property.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this replaces.</b> Every view in <c>Module/</c> used to write its
/// dates and times with a fixed pattern — <c>"d MMM"</c>, <c>"HH:mm"</c> — on
/// the server's clock and in no locale anybody chose. A property in Doha read
/// its activity at UTC, a 12-hour locale read a 24-hour clock, and a property
/// with no locale established read <c>31 Aug</c> where page 64 §11 requires the
/// marked ISO form (I1, I2, I4 of the app surface checklist).
/// </para>
/// <para>
/// <b>Asserted as a value, not as a shape.</b> Each test parses what crossed the
/// envelope and compares it with what was stored. A regular expression for
/// "looks like ISO" would pass a date formatted in the wrong zone; equality
/// with the stored instant cannot.
/// </para>
/// </remarks>
public sealed class LocaleWireTests
{
    private static JsonElement Wire(object? answer) => JsonSerializer.SerializeToElement(answer);

    /// <summary>An instant as the wire carries it, read back exactly.</summary>
    private static DateTimeOffset Instant(JsonElement field)
        => DateTimeOffset.ParseExact(
            field.GetString()!, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

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

    [Fact]
    public async Task Activity_sends_the_instant_and_no_rendering_of_it()
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stayId = await StayAsync(harness);

        // 23:40 UTC is the next day in any zone east of it — the case a
        // server-side "d MMM" got wrong for every property in India or the Gulf.
        var occurred = new DateTimeOffset(2026, 8, 31, 23, 40, 0, TimeSpan.Zero);
        harness.Db.Set<StoredEvent>().Add(new StoredEvent
        {
            EventId = Guid.CreateVersion7(),
            EventType = "stay.arrived",
            AggregateType = "stay",
            AggregateId = stayId,
            PropertyId = DeskHarness.Property,
            EntityVersion = 1,
            OccurredAt = occurred,
            ActorType = 1,
            Source = "guestops",
            Payload = JsonDocument.Parse("{}"),
        });
        await harness.Db.SaveChangesAsync();

        var entry = Wire(await new ActivityView(harness.Db)
                .AnswerAsync(harness.Scope(), stayId, CancellationToken.None))
            .GetProperty("entries").EnumerateArray().Single();

        Assert.Equal(occurred, Instant(entry.GetProperty("at")));
        Assert.False(entry.TryGetProperty("date", out _), "'date' is a rendering; the screen formats 'at'");
        Assert.False(entry.TryGetProperty("time", out _), "'time' is a rendering; the screen formats 'at'");
    }
}
