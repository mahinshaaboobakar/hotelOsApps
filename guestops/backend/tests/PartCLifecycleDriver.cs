using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure.Platform;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Part C's driver for the four lifecycle writes — <c>CheckIn</c>, <c>CheckOut</c>,
/// <c>CancelStay</c> and <c>RecordNoShow</c> — as a matrix of which SOURCE state
/// each one accepts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Part C creates data; it does not press controls</b> — ADR 0358. Every row is
/// written through <see cref="StayLifecycleService"/> (ADR 0228), and each source
/// state is reached by <c>CorrectAsync</c> rather than by a hand-made row.
/// </para>
/// <para>
/// <b>The matrix is the axis the existing suites do not drive.</b>
/// <c>CheckOutCommandTests</c> drives <i>a guest in house is recorded as
/// departed</i>; <c>NoShowCommandTests</c> drives <i>never arrived</i> and <i>in
/// house is refused</i>; <c>CancelAtomicityTests</c> drives multi-stay atomicity.
/// Each is the operation's own happy path and one refusal. <b>None asks which of the
/// seven states a stay can be in each operation accepts.</b>
/// </para>
/// <para>
/// <b>⚠ ONE HARNESS PER MATRIX, AND THE REASON IS A CEILING I CROSSED.</b> The first
/// version of this file was four xUnit theories — 25 <c>InlineData</c> rows, each a
/// separate invocation creating its own scratch database. <b>The suite went from ~167
/// scratch databases to ~232, and 18 tests failed</b>: fifteen with
/// <c>Npgsql … Exception while reading from stream</c> and three with EF's transient
/// wrapper, scattered across <c>StayTabViewTests</c>, <c>ReconciliationTests</c>,
/// <c>AvailabilityWireTests</c> and <c>DeskTests</c> — <i>suites this block never
/// touched</i> — and <b>zero logic failures</b>. <c>DeskHarness</c> documents the
/// mechanism: provisioning on every harness <i>"slowed setup enough that the suite
/// exhausted <c>hotelos_migrator</c>'s connection limit."</i>
/// </para>
/// <para>
/// So each matrix is ONE test over one database, seeding <b>a fresh stay per row</b>
/// so no row can observe another's. <i>The per-row test NAME is what that costs</i>,
/// and it is bought back in the assertion: each one names the state it was driving,
/// because the failure message is the deliverable.
/// </para>
/// <para>
/// <b>The verdicts are SPELLED OUT, never derived from the rule under test</b> — a
/// matrix computed from <c>stay.Lifecycle is not (Booked or Pending or Waitlisted)</c>
/// would assert the code against itself. The <i>completeness</i> of each matrix is
/// derived instead, by <see cref="Every_lifecycle_has_a_verdict_in_every_matrix"/>, so
/// a member added to <see cref="StayLifecycle"/> fails a test that asks its author to
/// decide the verdicts rather than being silently skipped.
/// </para>
/// </remarks>
public sealed class PartCLifecycleDriver
{
    /// <summary><c>CheckOut</c> accepts ONLY an in-house stay — ADR 0365 §1.</summary>
    /// <remarks>
    /// <b>Every row but one was <c>true</c> until 2026-10-02</b>, and that matrix is
    /// what raised <c>GUEST-Q16</c>. The owner ruled <i>"check-out only in house"</i>;
    /// the six refusals are the ruling.
    /// </remarks>
    private static readonly (StayLifecycle From, bool Accepted)[] CheckOutMatrix =
    [
        (StayLifecycle.Waitlisted, false),
        (StayLifecycle.Pending, false),
        (StayLifecycle.Booked, false),
        (StayLifecycle.InHouse, true),
        (StayLifecycle.Departed, false),
        (StayLifecycle.Cancelled, false),
        (StayLifecycle.NoShow, false),
    ];

