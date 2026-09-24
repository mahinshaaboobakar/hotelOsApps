using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Recording that nobody came — the owner's N1 and N2, ruled 2026-09-24.
/// </summary>
/// <remarks>
/// <b>The door was the missing half.</b> <c>RecordNoShowAsync</c> has been
/// built and tested since it was written and was reachable only over the gRPC
/// surface; what these assert is that the module door reaches it and does not
/// soften it on the way.
/// </remarks>
public sealed class NoShowCommandTests
{
    [Fact]
    public async Task A_stay_that_never_arrived_is_recorded_as_a_no_show()
    {
        await using var harness = await Ready();
        var stay = await Booked(harness);

        var answer = await NoShow(harness, stay.Id, stay.Version);

        Assert.Equal(nameof(StayLifecycle.NoShow), Read<string>(answer, "lifecycle"));

        harness.Db.ChangeTracker.Clear();
        var after = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);
        Assert.Equal(StayLifecycle.NoShow, after.Lifecycle);
    }

    /// <summary>
    /// A no-show is not a cancellation, and the record keeps them apart.
    /// </summary>
    /// <remarks>
    /// <b>This is the whole reason it is its own transition.</b> The guest did
    /// not arrive and did not cancel; the two carry different commercial
    /// consequences, and a screen that could only cancel would record the
    /// wrong one of the two every time somebody failed to turn up.
    /// </remarks>
    [Fact]
    public async Task A_no_show_is_not_recorded_as_a_cancellation()
    {
        await using var harness = await Ready();
        var stay = await Booked(harness);

        await NoShow(harness, stay.Id, stay.Version);

        harness.Db.ChangeTracker.Clear();
        var after = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);

        Assert.NotEqual(StayLifecycle.Cancelled, after.Lifecycle);
        Assert.DoesNotContain("stay.cancelled", harness.Events.Types);
    }

    /// <summary>
    /// A stay already in house is refused, by the service rather than the door.
    /// </summary>
    /// <remarks>
    /// <b>The door must not acquire its own opinion.</b> If this file filtered
    /// the state itself, the module surface and the gRPC surface would each
    /// hold a copy of the rule and would drift; the assertion is that the
    /// service's own sentence comes back through the door unchanged.
    /// </remarks>
    [Fact]
    public async Task A_stay_in_house_is_refused_in_the_services_own_words()
    {
        await using var harness = await Ready();
        var stay = await Booked(harness);
        Assert.Equal(StayLifecycle.Booked, stay.Lifecycle);   // the arrange below must MOVE it

        stay.Lifecycle = StayLifecycle.InHouse;
        await harness.Db.SaveChangesAsync();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => NoShow(harness, stay.Id, stay.Version));

        Assert.Contains("never arrived", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A body with no version is refused by name, not as a stale one.</summary>
    [Fact]
    public async Task A_body_with_no_version_is_refused_by_name()
    {
        await using var harness = await Ready();
        var stay = await Booked(harness);

        var body = JsonSerializer.SerializeToElement(new { stayId = stay.Id.ToString() });

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Command(harness).RunAsync(harness.Scope(), body, CancellationToken.None));

        Assert.Contains("version", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A version somebody else has moved past is refused.</summary>
    [Fact]
    public async Task A_stale_version_is_refused()
    {
        await using var harness = await Ready();
        var stay = await Booked(harness);

        await Assert.ThrowsAsync<HotelOS.Platform.ConcurrencyException>(
            () => NoShow(harness, stay.Id, stay.Version - 1));
    }

    private static Task<object?> NoShow(DeskHarness harness, Guid stayId, long version)
    {
        var body = JsonSerializer.SerializeToElement(new
        {
            stayId = stayId.ToString(),
            version,
        });

        return Command(harness).RunAsync(harness.Scope(), body, CancellationToken.None);
    }

    private static NoShowCommand Command(DeskHarness harness)
        => new(new StayLifecycleService(
            harness.Db, harness.Authorizer, harness.Events, harness.Clock));

    /// <summary>A stay still waiting to arrive.</summary>
    /// <remarks>
    /// <b>The state is set, not assumed.</b> <c>SeedStayAsync</c> creates an
    /// <c>InHouse</c> stay, so a helper called <c>Booked</c> that only seeded
    /// would have been a lie — and the refusal test below would have set
    /// <c>InHouse</c> on a stay that was already in house, passing as a no-op
    /// with its own arrange line deleted.
    /// </remarks>
    private static async Task<RoomStay> Booked(DeskHarness harness)
    {
        var stay = await harness.SeedStayAsync(new DateTimeOffset(
            new DateOnly(2026, 8, 19).ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero));

        stay.Lifecycle = StayLifecycle.Booked;
        await harness.Db.SaveChangesAsync();
        return stay;
    }

    private static async Task<DeskHarness> Ready()
    {
        var harness = await DeskHarness.CreateAsync(withEventStore: true);
        await harness.ConfigureAsync();
        return harness;
    }

    private static T? Read<T>(object? answer, string name)
    {
        var json = JsonSerializer.SerializeToElement(answer);
        return json.TryGetProperty(name, out var value) ? value.Deserialize<T>() : default;
    }
}
