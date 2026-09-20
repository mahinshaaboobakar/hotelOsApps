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
/// A walk-in is two authorized operations: phase 1 stands, phase 2 is all or nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>This file asserted the opposite until 2026-09-19</b> — <i>"a refusal
/// anywhere in it leaves nothing behind"</i>, from one transaction around all
/// three operations. RC-Q8a ruled that promise was never architectural and that a
/// refused second step leaves a <b>booked, room-less stay</b>, telling the desk
/// so; RC-Q8b-2 made phase 2 a separate action, authorized when it starts. The
/// test is rewritten to the new contract under ADR 0034, not deleted: the
/// measurements that made the old one (a refusal leaving 1 · 1 · 3 events with no
/// word to the desk) are why the new answer says what was left.
/// </para>
/// <para>
/// <b>The refusal the stand-in gives is the Kernel's real answer today</b>: a
/// permission asked on a <c>stay</c>, a type the Kernel does not cover, is an
/// invalid request (<c>authz/registry.rs:282</c>). A true denial is its own case.
/// </para>
/// <para>
/// <b>The allowed case proves phase 2 commits, not that a walk-in can be
/// allowed</b> on the platform — the stand-in answers yes where the real Kernel
/// cannot until installable object types are registered (ADR 0193).
/// </para>
/// </remarks>
public sealed class WalkInPhasesTests
{
    /// <summary>
    /// Answers as the Kernel does: property scope is answered; a permission named
    /// in <c>refused</c> is refused on the stay with the Kernel's own sentence
    /// (<c>authz/registry.rs:282</c>).
    /// </summary>
    private sealed class Kernel(string[] refused, bool deny = false) : IKernelAuthorizer
    {
        public Kernel(params string[] refused) : this(refused, false) { }

        public Task RequireAsync(
            RequestScope scope, string permission, string objectType, Guid objectId,
            CancellationToken cancellationToken)
            => !refused.Contains(permission) ? Task.CompletedTask
                : deny ? throw new PermissionDeniedException(permission, objectType)
                : throw new InvalidRequestException(
                    $"permission \"{permission}\" may be checked against property, but the object is a {objectType}");

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
            new StayAssignmentService(harness.Db, kernel, events, new StubBusinessDay(new DateOnly(2026, 9, 1)), harness.Clock),
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

    /// <summary>The answer, as JSON crosses the envelope.</summary>
    private static JsonElement Wire(object? answer) => JsonSerializer.SerializeToElement(answer);

    [Theory]
    [InlineData("stay.assign")]
    [InlineData("stay.override")]
    public async Task A_refused_second_step_leaves_the_booked_room_less_stay_and_says_so(string refusedAt)
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);

        var answer = Wire(await Command(harness, new Kernel(refusedAt))
            .RunAsync(harness.Scope(), Sheet, CancellationToken.None));

        // The desk is told: the stay exists, and the second step did not happen.
        Assert.False(answer.GetProperty("checkedIn").GetBoolean());
        Assert.Equal("refused", answer.GetProperty("secondStep").GetString());

        var (bookings, stays, guests, events) = await Committed(harness);
        Assert.Equal((1, 1, 1), (bookings, stays, guests));

        var stay = await harness.Db.Stays.AsNoTracking().SingleAsync();
        Assert.Equal(StayLifecycle.Booked, stay.Lifecycle);

        // Refused at check-in, the assign before it is rolled back with it:
        // room-less, as RC-Q8a says — never a room held for a guest not in it.
        Assert.Null(stay.CurrentRoomId);
        Assert.DoesNotContain("stay.assigned", events);
        Assert.DoesNotContain("stay.arrived", events);
        Assert.Contains("reservation.created", events);
    }

    [Fact]
    public async Task A_denied_second_step_says_not_authorized_not_refused()
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);

        var answer = Wire(await Command(harness, new Kernel(["stay.assign"], deny: true))
            .RunAsync(harness.Scope(), Sheet, CancellationToken.None));

        Assert.False(answer.GetProperty("checkedIn").GetBoolean());
        Assert.Equal("not-authorized", answer.GetProperty("secondStep").GetString());
    }

    [Fact]
    public async Task A_walk_in_that_is_allowed_is_in_house_with_every_steps_event()
    {
        await using var harness = await DeskHarness.CreateAsync(withEventStore: true);

        var answer = Wire(await Command(harness, new Kernel())
            .RunAsync(harness.Scope(), Sheet, CancellationToken.None));

        Assert.True(answer.GetProperty("checkedIn").GetBoolean());
        Assert.Equal(JsonValueKind.Null, answer.GetProperty("secondStep").ValueKind);

        var (bookings, stays, guests, events) = await Committed(harness);

        Assert.Equal((1, 1, 1), (bookings, stays, guests));
        Assert.Equal(
            StayLifecycle.InHouse,
            await harness.Db.Stays.AsNoTracking().Select(s => s.Lifecycle).SingleAsync());

        Assert.Contains("reservation.created", events);
        Assert.Contains("stay.assigned", events);
        Assert.Contains("stay.arrived", events);
    }
}
