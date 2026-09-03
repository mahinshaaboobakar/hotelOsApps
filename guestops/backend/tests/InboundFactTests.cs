using HotelOS.GuestOps.Application.Inbound;
using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// What the Hub's facts do when they arrive — the deferred backlog's landing.
/// </summary>
public class InboundFactTests
{
    /// <summary>A fact for a stay nobody has seen creates it.</summary>
    [Fact]
    public async Task An_unknown_reservation_becomes_a_stay()
    {
        await using var harness = await InboundHarness.CreateAsync();

        var outcome = await harness.Inbound.ApplyAsync(
            harness.Scope(),
            InboundHarness.Fact(StayLifecycle.Booked, room: InboundHarness.Room),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Created, outcome);

        var stay = await harness.Db.Stays.SingleAsync();
        Assert.Equal(StayLifecycle.Booked, stay.Lifecycle);

        // The PMS sent it, so the PMS knows it — the flag says who knows, and
        // never how the guest arrived.
        Assert.False(stay.PmsUnknown);
        Assert.Equal(RecordOrigin.Pms, stay.Origin);
    }

    /// <summary>
    /// The stay and its references are minted together — GUEST-Q8.
    /// </summary>
    /// <remarks>
    /// What makes the second fact for one reservation find its stay rather than
    /// make another. A crash between the two would leave a stay nothing could
    /// ever match again.
    /// </remarks>
    [Fact]
    public async Task Minting_records_the_reference_that_finds_it_again()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();

        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Booked, room: InboundHarness.Room),
            CancellationToken.None);

        var second = await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.InHouse, room: InboundHarness.Room),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Applied, second);
        Assert.Single(await harness.Db.Stays.ToListAsync());
    }

    /// <summary>The same fact twice changes nothing and publishes nothing.</summary>
    /// <remarks>
    /// Replay is idempotent <b>by construction</b>: the Hub's backlog re-runs
    /// through the same rule, and a fact already applied must not announce
    /// itself again.
    /// </remarks>
    [Fact]
    public async Task A_replayed_fact_publishes_nothing()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();
        var fact = InboundHarness.Fact(StayLifecycle.Booked, room: InboundHarness.Room);

        await harness.Inbound.ApplyAsync(scope, fact, CancellationToken.None);
        var published = harness.Events.Types.Count;

        var again = await harness.Inbound.ApplyAsync(scope, fact, CancellationToken.None);

        Assert.Equal(InboundOutcome.Settled, again);
        Assert.Equal(published, harness.Events.Types.Count);
    }

    /// <summary>
    /// A check-out for a stay never seen creates it departed, and invents no
    /// arrival — R7, S12.
    /// </summary>
    [Fact]
    public async Task A_check_out_first_creates_a_departed_stay_with_no_arrival()
    {
        await using var harness = await InboundHarness.CreateAsync();

        var fact = InboundHarness.Fact(StayLifecycle.Departed, room: InboundHarness.Room)
            with
        { Arrival = StayTime.None };

        await harness.Inbound.ApplyAsync(harness.Scope(), fact, CancellationToken.None);

        var stay = await harness.Db.Stays.SingleAsync();
        Assert.Equal(StayLifecycle.Departed, stay.Lifecycle);
        Assert.False(stay.ArrivalAt.IsKnown);

        // The absence is recorded rather than the arrival being fabricated —
        // the intermediate states are never invented.
        var absences = await harness.Db.Absences.Select(a => a.Field).ToListAsync();
        Assert.Contains(AbsentFields.ArrivalTime, absences);

        // And exactly one fact was published: the stay exists. No arrival was
        // announced, because none happened where we could see it.
        Assert.Equal(["stay.created"], harness.Events.Types);
    }

    /// <summary>Cancelling an in-house stay is recorded and not applied — S26.</summary>
    [Fact]
    public async Task A_cancellation_for_an_arrived_guest_is_a_contradiction()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();

        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.InHouse, room: InboundHarness.Room),
            CancellationToken.None);

        var outcome = await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Cancelled, room: InboundHarness.Room),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Contradicted, outcome);

        // The guest stays served and the room stays occupied.
        var stay = await harness.Db.Stays.SingleAsync();
        Assert.Equal(StayLifecycle.InHouse, stay.Lifecycle);

        var recorded = await harness.Db.Disagreements.SingleAsync();
        Assert.Equal(DisagreementState.Standing, recorded.State);
    }

    /// <summary>
    /// A fact that might be a stay this property created is held — GUEST-Q5.
    /// </summary>
    /// <remarks>
    /// <b>Nothing is published and no second stay exists</b> while a candidate
    /// is undecided: announcing a stay we intend to withdraw would tell every
    /// consumer something we cannot honestly take back.
    /// </remarks>
    [Fact]
    public async Task A_possible_duplicate_is_held_and_announced_to_nobody()
    {
        await using var harness = await InboundHarness.CreateAsync();

        await harness.SeedLocalStayAsync(
            InboundHarness.Room, new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1));

        harness.Events.Types.Clear();

        var outcome = await harness.Inbound.ApplyAsync(
            harness.Scope(),
            InboundHarness.Fact(
                StayLifecycle.InHouse,
                room: InboundHarness.Room,
                arrival: new DateOnly(2026, 8, 31),
                departure: new DateOnly(2026, 9, 1),
                guest: "Joseph K Mathew"),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Held, outcome);
        Assert.Empty(harness.Events.Types);
        Assert.Single(await harness.Db.Stays.ToListAsync());

        var candidate = await harness.Db.LinkCandidates.SingleAsync();
        Assert.Equal(CandidateState.Proposed, candidate.State);

        // The names only ordered the list. Two of three words match, and that
        // number decides nothing.
        Assert.True(candidate.RankScore > 0);
    }

    /// <summary>
    /// A different room is not a candidate, however alike the names.
    /// </summary>
    /// <remarks>
    /// The test is same room and overlapping dates. The system this replaces
    /// matched on surname and arrival date, and a wrong match silently merges
    /// two guests' histories — worse than a duplicate.
    /// </remarks>
    [Fact]
    public async Task A_matching_name_in_another_room_is_not_a_candidate()
    {
        await using var harness = await InboundHarness.CreateAsync();

        await harness.SeedLocalStayAsync(
            InboundHarness.Room, new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1));

        var outcome = await harness.Inbound.ApplyAsync(
            harness.Scope(),
            InboundHarness.Fact(
                StayLifecycle.InHouse,
                room: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                arrival: new DateOnly(2026, 8, 31),
                departure: new DateOnly(2026, 9, 1),
                guest: "Joseph Mathew"),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Created, outcome);
        Assert.Empty(await harness.Db.LinkCandidates.ToListAsync());
    }

    /// <summary>
    /// A turnaround is not an overlap: one leaves, one arrives, same day.
    /// </summary>
    [Fact]
    public async Task Same_room_on_consecutive_stays_is_not_a_candidate()
    {
        await using var harness = await InboundHarness.CreateAsync();

        await harness.SeedLocalStayAsync(
            InboundHarness.Room, new DateOnly(2026, 8, 28), new DateOnly(2026, 8, 31));

        var outcome = await harness.Inbound.ApplyAsync(
            harness.Scope(),
            InboundHarness.Fact(
                StayLifecycle.Booked,
                room: InboundHarness.Room,
                arrival: new DateOnly(2026, 8, 31),
                departure: new DateOnly(2026, 9, 2)),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Created, outcome);
        Assert.Empty(await harness.Db.LinkCandidates.ToListAsync());
    }

    /// <summary>
    /// Every arriving fact stamps the feed, whatever becomes of it.
    /// </summary>
    /// <remarks>
    /// The inversion this replaced: `HeldFact.ReceivedAt` is written only when
    /// a fact <b>fails</b>, so a healthy property had no arrival timestamp at
    /// all and a widget reading it reported "never" exactly when the wire was
    /// fine. The mark is taken before the decision, so a fact that settles
    /// silently counts as much as one that is held.
    /// </remarks>
    [Fact]
    public async Task A_settled_fact_still_proves_the_feed_is_alive()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var fact = InboundHarness.Fact(StayLifecycle.Booked);

        await harness.Inbound.ApplyAsync(harness.Scope(), fact, CancellationToken.None);

        var mark = await harness.Db.FeedMarks.SingleAsync();

        Assert.Equal(fact.IntegrationId, mark.IntegrationId);
        Assert.Empty(await harness.Db.HeldFacts.ToListAsync());
    }

    /// <summary>A replayed fact never ages a live feed backwards.</summary>
    /// <remarks>
    /// Section 13's replay re-sends facts that arrived days ago. The mark moves
    /// forward only — letting an old one overwrite it would be the same
    /// inversion arriving by a different road.
    /// </remarks>
    [Fact]
    public async Task A_replayed_fact_does_not_move_the_mark_backwards()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();

        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Booked), CancellationToken.None);

        var first = (await harness.Db.FeedMarks.SingleAsync()).LastFactAt;

        harness.Clock.Advance(TimeSpan.FromMinutes(-30));
        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Booked), CancellationToken.None);

        Assert.Equal(first, (await harness.Db.FeedMarks.SingleAsync()).LastFactAt);
    }
}
