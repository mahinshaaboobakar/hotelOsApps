using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Recording that the guest has left — gold frame 3's <i>Check out</i>.
/// </summary>
/// <remarks>
/// <b>It announces a departure and asserts nothing about cleaning.</b> Room
/// Care decides for itself whether a vacated room becomes work (APPS-Q1), so
/// what is checked here is that the fact leaves and that nothing else is
/// claimed with it.
/// </remarks>
public sealed class CheckOutCommandTests
{
    [Fact]
    public async Task A_guest_in_house_is_recorded_as_departed()
    {
        await using var harness = await Ready();
        var stay = await InHouse(harness);

        var answer = await CheckOut(harness, stay.Id, stay.Version);

        Assert.Equal(nameof(StayLifecycle.Departed), Read<string>(answer, "lifecycle"));

        harness.Db.ChangeTracker.Clear();
        var after = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);

        Assert.Equal(StayLifecycle.Departed, after.Lifecycle);
        Assert.True(after.DepartureAt.IsKnown);
    }

    /// <summary>
    /// The departure is announced, and nothing is said about the room's state.
    /// </summary>
    /// <remarks>
    /// <b>APPS-Q1.</b> Cleaning is policy-driven: a checked-out room becoming a
    /// task is a hotel's decision, not an automatic consequence. A command that
    /// published a cleaning fact here would be this application deciding Room
    /// Care's policy for every property.
    /// </remarks>
    [Fact]
    public async Task The_departure_is_announced_and_nothing_about_cleaning_is()
    {
        await using var harness = await Ready();
        var stay = await InHouse(harness);

        await CheckOut(harness, stay.Id, stay.Version);

        var subjects = harness.Events.Types;

        Assert.Contains("stay.departed", subjects);
        Assert.DoesNotContain(subjects, subject => subject.Contains("clean", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(subjects, subject => subject.StartsWith("room.", StringComparison.Ordinal));
    }

    /// <summary>
    /// A body with no version is refused by name, not as a stale one.
    /// </summary>
    /// <remarks>
    /// Zero would reach the concurrency check and come back saying somebody
    /// else had changed the stay — a claim about the world, when what happened
    /// is that the caller never said which stay it read.
    /// </remarks>
    [Fact]
    public async Task A_body_with_no_version_is_refused_by_name()
    {
        await using var harness = await Ready();
        var stay = await InHouse(harness);

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
        var stay = await InHouse(harness);

        await Assert.ThrowsAsync<HotelOS.Platform.ConcurrencyException>(
            () => CheckOut(harness, stay.Id, stay.Version - 1));
    }

    private static Task<object?> CheckOut(DeskHarness harness, Guid stayId, long version)
    {
        var body = JsonSerializer.SerializeToElement(new
        {
            stayId = stayId.ToString(),
            version,
        });

        return Command(harness).RunAsync(harness.Scope(), body, CancellationToken.None);
    }

    private static CheckOutCommand Command(DeskHarness harness)
        => new(new StayLifecycleService(
            harness.Db, harness.Authorizer, harness.Events, harness.Clock));

    private static async Task<RoomStay> InHouse(DeskHarness harness)
    {
        var stay = await harness.SeedStayAsync(new DateTimeOffset(
            new DateOnly(2026, 8, 31).ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero));

        stay.Lifecycle = StayLifecycle.InHouse;
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
