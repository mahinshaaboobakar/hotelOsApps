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
/// <b>All three views are asserted here, and two of them could not be until
/// the scratch database grew a table.</b> `BookingView` and `CancelPlanView`
/// read room-type names and <c>RoomStay.RoomTypeId</c> is non-nullable, so
/// there is no arrangement in which they skip <c>masterdata.room_types</c> —
/// which this database did not have. They were withdrawn from the commit of
/// 2026-09-20 with that gap stated rather than hidden, and
/// <c>GuestOpsScratch.MasterDataRoomTypesAsync</c> closed it the same day,
/// following Room Care's <c>MasterDataStaffSource</c> rather than inventing a
/// second shape.
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

        // The room type the views read by name. Seeded through the harness so
        // the table is Master Data's shape and the application role can read
        // it — the split an install produces.
        await harness.MasterDataRoomTypesAsync([
            new MasterDataRoomTypeSource.Row
            {
                Id = DeskHarness.RoomType,
                Name = "Executive Suite",
                BaseOccupancy = 2,
                MaxOccupancy = 3,
                MaxAdults = 2,
                MaxChildren = 1,
                ExtraBedAllowed = true,
                MaxExtraBeds = 1,
            },
        ]);

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

    [Fact]
    public async Task A_bookings_summary_sends_the_count_and_the_span()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var id = await BookingAsync(harness, Arrive, Arrive);

        var answer = Wire(await new BookingView(harness.Db, Reads(harness))
            .AnswerAsync(harness.Scope(), id, new Paging.Window(0, 20), CancellationToken.None));

        var summary = answer.GetProperty("summary");

        // The owner's ruling on frame 9, 2026-09-20 (A2): the heading carries
        // the confirmation number. Null here because this booking has no
        // `confirmation` external reference — the screen then starts the line
        // with the count, rather than drawing a gap where a number would be.
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("confirmation").ValueKind);
        Assert.Equal(2, summary.GetProperty("stays").GetInt32());
        Assert.Equal("2026-09-03", summary.GetProperty("arrive").GetString());
        Assert.Equal("2026-09-07", summary.GetProperty("depart").GetString());

        var stay = answer.GetProperty("stays").EnumerateArray().First();
        Assert.Equal("2026-09-03", stay.GetProperty("arrive").GetString());
        Assert.Equal("2026-09-07", stay.GetProperty("depart").GetString());

        // The name, not the id: the room type is the one value here that comes
        // from Master Data, and reading it is why this test needed a table.
        Assert.Equal("Executive Suite", stay.GetProperty("roomType").GetString());
    }

    [Fact]
    public async Task A_cancel_plan_sends_the_subject_in_parts_and_no_sentence()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var id = await BookingAsync(harness, Arrive, Arrive);

        var answer = Wire(await new CancelPlanView(harness.Db, Reads(harness))
            .AnswerAsync(harness.Scope(), id, CancellationToken.None));

        var subject = answer.GetProperty("subject");

        // Null, and that is the fact: a booking's reference lives in
        // `BookingExternalRef` and this one has none. The screen drops the part
        // rather than drawing a placeholder where a reference would be.
        Assert.Equal(JsonValueKind.Null, subject.GetProperty("reference").ValueKind);
        Assert.Equal("2026-09-03", subject.GetProperty("arrive").GetString());
        Assert.Equal("2026-09-07", subject.GetProperty("depart").GetString());
        Assert.Equal(2, answer.GetProperty("stays").GetInt32());

        // `consequence` was "This cancels two stays, one at a time." — a
        // sentence the screen says now. Asserted ABSENT rather than left
        // untested: a view that kept sending it would leave two wordings of one
        // sentence, and the screen's would be the one nobody maintained.
        Assert.False(answer.TryGetProperty("consequence", out _));

        var rows = answer.GetProperty("rows").EnumerateArray().ToList();

        // Every penalty row is about a stay's span; the why is not, and carries
        // no day at all rather than a null that reads as a missing date.
        Assert.Equal("2026-09-03", rows[0].GetProperty("arrive").GetString());

        var afterwards = rows.Single(row => row.GetProperty("label").GetString() == "Afterwards");
        Assert.Equal(string.Empty, afterwards.GetProperty("value").GetString());
        Assert.Equal("2026-09-03", afterwards.GetProperty("arrive").GetString());
    }
}
