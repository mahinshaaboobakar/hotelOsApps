using System.Text.Json;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure.Platform;
using HotelOS.GuestOps.Module;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// A walk-in is one act: a refusal anywhere in it leaves nothing behind.
/// </summary>
/// <remarks>
/// <para>
/// <b>Measured non-atomic on 2026-09-19</b>, before its handler was ever wired: a
/// walk-in refused at <c>stay.assign</c> left <b>1 booking · 1 stay · 3
/// events</b> (<c>guest.created</c>, <c>reservation.created</c>,
/// <c>stay.created</c>) — a guest the desk turned away, announced to every
/// consumer. <c>BookingService.CreateAsync</c> commits on its own and nothing
/// wrapped the three calls.
/// </para>
/// <para>
/// <b>Why the checks cannot simply be asked first.</b> <c>stay.assign</c> and the
/// check-in are asked against the <i>stay</i>, and the stay's id is minted by the
/// create — there is nothing to ask about until it exists, and asking at property
/// scope instead would widen the grant. So the refusal still arrives after the
/// create, and what makes it leave nothing is the transaction: the change and its
/// events commit together or not at all.
/// </para>
/// <para>
/// <b>The third case is the positive control.</b> A walk-in that rolled back
/// every time would pass the first two; only a walk-in that is allowed and
/// commits proves the transaction is closed on the way out as well as on the
/// way down.
/// </para>
/// </remarks>
public sealed class WalkInAtomicityTests
{
    /// <summary>
    /// Answers as the Kernel does: property scope is answered; a permission named
    /// in <c>refused</c> is refused on the stay with the Kernel's own sentence
    /// (<c>authz/registry.rs:282</c>).
    /// </summary>
    private sealed class Kernel(params string[] refused) : IKernelAuthorizer
    {
        public Task RequireAsync(
            RequestScope scope, string permission, string objectType, Guid objectId,
            CancellationToken cancellationToken)
            => refused.Contains(permission)
                ? throw new InvalidRequestException(
                    $"permission \"{permission}\" may be checked against property, but the object is a {objectType}")
                : Task.CompletedTask;

        public Task<IReadOnlyList<bool>> AllowedAsync(
            RequestScope scope, string permission, string objectType,
            IReadOnlyList<Guid> objectIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<bool>>([.. objectIds.Select(_ => !refused.Contains(permission))]);
    }

    private static WalkInCommand Command(DeskHarness harness, IKernelAuthorizer kernel)
    {
        // The real appender into the real event store, so "no events" is a
        // statement about rows that were or were not committed.
        var events = new EventAppender(harness.Db, harness.Clock, new ServiceIdentity("guestops"));

        return new WalkInCommand(
            harness.Db,
            new BookingService(
                harness.Db, kernel, events,
                new StubBusinessDay(new DateOnly(2026, 9, 1)),
                new ContactProtector(new byte[32], new byte[32]),
                harness.Clock),
            new StayAssignmentService(harness.Db, kernel, events, harness.Clock),
            new StayLifecycleService(harness.Db, kernel, events, harness.Clock));
    }

    private static readonly JsonElement Sheet = JsonDocument.Parse($$"""
        {"guest":"Walk-in Atomicity","roomTypeId":"{{DeskHarness.RoomType}}",
         "roomId":"{{DeskHarness.Room}}","arrives":"2026-09-01","departs":"2026-09-02",
         "adults":1}
        """).RootElement;

    /// <summary>What the DATABASE holds — not what this context is tracking.</summary>
    private static async Task<(int Bookings, int Stays, int Guests, string[] Events)> Committed(
        DeskHarness harness)
    {
        harness.Db.ChangeTracker.Clear();

        return (
            await harness.Db.Bookings.AsNoTracking().CountAsync(),
            await harness.Db.Stays.AsNoTracking().CountAsync(),
            await harness.Db.Guests.AsNoTracking().CountAsync(),
            await harness.Db.Set<StoredEvent>().AsNoTracking()
                .OrderBy(e => e.EventType).Select(e => e.EventType).ToArrayAsync());
    }

    [Theory]
    [InlineData("stay.assign")]
    [InlineData("stay.override")]
    public async Task A_walk_in_refused_after_the_create_leaves_nothing(string refusedAt)
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Command(harness, new Kernel(refusedAt))
                .RunAsync(harness.Scope(), Sheet, CancellationToken.None));

        var (bookings, stays, guests, events) = await Committed(harness);

        // Stated as one tuple so a failure prints every figure at once — the
        // measurement this replaces was 1 · 1 · 3 events, and that is what a
        // regression would print.
        Assert.Equal(
            (0, 0, 0, string.Empty),
            (bookings, stays, guests, string.Join(", ", events)));
    }

    [Fact]
    public async Task A_walk_in_that_is_allowed_commits_whole_and_in_house()
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);

        await Command(harness, new Kernel())
            .RunAsync(harness.Scope(), Sheet, CancellationToken.None);

        var (bookings, stays, guests, events) = await Committed(harness);

        Assert.Equal((1, 1, 1), (bookings, stays, guests));
        Assert.Equal(
            StayLifecycle.InHouse,
            await harness.Db.Stays.AsNoTracking().Select(s => s.Lifecycle).SingleAsync());

        // Every step's event, committed with its change.
        Assert.Contains("reservation.created", events);
        Assert.Contains("stay.assigned", events);
        Assert.Contains("stay.arrived", events);
    }
}
