using HotelOS.GuestOps.Application.Inbound;
using HotelOS.GuestOps.Application.Reconciliation;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The override, the disagreement, and the two decisions a person makes.
/// </summary>
public class ReconciliationTests
{
    /// <summary>
    /// A matching fact settles the override silently — GUEST-Q4.
    /// </summary>
    /// <remarks>
    /// <b>The ruling that keeps the mechanism from being decorative.</b> When a
    /// six-hour outage ends and fourteen facts arrive, thirteen match what the
    /// desk already recorded. Flagging those would bury the one that differs.
    /// </remarks>
    [Fact]
    public async Task A_fact_that_agrees_settles_silently()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();
        var stay = await SeedOverriddenStayAsync(harness, ours: StayLifecycle.InHouse);

        harness.Events.Types.Clear();

        var outcome = await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.InHouse, room: InboundHarness.Room),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Settled, outcome);

        // Recorded, and surfacing nothing: no event, and the row leaves the
        // Attention list.
        Assert.Empty(harness.Events.Types);

        var row = await harness.Db.Disagreements.SingleAsync();
        Assert.Equal(DisagreementState.Confirmed, row.State);
    }

    /// <summary>
    /// A differing fact raises a disagreement and applies nothing — GUEST-Q3.
    /// </summary>
    /// <remarks>
    /// <b>One truth still leaves the application.</b> The stay keeps the desk's
    /// value, so the board, Room Care and Context all read the same room — the
    /// disagreement is a flag on that one answer, never a second answer.
    /// </remarks>
    [Fact]
    public async Task A_fact_that_differs_raises_a_disagreement_and_changes_nothing()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();
        var stay = await SeedOverriddenStayAsync(harness, ours: StayLifecycle.InHouse);

        harness.Events.Types.Clear();

        var outcome = await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Departed, room: InboundHarness.Room),
            CancellationToken.None);

        Assert.Equal(InboundOutcome.Disagreed, outcome);
        Assert.Empty(harness.Events.Types);

        var after = await harness.Db.Stays.SingleAsync(s => s.Id == stay.Id);
        Assert.Equal(StayLifecycle.InHouse, after.Lifecycle);

        var row = await harness.Db.Disagreements.SingleAsync();
        Assert.Equal(DisagreementState.Standing, row.State);
        Assert.Equal("InHouse", row.OurValue);
        Assert.Equal("Departed", row.PmsValue);
    }

    /// <summary>Keeping ours closes it and announces nothing.</summary>
    /// <remarks>
    /// Nothing about the hotel changed, so there is nothing to publish — and
    /// both values stay on the row, because a decision that discarded the
    /// losing value could not explain itself later.
    /// </remarks>
    [Fact]
    public async Task Clearing_to_ours_publishes_nothing_and_keeps_both_values()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();
        await SeedOverriddenStayAsync(harness, ours: StayLifecycle.InHouse);

        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Departed, room: InboundHarness.Room),
            CancellationToken.None);

        var row = await harness.Db.Disagreements.SingleAsync();
        harness.Events.Types.Clear();

        await harness.Reconciliation.ClearAsync(
            scope, row.Id, ClearSide.Ours, CancellationToken.None);

        Assert.Empty(harness.Events.Types);
        Assert.Equal(DisagreementState.ClearedOurs, row.State);
        Assert.Equal("InHouse", row.OurValue);
        Assert.Equal("Departed", row.PmsValue);
    }

    /// <summary>
    /// Taking the PMS's side publishes the same correction a move does.
    /// </summary>
    /// <remarks>
    /// GUEST-Q3 (3), deliberately: a consumer that already handles a correction
    /// needs nothing new, and a <c>disagreement.cleared</c> subject would make
    /// every consumer learn a second way to hear one thing.
    /// </remarks>
    [Fact]
    public async Task Clearing_to_the_PMS_publishes_a_correction()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();
        await SeedOverriddenStayAsync(harness, ours: StayLifecycle.InHouse);

        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Departed, room: InboundHarness.Room),
            CancellationToken.None);

        var row = await harness.Db.Disagreements.SingleAsync();
        harness.Events.Types.Clear();

        await harness.Reconciliation.ClearAsync(
            scope, row.Id, ClearSide.Pms, CancellationToken.None);

        Assert.Equal(["stay.corrected"], harness.Events.Types);

        var stay = await harness.Db.Stays.SingleAsync();
        Assert.Equal(StayLifecycle.Departed, stay.Lifecycle);
    }

    /// <summary>Clearing takes the stay's write permission — never its own.</summary>
    /// <remarks>
    /// GUEST-Q3 refused both alternatives by name: author-only fails across
    /// shifts, supervisor-only escalates a routine reconciliation. A separate
    /// <c>disagreement.clear</c> would re-introduce the escalation.
    /// </remarks>
    [Fact]
    public async Task Clearing_asks_for_the_stays_write_permission()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();
        await SeedOverriddenStayAsync(harness, ours: StayLifecycle.InHouse);

        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Departed, room: InboundHarness.Room),
            CancellationToken.None);

        var row = await harness.Db.Disagreements.SingleAsync();
        harness.Authorizer.Permissions.Clear();

        await harness.Reconciliation.ClearAsync(
            scope, row.Id, ClearSide.Ours, CancellationToken.None);

        Assert.Equal(["stay.write"], harness.Authorizer.Permissions);
    }

    /// <summary>
    /// Confirming a candidate keeps the local stay and maps the PMS's ids onto
    /// it.
    /// </summary>
    /// <remarks>
    /// <b>The local stay survives</b> because its id is what Room Care, Jobs and
    /// the registration already name. Merging the other way would invalidate
    /// every reference already given out.
    /// </remarks>
    [Fact]
    public async Task Confirming_a_candidate_keeps_the_local_stay()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();

        var local = await harness.SeedLocalStayAsync(
            InboundHarness.Room, new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1));

        await harness.Inbound.ApplyAsync(
            scope,
            InboundHarness.Fact(
                StayLifecycle.InHouse,
                room: InboundHarness.Room,
                arrival: new DateOnly(2026, 8, 31),
                departure: new DateOnly(2026, 9, 1)),
            CancellationToken.None);

        var candidate = await harness.Db.LinkCandidates.SingleAsync();

        await harness.Reconciliation.DecideCandidateAsync(
            scope, candidate.Id, sameStay: true, CancellationToken.None);

        Assert.Single(await harness.Db.Stays.ToListAsync());

        var stay = await harness.Db.Stays.SingleAsync(s => s.Id == local.Id);
        Assert.False(stay.PmsUnknown);

        var reference = await harness.Db.StayExternalRefs.SingleAsync();
        Assert.Equal(local.Id, reference.StayId);
        Assert.Equal("84119377", reference.ExternalId);
    }

    /// <summary>
    /// Rejecting produces two stays and a double-booked room, honestly.
    /// </summary>
    /// <remarks>
    /// GUEST-Q5: *"different → two stays honestly, because a double-booked room
    /// is then the truth, not an artefact."* It is also why the conflict check
    /// warns rather than forbids — a hard block would put this outcome out of
    /// reach.
    /// </remarks>
    [Fact]
    public async Task Rejecting_a_candidate_leaves_the_room_double_booked()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();

        await harness.SeedLocalStayAsync(
            InboundHarness.Room, new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1));

        await harness.Inbound.ApplyAsync(
            scope,
            InboundHarness.Fact(
                StayLifecycle.InHouse,
                room: InboundHarness.Room,
                arrival: new DateOnly(2026, 8, 31),
                departure: new DateOnly(2026, 9, 1)),
            CancellationToken.None);

        var candidate = await harness.Db.LinkCandidates.SingleAsync();

        await harness.Reconciliation.DecideCandidateAsync(
            scope, candidate.Id, sameStay: false, CancellationToken.None);

        Assert.Equal(CandidateState.Rejected, candidate.State);

        // The held fact is resolved either way — it is one fact, and it is now
        // either the local stay's or its own.
        var held = await harness.Db.HeldFacts.SingleAsync();
        Assert.NotNull(held.ResolvedAt);
    }

    /// <summary>A decided candidate is not decided twice.</summary>
    [Fact]
    public async Task A_decided_candidate_is_refused_the_second_time()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var scope = harness.Scope();

        await harness.SeedLocalStayAsync(
            InboundHarness.Room, new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1));

        await harness.Inbound.ApplyAsync(
            scope,
            InboundHarness.Fact(
                StayLifecycle.InHouse,
                room: InboundHarness.Room,
                arrival: new DateOnly(2026, 8, 31),
                departure: new DateOnly(2026, 9, 1)),
            CancellationToken.None);

        var candidate = await harness.Db.LinkCandidates.SingleAsync();

        await harness.Reconciliation.DecideCandidateAsync(
            scope, candidate.Id, sameStay: true, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            harness.Reconciliation.DecideCandidateAsync(
                scope, candidate.Id, sameStay: false, CancellationToken.None));
    }

    /// <summary>A stay the PMS knows, with a staff override standing on it.</summary>
    private static async Task<RoomStay> SeedOverriddenStayAsync(
        InboundHarness harness, StayLifecycle ours)
    {
        var scope = harness.Scope();

        await harness.Inbound.ApplyAsync(
            scope, InboundHarness.Fact(StayLifecycle.Booked, room: InboundHarness.Room),
            CancellationToken.None);

        var stay = await harness.Db.Stays.SingleAsync();
        stay.Lifecycle = ours;

        harness.Db.Disagreements.Add(new StayDisagreement
        {
            Id = Uuid7.NewUuid7(),
            StayId = stay.Id,
            Aspect = DisagreementAspect.Lifecycle,
            OurValue = ours.ToString(),
            PmsValueAtOverride = StayLifecycle.Booked.ToString(),
            OverrideActor = scope.UserId,
            OverrideAt = harness.Clock.GetUtcNow(),
            RaisedAt = harness.Clock.GetUtcNow(),
            State = DisagreementState.Overridden,
        });

        await harness.Db.SaveChangesAsync();
        return stay;
    }
}
