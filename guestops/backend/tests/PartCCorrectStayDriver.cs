using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Part C's driver for <c>CorrectStay</c> — the only write that carries an enum.
/// </summary>
/// <remarks>
/// <para>
/// <b>Part C creates data; it does not press controls</b> — ADR 0358 as the owner
/// ruled it. Every row is written through
/// <see cref="StayLifecycleService.CorrectAsync"/> (ADR 0228).
/// </para>
/// <para>
/// <b>This is the enum axis, and it was the last one at zero.</b> GuestOps declares
/// five enums with 24 members between them and <b>one appears on a write
/// request</b>: <c>CorrectStayRequest.to</c>, a <c>StayLifecycle</c>. So the 11
/// member cases <c>part-c-coverage.md</c> counts all meet the wire here.
/// </para>
/// <para>
/// <b>Why the zero value is driven at the SERVICE and that is faithful to the
/// wire.</b> <c>GuestOpsGrpcService.Stays.cs:134</c> converts with a bare cast —
/// <c>(Domain.StayLifecycle)(int)request.To</c> — so a caller sending
/// <c>STAY_LIFECYCLE_UNSPECIFIED = 0</c> hands the service exactly
/// <c>(StayLifecycle)0</c>, which is what the last theory below passes. <i>Driving
/// it here is not a shortcut around the door; it is the value the door
/// produces.</i>
/// </para>
/// <para>
/// <b>The correction validates no target, and that is right for seven of the eight
/// values.</b> A correction exists to undo a wrong transition, so it must be able
/// to reach any real state — which is why <c>CorrectAsync</c> checks only the
/// reason, where <c>RecordNoShowAsync</c> refuses an illegal source (ADR 0310's
/// measurement). <b>The eighth value is not a state.</b>
/// </para>
/// </remarks>
public sealed class PartCCorrectStayDriver
{
    /// <summary>All seven NAMED members are reachable, and each is stored.</summary>
    /// <remarks>
    /// <para>
    /// <b>What this adds over <c>CorrectCommandTests</c>, which already covers this
    /// RPC.</b> That suite drives three transitions by name — a check-out put back
    /// in house, a no-show reinstated, the mistake and the correction both kept.
    /// <i>This covers every member the enum declares</i>, which is Part C's axis
    /// rather than a second characterisation suite (ADR 0054).
    /// </para>
    /// <para>
    /// Enumerated from the enum rather than listed, so a member added to
    /// <see cref="StayLifecycle"/> is driven the day it is declared and not the day
    /// somebody remembers this file.
    /// </para>
    /// <para>
    /// <b>The announcement is asserted by TYPE here, because that is what this
    /// double records</b> — <c>RecordingAppender</c> keeps event types on purpose,
    /// and its own remarks say why: <i>"what they can assert exactly is which facts
    /// were announced"</i>. The <c>from</c>/<c>to</c> payload needs the real
    /// appender, and has its own test below rather than a widened double.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(NamedMembers))]
    public async Task Every_named_lifecycle_is_a_reachable_correction(StayLifecycle to)
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var corrected = await Lifecycle(harness).CorrectAsync(
            harness.Scope(), stay.Id, to, "the desk recorded the wrong state",
            stay.Version, default);

