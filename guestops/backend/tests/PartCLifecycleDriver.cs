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
    /// <summary>⚠ <c>CheckOut</c> accepts every source state.</summary>
    private static readonly (StayLifecycle From, bool Accepted)[] CheckOutMatrix =
    [
        (StayLifecycle.Waitlisted, true),
        (StayLifecycle.Pending, true),
        (StayLifecycle.Booked, true),
        (StayLifecycle.InHouse, true),
        (StayLifecycle.Departed, true),
        (StayLifecycle.Cancelled, true),
        (StayLifecycle.NoShow, true),
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

    /// <summary>
    /// ⚠ A departure can be recorded from any state — recorded, endorsed by nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CheckOutAsync</c> validates no lifecycle, and neither does
    /// <c>CheckOutCommand</c>, which checks only that a stay id and a version are
    /// present. So a <c>Waitlisted</c>, <c>Cancelled</c> or <c>NoShow</c> stay can be
    /// marked departed, and a <c>Departed</c> one can be departed again with its
    /// <c>DepartureAt</c> overwritten — each announcing <c>stay.departed</c> to every
    /// consumer.
    /// </para>
    /// <para>
    /// <b>Its two siblings guard their source and say why.</b> <c>CancelAsync</c>
    /// refuses an arrival with <i>"this guest has already arrived; correct the stay
    /// rather than cancelling it"</i>, and <c>RecordNoShowAsync</c> with <i>"only a
    /// stay that never arrived can be a no-show"</i> — both directing the caller to
    /// the correction. <b>Nothing in either file explains why check-out is the
    /// exception</b>, which is why this is a measured ambiguity reported upward and not
    /// a repair a stream makes.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Check_out_accepts_every_source_state()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var lifecycle = Lifecycle(harness);

        foreach (var (from, _) in CheckOutMatrix)
        {
            var stay = await At(harness, lifecycle, from);

            var departed = await lifecycle.CheckOutAsync(
                harness.Scope(), stay.Id, stay.Version, default);

            Assert.True(
                departed.Lifecycle == StayLifecycle.Departed,
                $"from {from}: expected Departed, got {departed.Lifecycle}");
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
        => new(harness.Db, harness.Authorizer, harness.Events, harness.Clock);

    private static BookingService Bookings(DeskHarness harness)
        => new(
            harness.Db, harness.Authorizer, harness.Events,
            new StubBusinessDay(new DateOnly(2026, 9, 1)),
            new ContactProtector(new byte[32], new byte[32]), harness.Clock);
}
