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
/// Cancelling a booking is one act: a refusal on any of its stays cancels none.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why it exists — the screen's sentence depends on it.</b> When a cancel is
/// refused, GuestOps tells the desk <i>"nothing was changed"</i>. That is true
/// only if every write waits on every check. <c>CancelCommand</c> cancelled
/// stay by stay, each committing on its own, so a refusal on the second stay of
/// a group arrived after the first was cancelled — and the desk was told nothing
/// had changed. Found auditing the "nothing was changed" wording, 2026-09-19.
/// </para>
/// <para>
/// <b>The positive control</b> is the second case: a cancel allowed on every
/// stay commits them all, so a rollback that swallowed every cancel cannot pass
/// as atomicity.
/// </para>
/// </remarks>
public sealed class CancelAtomicityTests
{
    /// <summary>
    /// Refuses the N-th <c>stay.override</c> asked (0 = none), as the real
    /// <c>KernelAuthorizer</c> refuses — with <c>PermissionDeniedException</c>.
    /// </summary>
    /// <remarks>
    /// Counted by ASKING ORDER, not by stay id: the partial cancel appears only
    /// when a stay is written before the refused one is asked, and which stay
    /// the loop reaches second is the loop's business, not the test's guess.
    /// </remarks>
    private sealed class Kernel(int refuseOverride) : IKernelAuthorizer
    {
        private int _asked;

        public Task RequireAsync(
            RequestScope scope, string permission, string objectType, Guid objectId,
            CancellationToken cancellationToken)
            => permission == "stay.override" && ++_asked == refuseOverride
                ? throw new PermissionDeniedException(permission, $"{objectType}:{objectId}")
                : Task.CompletedTask;

        public Task<IReadOnlyList<bool>> AllowedAsync(
            RequestScope scope, string permission, string objectType,
            IReadOnlyList<Guid> objectIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<bool>>([.. objectIds.Select(_ => true)]);
    }

    private static async Task<(DeskHarness Harness, Guid Booking)> TwoStayBooking()
    {
        var harness = await DeskHarness.CreateAsync(withEventStore: true);
        var kernel = new Kernel(0);
        var events = new EventAppender(harness.Db, harness.Clock, new ServiceIdentity("guestops"));
        var bookings = new BookingService(
            harness.Db, kernel, events, new StubBusinessDay(new DateOnly(2026, 9, 1)),
            new ContactProtector(new byte[32], new byte[32]), harness.Clock);

        NewStay Stay(string guest) => new(
            RoomTypeId: DeskHarness.RoomType, ArrivalDate: new DateOnly(2026, 9, 3),
            DepartureDate: new DateOnly(2026, 9, 5), Adults: 1, Children: 0,
            Guests: [new NewGuest(guest, null, null, null, null, IsPrimary: true)],
            WalkIn: false, Terms: null);

        var booking = await bookings.CreateAsync(
            harness.Scope(),
            new NewBooking([Stay("First Guest"), Stay("Second Guest")],
                Channel: "direct", TravelAgent: null, MarketCode: null, MealPlan: null,
                ExpectedStayCount: 2),
            CancellationToken.None);

        harness.Db.ChangeTracker.Clear();
        return (harness, booking.Id);
    }

    private static CancelCommand Command(DeskHarness harness, IKernelAuthorizer kernel)
    {
        var events = new EventAppender(harness.Db, harness.Clock, new ServiceIdentity("guestops"));
        return new CancelCommand(
            harness.Db,
            new BookingReadService(harness.Db, kernel),
            new StayLifecycleService(harness.Db, kernel, events, harness.Clock));
    }

    private static JsonElement Body(Guid booking) => JsonDocument.Parse(
        $$"""{"bookingId":"{{booking}}","reason":"Guest request"}""").RootElement;

    /// <summary>Each stay's lifecycle, and the cancellation events — from the DATABASE.</summary>
    private static async Task<(string Stays, int Cancelled)> Committed(DeskHarness harness)
    {
        harness.Db.ChangeTracker.Clear();
        var stays = await harness.Db.Stays.AsNoTracking()
            .OrderBy(s => s.Id).Select(s => s.Lifecycle.ToString()).ToListAsync();
        var cancelled = await harness.Db.Set<StoredEvent>().AsNoTracking()
            .CountAsync(e => e.EventType == "stay.cancelled");
        return (string.Join(",", stays), cancelled);
    }

    [Fact]
    public async Task A_cancel_refused_on_the_second_stay_cancels_neither()
    {
        var (harness, booking) = await TwoStayBooking();
        await using var _ = harness;

        // The second stay asked is refused, so the first is written before the
        // refusal arrives — the order that exposes a partial cancel.
        await Assert.ThrowsAsync<PermissionDeniedException>(() => Command(harness, new Kernel(2))
            .RunAsync(harness.Scope(), Body(booking), CancellationToken.None));

        Assert.Equal(("Booked,Booked", 0), await Committed(harness));
    }

    [Fact]
    public async Task A_cancel_allowed_on_every_stay_cancels_them_all()
    {
        var (harness, booking) = await TwoStayBooking();
        await using var __ = harness;

        await Command(harness, new Kernel(0))
            .RunAsync(harness.Scope(), Body(booking), CancellationToken.None);

        Assert.Equal(("Cancelled,Cancelled", 2), await Committed(harness));
    }
}
