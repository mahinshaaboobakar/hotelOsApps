using System.Text.Json;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// What Booking, Bookings and the cancel plan put on the wire — values, and no
/// renderings (ADR 0175).
/// </summary>
/// <remarks>
/// <para>
/// <b>`RenderedDateGuardTests` and this test answer different questions.</b>
/// The guard reads the source and can say that no view holds a date format; it
/// cannot say that the screen's fields are there, because an absence has
/// nothing to read. These three views were converted on 2026-09-20 and the
/// screens now compose `Two stays · 3 Sep → 7 Sep` themselves — so a field that
/// silently stopped being sent would draw `undefined` on a cancellation dialog,
/// and no source-reading check would notice.
/// </para>
/// <para>
/// <b>Asserted as JSON, because JSON is the contract</b> — the same reason
/// <c>StayTabViewTests</c> gives: the views return <c>object?</c>, and what
/// crosses the module envelope is the serialised form.
/// </para>
/// <para>
/// <b>Only the list's row is asserted here, and the other two views are a
/// stated gap rather than a silent one.</b> `BookingView` and `CancelPlanView`
/// both read room-type names, and <c>RoomStay.RoomTypeId</c> is non-nullable —
/// so every stay sends them to <c>masterdata.room_types</c>, which this scratch
/// database does not have (it provisions <c>guestops</c> only). They throw
/// 42P01 before reaching anything this file is about, which is the same gap
/// <c>LocaleWireTests</c> states for Watchlist.
/// </para>
/// <para>
/// <b>The fix exists next door and is the next piece of work</b>: Room Care's
/// <c>MasterDataStaffSource</c> creates Master Data's real table in its scratch
/// database from an EF mapping of Master Data's own shape — no hand-written
/// DDL — and grants the application role SELECT on it. The same for
/// <c>room_types</c> lets those two views be asserted here. Until then their
/// wire shape is covered by two weaker instruments, named so nobody reads this
/// file as full coverage: <c>RenderedDateGuardTests</c> refuses any rendering
/// in <c>Module/</c>, and the screens' own tests compose from a fixture whose
/// shape is what these views send.
/// </para>
/// <para>
/// <b>The arrangement failed first, twice, wearing the failure of the thing
/// under test</b> (2026-09-20): a `Paging.Window(1, 20)` is the SECOND page —
/// <c>Skip => Page * PageSize</c> — so the one row was skipped and the list
/// read as empty; and a seeded room type sent the view to
/// <c>masterdata.room_types</c>, which this scratch database does not have. The
/// views were right both times.
/// </para>
/// <para>
/// The day is asserted as <c>2026-09-03</c> rather than <i>any string</i>: ISO
/// is what `formatDay` parses, and a view sending <c>03/09/2026</c> would pass
/// a "the field is present" test and render `Invalid Date` on the screen.
/// </para>
/// </remarks>
public sealed class BookingWireTests
{
    private static readonly DateOnly Arrive = new(2026, 9, 3);
    private static readonly DateOnly Depart = new(2026, 9, 7);

    private static JsonElement Wire(object? answer) => JsonSerializer.SerializeToElement(answer);

    private static async Task<Guid> BookingAsync(DeskHarness harness, params DateOnly[] arrivals)
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = DeskHarness.Property,
            Origin = RecordOrigin.Pms,
            CreatedAt = harness.Clock.GetUtcNow(),
        };

        harness.Db.Bookings.Add(booking);

        foreach (var arrival in arrivals)
        {
            harness.Db.Stays.Add(new RoomStay
            {
                Id = Guid.CreateVersion7(),
                BookingId = booking.Id,
                PropertyId = DeskHarness.Property,
                RoomTypeId = DeskHarness.RoomType,
                Lifecycle = StayLifecycle.Booked,
                BusinessDate = arrival,
                ArrivalAt = StayTime.Observed(
                    new DateTimeOffset(arrival.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero)),
                DepartureAt = StayTime.Observed(
                    new DateTimeOffset(Depart.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero)),
            });
        }

        await harness.Db.SaveChangesAsync();
        return booking.Id;
    }

    private static BookingReadService Reads(DeskHarness harness)
        => new(harness.Db, harness.Authorizer, new StubBusinessDay(Arrive));

    [Fact]
    public async Task A_bookings_row_sends_counts_and_two_iso_days()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await BookingAsync(harness, Arrive, Arrive);

        var answer = Wire(await new BookingsView(Reads(harness))
            .AnswerAsync(harness.Scope(), new Paging.Window(0, 20), CancellationToken.None));

        var row = answer.GetProperty("rows").EnumerateArray().Single();

        Assert.Equal(2, row.GetProperty("rooms").GetInt32());
        Assert.Equal("2026-09-03", row.GetProperty("arrive").GetString());
        Assert.Equal("2026-09-07", row.GetProperty("depart").GetString());

        // Null rather than the count repeated: `1 of 1 known` would attribute a
        // claim to a source that never made one.
        Assert.Equal(JsonValueKind.Null, row.GetProperty("claimed").ValueKind);
    }

}