        Assert.Equal(to, corrected.Lifecycle);
        Assert.Contains("stay.corrected", harness.Events.Types);
    }

    /// <summary>
    /// And the announcement carries BOTH states — read from the event store, not a
    /// double.
    /// </summary>
    /// <remarks>
    /// <b>A correction that moved the row and announced something else would leave
    /// every consumer disagreeing with the property</b>, and the <c>from</c> is the
    /// fact an activity timeline reads to say what was put right. Driven once with
    /// the real <c>EventAppender</c> and a provisioned <c>StoredEvent</c> table —
    /// <c>withEventStore: true</c> is off by default because provisioning it on
    /// every harness exhausted the migrator's connection limit, so one test earns it
    /// rather than the whole theory.
    /// </remarks>
    [Fact]
    public async Task The_correction_announces_both_the_state_it_left_and_the_one_it_reached()
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var stay = await harness.SeedStayAsync(Arrival);
        var from = stay.Lifecycle;

        var appender = new EventAppender(harness.Db, harness.Clock, new ServiceIdentity("guestops"));
        var lifecycle = harness.Lifecycle(events: appender);

        await lifecycle.CorrectAsync(
            harness.Scope(), stay.Id, StayLifecycle.InHouse, "recorded in error",
            stay.Version, default);

        harness.Db.ChangeTracker.Clear();
        var announced = await harness.Db.Set<StoredEvent>().AsNoTracking()
            .SingleAsync(e => e.EventType == "stay.corrected");

        var payload = announced.Payload?.RootElement.ToString() ?? string.Empty;

        Assert.Contains(from.ToString(), payload, StringComparison.Ordinal);
        Assert.Contains(nameof(StayLifecycle.InHouse), payload, StringComparison.Ordinal);
        Assert.Contains("recorded in error", payload, StringComparison.Ordinal);
    }

    /// <summary>The seven the domain declares — derived, never written out.</summary>
    public static TheoryData<StayLifecycle> NamedMembers()
    {
        var data = new TheoryData<StayLifecycle>();
        foreach (var member in Enum.GetValues<StayLifecycle>()) data.Add(member);
        return data;
    }

    /// <summary>
    /// ⚠ THE EIGHTH VALUE — <c>STAY_LIFECYCLE_UNSPECIFIED</c> is ACCEPTED and
    /// STORED, and this test records that rather than asserting it is right.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is a Part C finding, not a passing contract.</b> The driver's job is
    /// to create data in every shape the wire admits and report what the API did —
    /// and what it does here is write a lifecycle that <b>no name in the domain
    /// describes</b>:
    /// </para>
    /// <code>
    /// proto      STAY_LIFECYCLE_UNSPECIFIED = 0        protobuf requires a zero
    /// C# domain  StayLifecycle starts at Waitlisted=1  NO member 0, deliberately
    /// the door   (Domain.StayLifecycle)(int)request.To bare cast, Stays.cs:134
    /// the service CorrectAsync validates the REASON    and not the target
    /// the column HasIndex only                         no conversion, no constraint
    /// </code>
    /// <para>
    /// <b>⚠ IT IS ONE DOOR OF TWO, AND NAMING THE DOOR IS THE WHOLE FINDING.</b>
    /// The first draft of this remark said <i>"the write path casts its own zero
    /// straight through"</i> — a broad conclusion from a narrow check, which is the
    /// class this repository has already paid for.
    /// </para>
    /// <code>
    /// the MODULE door   takes the lifecycle as a STRING and refuses anything
    ///                   that is not an exact member name. Already covered three
    ///                   ways by CorrectCommandTests:116 "Levitating" and :133
    ///                   "inhouse". A desk CANNOT express zero.
    /// the gRPC door     (Domain.StayLifecycle)(int)request.To — Stays.cs:134.
    ///                   A connector sending STAY_LIFECYCLE_UNSPECIFIED, or any
    ///                   number, reaches the service unchecked.
    /// the service       validates the REASON and not the target.
    /// </code>
    /// <para>
    /// <b>So the exposure is a gRPC caller, not the desk</b>, and the
    /// <c>stay.corrected</c> event then announces a <c>to</c> with no name while the
    /// stay's index carries a value no screen can label. That is the gap rule — <i>a
    /// default is a third claim nobody made</i> — and the application already holds
    /// the right pattern on its own read path:
    /// <c>GuestOpsGrpcService.Stays.cs:29</c> records that
    /// <c>STAY_VIEW_UNSPECIFIED</c> <i>"is refused by name rather than defaulted to
    /// arrivals: a caller that forgot the field would otherwise get a plausible list
    /// for a question it never asked."</i> <b>The same file refuses one zero by name
    /// and casts the other.</b>
    /// </para>
    /// <para>
    /// <b>The reference has no unnamed status at all</b> —
    /// <c>pms-integrations/src/main/java/co/instio/integrations/common/misc/ReservationStatus.java:3-4</c>
    /// declares <c>BOOKING, CHECKIN, CHECKOUT, CANCELLED, WAITING, NO_SHOW</c> and a
    /// Java enum has no zero sentinel, so <i>the concept carries six named states
    /// and no "did not say"</i>. Our own domain enum agrees by starting at 1. The
    /// zero exists only because proto3 demands one, which makes it a wire artefact
    /// rather than a state. <i>Cited as the connectors' reference, which is where
    /// that file lives — <c>guest-management-server</c> mentions
    /// <c>ReservationStatus</c> in nine files and declares it in none.</i>
    /// </para>
    /// <para>
    /// <b>⚠ THE DOOR IS NOW CLOSED, AND THIS TEST IS KEPT AS THE RECORD OF WHAT IT
    /// USED TO ADMIT.</b> <c>GuestOpsGrpcService.FromProto(StayLifecycle)</c> refuses
    /// an unspecified or undeclared value by name —
    /// <see cref="LifecycleWireTests"/> — so <b>nothing can send the service a zero
    /// any more</b>. The architect assigned that as a repair on this measurement;
    /// CLAUDE.md's gap rule and the same file's own <c>StayView</c> refusal ruled it,
    /// so no ADR was owed.
    /// </para>
    /// <para>
    /// <b>The service's own behaviour is unchanged, and the test name says so.</b>
    /// <c>CorrectAsync</c> still accepts a <c>(StayLifecycle)0</c> handed to it
    /// directly, because the guard went where the untrusted value enters rather than
    /// being duplicated inland. <i>That is a decision, not an oversight</i>: a second
    /// check in the service would be a second place for the rule to drift, and the
    /// correction's whole contract is that it may reach any real state.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task The_service_still_accepts_a_zero_the_door_no_longer_lets_through()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        // Exactly what the gRPC door hands the service for a request that set no
        // lifecycle at all.
        var unspecified = (StayLifecycle)0;
        Assert.False(Enum.IsDefined(unspecified));

        var corrected = await Lifecycle(harness).CorrectAsync(
            harness.Scope(), stay.Id, unspecified, "a caller that set no state",
            stay.Version, default);

        // CURRENT BEHAVIOUR, recorded. Not a contract this driver endorses.
        Assert.Equal(unspecified, corrected.Lifecycle);

        // And it reaches the database, so it is not merely an in-memory artefact.
        harness.Db.ChangeTracker.Clear();
        var stored = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);
        Assert.False(Enum.IsDefined(stored.Lifecycle));
    }

    /// <summary>A correction with no reason is refused — the one check it makes.</summary>
    /// <remarks>
    /// <para>
    /// <b>Three blank forms, because "empty" is not one value.</b> A validator
    /// written <c>== ""</c> would admit a single space, and a reason of whitespace
    /// is the same defect as none: <i>"without one it is indistinguishable from a
    /// mistake."</i> The accepting counterpart is every row of the theory above, so
    /// a validator refusing everything cannot hide here.
    /// </para>
    /// <para>
    /// <b>Shown able to fail, at <c>a375699a</c>:</b> removing the service's reason
    /// guard gives <b>4 failed, 11 passed</b> — all three blank forms <i>and</i>
    /// <c>And_a_refused_correction_writes_nothing</c>. <b>That all three fail is what
    /// proves the guard is <c>IsNullOrWhiteSpace</c> rather than <c>== ""</c></b>; a
    /// theory with only the empty string would pass against the weaker check.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task A_correction_with_no_reason_is_refused(string reason)
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var refused = await Assert.ThrowsAsync<InvalidRequestException>(
            () => Lifecycle(harness).CorrectAsync(
                harness.Scope(), stay.Id, StayLifecycle.Booked, reason, stay.Version, default));

        Assert.Contains("needs a reason", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>And nothing is written when the reason is refused.</summary>
    /// <remarks>
    /// <b>The refusal is first, and this is what says so.</b> A guard placed after
    /// the write it protects is not a guard — so the assertion is that the stay's
    /// version and lifecycle are untouched and no event was appended, not merely
    /// that an exception arrived.
    /// </remarks>
    [Fact]
    public async Task And_a_refused_correction_writes_nothing()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        var was = stay.Lifecycle;
        var version = stay.Version;

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Lifecycle(harness).CorrectAsync(
                harness.Scope(), stay.Id, StayLifecycle.Departed, "", version, default));

        harness.Db.ChangeTracker.Clear();
        var stored = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);

        Assert.Equal(was, stored.Lifecycle);
        Assert.Equal(version, stored.Version);

        // Silence is the assertion here, which is what RecordingAppender's own
        // remarks say it exists to make possible.
        Assert.DoesNotContain("stay.corrected", harness.Events.Types);
    }

    /// <summary>A stale version is refused rather than silently winning.</summary>
    /// <remarks>
    /// Two desks correcting one stay is the case this exists for. The second
    /// correction is driven at the version the FIRST one returned, so the pair also
    /// shows that a correct sequence succeeds — <c>version + 1</c> would be a guess
    /// about the increment rather than a read of it.
    /// </remarks>
    [Fact]
    public async Task A_stale_version_is_refused_and_the_current_one_is_accepted()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        var lifecycle = Lifecycle(harness);
        var stale = stay.Version;

        var first = await lifecycle.CorrectAsync(
            harness.Scope(), stay.Id, StayLifecycle.Pending, "first", stale, default);

        var current = first.Version;
        Assert.True(current > stale);

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => lifecycle.CorrectAsync(
                harness.Scope(), stay.Id, StayLifecycle.Booked, "stale", stale, default));

        var second = await lifecycle.CorrectAsync(
            harness.Scope(), stay.Id, StayLifecycle.Booked, "current", current, default);

        Assert.Equal(StayLifecycle.Booked, second.Lifecycle);
    }

    /// <summary>A correction of a stay at another property is NOT FOUND, never forbidden.</summary>
    /// <remarks>
    /// <para>
    /// ADR 0054's boundary: the query is scoped by property before id, so another
    /// property's stay does not exist as far as this caller is concerned. A
    /// <c>PermissionDenied</c> would confirm the stay is real, which is the
    /// cross-property leak the scoping exists to prevent.
    /// </para>
    /// <para>
    /// <b>Shown able to fail, at <c>a375699a</c>:</b> dropping
    /// <c>s.PropertyId == scope.PropertyId</c> from <c>RequireWritableAsync</c>'s
    /// lookup gives <b>1 failed, 14 passed</b>, and the failure is this test alone.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_stay_at_another_property_is_not_found()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var elsewhere = new RequestScope { PropertyId = Guid.CreateVersion7(), UserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(
            () => Lifecycle(harness).CorrectAsync(
                elsewhere, stay.Id, StayLifecycle.Departed, "wrong property", stay.Version, default));
    }

    private static readonly DateTimeOffset Arrival =
        new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The service, built as its host composes it.</summary>
    private static StayLifecycleService Lifecycle(DeskHarness harness)
        => harness.Lifecycle();
}