    /// <summary><c>CancelStay</c> refuses the two states that record an arrival.</summary>
    private static readonly (StayLifecycle From, bool Accepted)[] CancelMatrix =
    [
        (StayLifecycle.Waitlisted, true),
        (StayLifecycle.Pending, true),
        (StayLifecycle.Booked, true),
        (StayLifecycle.InHouse, false),
        (StayLifecycle.Departed, false),
        (StayLifecycle.Cancelled, true),
        (StayLifecycle.NoShow, true),
    ];

    /// <summary><c>RecordNoShow</c> accepts only a stay that never arrived.</summary>
    private static readonly (StayLifecycle From, bool Accepted)[] NoShowMatrix =
    [
        (StayLifecycle.Waitlisted, true),
        (StayLifecycle.Pending, true),
        (StayLifecycle.Booked, true),
        (StayLifecycle.InHouse, false),
        (StayLifecycle.Departed, false),
        (StayLifecycle.Cancelled, false),
        (StayLifecycle.NoShow, false),
    ];

    /// <summary>Check-out acts only on an in-house stay — ADR 0365 §1.</summary>
    /// <remarks>
    /// <para>
    /// <b>This REPLACES <c>Check_out_accepts_every_source_state</c></b> — ADR 0034, a
    /// test encoding a superseded contract. Its name asserted the behaviour the owner
    /// withdrew on 2026-10-02, so a pass under it would have been fresh evidence,
    /// every run, for a rule nobody holds. <i>It failed rather than passing quietly,
    /// with the new guard's own sentence, which is the better of the two ways a stale
    /// name is found.</i>
    /// </para>
    /// <para>
    /// <b>Shown able to fail, at <c>1444673d</c>:</b> removing the InHouse guard
    /// gives <b>1 failed, 19 passed</b>, and the failure is this test alone.
    /// </para>
    /// <para>
    /// <b>What it recorded was real and is why the ruling exists:</b> check-out
    /// validated nothing, so a <c>Waitlisted</c>, <c>Cancelled</c> or <c>NoShow</c>
    /// stay could be marked departed and a <c>Departed</c> one again with its real
    /// departure time overwritten — each announcing <c>stay.departed</c>. Its three
    /// siblings all guarded their source; this was the anomaly.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Check_out_acts_only_on_an_in_house_stay()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var lifecycle = harness.Lifecycle();

        foreach (var (from, accepted) in CheckOutMatrix)
        {
            var stay = await At(harness, lifecycle, from);

            var checkOut = () => lifecycle.CheckOutAsync(
                harness.Scope(), stay.Id, stay.Version, default);

            if (accepted)
            {
                var departed = await checkOut();
                Assert.True(
                    departed.Lifecycle == StayLifecycle.Departed,
                    $"from {from}: expected Departed, got {departed.Lifecycle}");
                continue;
            }

            var refused = await Record.ExceptionAsync(checkOut);

            Assert.True(
                refused is InvalidRequestException,
                $"from {from}: expected a refusal, got "
                + (refused?.GetType().Name ?? "no exception at all"));

            Assert.True(
                refused!.Message.Contains("in house", StringComparison.Ordinal),
                $"from {from}: refused for the wrong reason — {refused.Message}");
        }

        Assert.Contains("stay.departed", harness.Events.Types);
    }

    /// <summary><c>CancelStay</c> refuses a stay that has already arrived.</summary>
    [Fact]
    public async Task Cancel_refuses_a_stay_that_has_already_arrived()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var lifecycle = Lifecycle(harness);

        foreach (var (from, accepted) in CancelMatrix)
        {
            var stay = await At(harness, lifecycle, from);

            var cancel = () => lifecycle.CancelAsync(
                harness.Scope(), stay.Id, "the guest rang off", stay.Version, default);

            if (accepted)
            {
                var cancelled = await cancel();
                Assert.True(
                    cancelled.Lifecycle == StayLifecycle.Cancelled,
                    $"from {from}: expected Cancelled, got {cancelled.Lifecycle}");
                continue;
            }

            // Record.ExceptionAsync rather than Assert.ThrowsAsync, because the
            // latter takes no message: its failure reads "No exception was thrown"
            // and names no state, which is the row a reader most needs identified.
            var refused = await Record.ExceptionAsync(cancel);

            Assert.True(
                refused is InvalidRequestException,
                $"from {from}: expected a refusal, got "
                + (refused?.GetType().Name ?? "no exception at all"));

            Assert.True(
                refused!.Message.Contains("already arrived", StringComparison.Ordinal),
                $"from {from}: refused for the wrong reason — {refused.Message}");
        }
    }

    /// <summary><c>RecordNoShow</c> accepts only a stay that never arrived.</summary>
    /// <remarks>
    /// <c>NoShow</c> is itself among the refusals, so a no-show cannot be recorded
    /// twice. <c>NoShowCommandTests</c> already drives the in-house refusal in the
    /// service's own words; what this adds is the other three refusals and all three
    /// acceptances.
    /// </remarks>
    [Fact]
    public async Task A_no_show_is_only_a_stay_that_never_arrived()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var lifecycle = Lifecycle(harness);

        foreach (var (from, accepted) in NoShowMatrix)
        {
            var stay = await At(harness, lifecycle, from);

            var noShow = () => lifecycle.RecordNoShowAsync(
                harness.Scope(), stay.Id, stay.Version, default);

            if (accepted)
            {
                var recorded = await noShow();
                Assert.True(
                    recorded.Lifecycle == StayLifecycle.NoShow,
                    $"from {from}: expected NoShow, got {recorded.Lifecycle}");
                continue;
            }

            var refused = await Record.ExceptionAsync(noShow);

            Assert.True(
                refused is InvalidRequestException,
                $"from {from}: expected a refusal, got "
                + (refused?.GetType().Name ?? "no exception at all"));

            Assert.True(
                refused!.Message.Contains("never arrived", StringComparison.Ordinal),
                $"from {from}: refused for the wrong reason — {refused.Message}");
        }
    }

    /// <summary>
    /// ⚠ <c>CheckIn</c> guards the ROOM and not the lifecycle — so a cancelled stay
    /// that kept its room can be checked in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The sharper half of the check-out finding, with a consequence a reader can
    /// act on.</b> <c>CheckInAsync</c> refuses only <c>CurrentRoomId is null</c>, and
    /// nothing releases that column on cancel, no-show or departure:
    /// <c>CurrentRoomId = null</c> appears once in the whole application, at
    /// <c>BookingService.cs:162</c>, where a stay is created.
    /// </para>
    /// <para>
    /// <b>And the room is free in the meantime, which is what makes it reachable.</b>
    /// <c>ConflictingStayAsync</c> treats only <c>Pending · Booked · InHouse</c> as
    /// holding a room, so a cancelled stay does not block its own room — correctly.
    /// But checking that stay in makes it <c>InHouse</c>, which does hold: <i>if the
    /// room was reassigned after the cancellation, two stays are now in house on one
    /// room, with no <c>accept_conflict</c> anywhere in the path.</i>
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Check_in_accepts_any_state_once_a_room_is_held()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var lifecycle = Lifecycle(harness);

        foreach (var from in new[]
                 {
                     StayLifecycle.Booked, StayLifecycle.Cancelled,
                     StayLifecycle.NoShow, StayLifecycle.Departed,
                 })
        {
            var stay = await At(harness, lifecycle, from);

            // The fixture's stay holds a room, and nothing above released it.
            Assert.NotNull(stay.CurrentRoomId);

            var arrived = await lifecycle.CheckInAsync(
                harness.Scope(), stay.Id, stay.Version, default);

            Assert.True(
                arrived.Lifecycle == StayLifecycle.InHouse,
                $"from {from}: expected InHouse, got {arrived.Lifecycle}");
        }
    }

    /// <summary>And a stay with NO room is refused — the one guard check-in has.</summary>
    /// <remarks>
    /// <b>The counterpart, and it needs an API-created stay.</b>
    /// <c>BookingService.CreateAsync</c> is the only thing that produces a stay with
    /// no room, so the booking path supplies the precondition — ADR 0228's own point:
    /// the fixture enters the model the way production does.
    /// </remarks>
    [Fact]
    public async Task A_stay_with_no_room_cannot_be_checked_in()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(),
            new NewBooking(
                Stays:
                [
                    new NewStay(
                        RoomTypeId: DeskHarness.RoomType,
                        ArrivalDate: new DateOnly(2026, 9, 3),
                        DepartureDate: new DateOnly(2026, 9, 5),
                        Adults: 1, Children: 0, Guests: [], WalkIn: false, Terms: null),
                ],
                Channel: null, TravelAgent: null, MarketCode: null, MealPlan: null,
                ExpectedStayCount: 0),
            default);

        var stay = await harness.Db.Stays.FirstAsync(s => s.BookingId == booking.Id);
        Assert.Null(stay.CurrentRoomId);

        var refused = await Assert.ThrowsAsync<InvalidRequestException>(
            () => Lifecycle(harness).CheckInAsync(
                harness.Scope(), stay.Id, stay.Version, default));

        Assert.Contains("assign one before checking the guest in", refused.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// All three leaving paths release the room, and the ASSIGNMENT RECORD is what
    /// says so — ADR 0365 §2 and §3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Driven on the record, not on the projection.</b> A room is held by an
    /// <c>Assignment</c> whose <c>ReleasedAt</c> is null; the field is a projection
    /// of that row. So the assertion that matters is <c>ReleasedAt</c> being
    /// stamped — a test reading only <c>CurrentRoomId</c> would pass against an
    /// implementation that nulled the field and left the row open, which is the
    /// second-writer defect ADR 0365 §4 forbids.
    /// </para>
    /// <para>
    /// <b>Both are asserted, because they are two facts.</b> The row is the truth and
    /// the projection is what four readers consult — among them
    /// <c>WatchlistView.cs:48</c>, which takes <c>== null</c> as <i>no room</i>. An
    /// implementation that closed the row and left the field set would leave the
    /// watchlist naming a room the stay no longer holds.
    /// </para>
    /// <para>
    /// <b>Shown able to fail twice, at <c>1444673d</c>, and the pair is the point.</b>
    /// Removing <c>ReleasedAt = clock.GetUtcNow()</c> gives <b>1 failed, 19
    /// passed</b>; removing <c>CurrentRoomId = null</c> gives the same, and both
    /// failures are this test. <i>So each of its two assertions is load-bearing on
    /// its own</i> — an implementation that did one and not the other is caught
    /// either way round, which is what ADR 0365 §4's hazard required.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Leaving_releases_the_room_on_all_three_paths()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var lifecycle = harness.Lifecycle();

        var paths = new (string Name, StayLifecycle From, Func<RoomStay, Task> Act)[]
        {
            ("check-out", StayLifecycle.InHouse,
                s => lifecycle.CheckOutAsync(harness.Scope(), s.Id, s.Version, default)),
            ("cancel", StayLifecycle.Booked,
                s => lifecycle.CancelAsync(harness.Scope(), s.Id, "rang off", s.Version, default)),
            ("no-show", StayLifecycle.Booked,
                s => lifecycle.RecordNoShowAsync(harness.Scope(), s.Id, s.Version, default)),
        };

        foreach (var (name, from, act) in paths)
        {
            var stay = await At(harness, lifecycle, from);
            await Assigned(harness, stay);

            await act(stay);

            harness.Db.ChangeTracker.Clear();

            var open = await harness.Db.Assignments
                .CountAsync(a => a.StayId == stay.Id && a.ReleasedAt == null);
            var stored = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);

            Assert.True(open == 0, $"{name}: {open} assignment(s) left open");
            Assert.True(
                stored.CurrentRoomId is null,
                $"{name}: the projection still names {stored.CurrentRoomId}");
        }
    }

    /// <summary>
    /// The room's freedom is its OWN announcement, carrying the room — ADR 0365 §5.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The owner ruled two events.</b> Leaving is a fact about a guest; a free
    /// room is a fact about a room, and Room Care — the consumer that needs the
    /// second — mostly does not care about the first. Told only
    /// <c>stay.departed</c>, it would have to reason about GuestOps' model to
    /// conclude the room is free.
    /// </para>
    /// <para>
    /// <b>The departure is asserted to still carry its room</b>, which is the half an
    /// ordering mistake would break: the release nulls the projection, so a release
    /// appended <i>before</i> the departure event would announce a departure from
    /// nowhere. Read from the real event store, because the payload is the claim.
    /// </para>
    /// <para>
    /// <i>The subject's spelling is the architect's proposal recorded in ADR 0365 §5,
    /// not the owner's words — they ruled "two events" and named neither. If it is
    /// corrected, this is where.</i>
    /// </para>
    /// <para>
    /// <b>Shown able to fail, at <c>1444673d</c>:</b> not appending
    /// <c>stay.room_released</c> gives <b>1 failed, 19 passed</b>, and the failure is
    /// this test alone — so the two-event ruling is asserted by something that can
    /// detect one event.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task The_room_being_free_is_announced_separately_and_names_the_room()
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var appender = new EventAppender(harness.Db, harness.Clock, new ServiceIdentity("guestops"));
        var lifecycle = harness.Lifecycle(events: appender);

        var stay = await At(harness, lifecycle, StayLifecycle.InHouse);
        var room = await Assigned(harness, stay);

        await lifecycle.CheckOutAsync(harness.Scope(), stay.Id, stay.Version, default);

        harness.Db.ChangeTracker.Clear();
        var announced = await harness.Db.Set<StoredEvent>().AsNoTracking()
            .Where(e => e.EventType == "stay.departed" || e.EventType == "stay.room_released")
            .ToListAsync();

        // **TWO events, which is the ruling.** Asserted as a set, not a sequence:
        // `StoredEvent` carries no ordering column, and both appends share one
        // ManualClock instant, so the order is NOT OBSERVABLE here. A test that
        // sorted and asserted a sequence would be asserting whatever the row order
        // happened to be.
        Assert.Equal(
            ["stay.departed", "stay.room_released"],
            announced.Select(e => e.EventType).OrderBy(t => t, StringComparer.Ordinal));

        // **And this is how the ORDER is tested without observing it.** The release
        // nulls the projection, so a release appended BEFORE the departure would
        // leave `stay.departed` carrying a null room. Both naming the room is the
        // consequence of the ordering, and the consequence is what a consumer gets.
        foreach (var e in announced)
        {
            var payload = e.Payload?.RootElement.ToString() ?? string.Empty;
            Assert.True(
                payload.Contains(room.ToString(), StringComparison.Ordinal),
                $"{e.EventType} does not name the room: {payload}");
        }
    }

    /// <summary>A stay that holds no room releases nothing and announces nothing.</summary>
    /// <remarks>
    /// <b>The negative case, and it is not decoration.</b> A release that announced
    /// <c>stay.room_released</c> for a stay with no room would tell Room Care a room
    /// is free without naming one — the gap rule, in an event payload. Cancel is the
    /// path that reaches it, because a booked stay need never have been assigned.
    /// </remarks>
    [Fact]
    public async Task A_stay_with_no_room_releases_nothing_and_announces_nothing()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var lifecycle = harness.Lifecycle();

        var booking = await Bookings(harness).CreateAsync(
            harness.Scope(),
            new NewBooking(
                Stays:
                [
                    new NewStay(
                        RoomTypeId: DeskHarness.RoomType,
                        ArrivalDate: new DateOnly(2026, 9, 3),
                        DepartureDate: new DateOnly(2026, 9, 5),
                        Adults: 1, Children: 0, Guests: [], WalkIn: false, Terms: null),
                ],
                Channel: null, TravelAgent: null, MarketCode: null, MealPlan: null,
                ExpectedStayCount: 0),
            default);

        var stay = await harness.Db.Stays.FirstAsync(s => s.BookingId == booking.Id);
        Assert.Null(stay.CurrentRoomId);
        harness.Events.Types.Clear();

        await lifecycle.CancelAsync(harness.Scope(), stay.Id, "rang off", stay.Version, default);

        Assert.Contains("stay.cancelled", harness.Events.Types);
        Assert.DoesNotContain("stay.room_released", harness.Events.Types);
    }

    /// <summary>
    /// Every member of <see cref="StayLifecycle"/> has a verdict in all three
    /// matrices — derived, so a new member cannot be skipped.
    /// </summary>
    /// <remarks>
    /// <b>This is what stops the spelled-out verdicts going stale.</b> They are typed
    /// out on purpose — deriving them from the rule under test would assert the code
    /// against itself — and a hand-typed list is a hand-kept list. <i>So the LIST is
    /// hand-written and its COMPLETENESS is derived</i>, which is the only arrangement
    /// where neither half checks itself.
    /// <para>
    /// <c>Check_in_accepts_any_state_once_a_room_is_held</c> is deliberately absent:
    /// check-in guards the room rather than the lifecycle, so four states show that,
    /// and a seventh row would assert a rule that does not exist.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_lifecycle_has_a_verdict_in_every_matrix()
    {
        var declared = Enum.GetValues<StayLifecycle>().ToHashSet();

        foreach (var (name, matrix) in new (string, (StayLifecycle From, bool Accepted)[])[]
                 {
                     (nameof(CheckOutMatrix), CheckOutMatrix),
                     (nameof(CancelMatrix), CancelMatrix),
                     (nameof(NoShowMatrix), NoShowMatrix),
                 })
        {
            var covered = matrix.Select(row => row.From).ToHashSet();

            Assert.True(
                declared.SetEquals(covered),
                $"{name} is missing a verdict for "
                + string.Join(", ", declared.Except(covered)));
        }
    }

    /// <summary>Give the stay a room through the assignment API, and say which.</summary>
    /// <remarks>
    /// Through <c>AssignAsync</c> rather than by setting the field: the release is
    /// tested against a room taken the way a room is really taken, so the open
    /// <c>Assignment</c> row exists because the application made it.
    /// <para>
    /// <c>acceptConflict</c> is true because the conflict check is not what is under
    /// test here and a scratch property has no availability to speak of — stated
    /// rather than left as a bare literal.
    /// </para>
    /// </remarks>
    private static async Task<Guid> Assigned(DeskHarness harness, RoomStay stay)
    {
        var room = Guid.CreateVersion7();

        await harness.Assignments().AssignAsync(
            harness.Scope(), stay.Id, room, AssignmentReason.Initial,
            acceptConflict: true, stay.Version, default);

        return room;
    }

    /// <summary>A fresh stay at <paramref name="from"/>, reached through the API.</summary>
    /// <remarks>
    /// <para>
    /// <b>The source state is established by <c>CorrectAsync</c>, not by writing a
    /// row.</b> The correction is the application's own way to put a lifecycle right,
    /// it is driven and probed by <c>PartCCorrectStayDriver</c>, and using it here
    /// means the matrix's precondition is reachable by a supported path (ADR 0166).
    /// The fixture's stay starts <c>InHouse</c>, so <c>InHouse</c> needs no correction.
    /// </para>
    /// <para>
    /// <b>A NEW stay per row, in one database.</b> The rows share a harness to stay
    /// under the connection ceiling, so they must not share a stay: each gets its own,
    /// and no row can observe another's lifecycle.
    /// </para>
    /// </remarks>
    private static async Task<RoomStay> At(
        DeskHarness harness, StayLifecycleService lifecycle, StayLifecycle from)
    {
        var stay = await harness.SeedStayAsync(Arrival);

        if (stay.Lifecycle == from) return stay;

        return await lifecycle.CorrectAsync(
            harness.Scope(), stay.Id, from, "establishing the source state",
            stay.Version, default);
    }

    private static readonly DateTimeOffset Arrival =
        new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    private static StayLifecycleService Lifecycle(DeskHarness harness)
        => harness.Lifecycle();

    private static BookingService Bookings(DeskHarness harness)
        => new(
            harness.Db, harness.Authorizer, harness.Events,
            new StubBusinessDay(new DateOnly(2026, 9, 1)),
            new ContactProtector(new byte[32], new byte[32]), harness.Clock);
}
