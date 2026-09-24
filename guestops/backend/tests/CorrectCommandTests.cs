using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Putting right a lifecycle fact recorded in error — the owner's C1 and C2,
/// ruled 2026-09-24.
/// </summary>
public sealed class CorrectCommandTests
{
    [Fact]
    public async Task A_check_out_recorded_in_error_puts_the_stay_back_in_house()
    {
        await using var harness = await Ready();
        var stay = await Departed(harness);

        var answer = await Correct(harness, stay.Id, stay.Version,
            to: nameof(StayLifecycle.InHouse), reason: "checked out in error, guest is still in the room");

        Assert.Equal(nameof(StayLifecycle.InHouse), Read<string>(answer, "lifecycle"));

        harness.Db.ChangeTracker.Clear();
        var after = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);
        Assert.Equal(StayLifecycle.InHouse, after.Lifecycle);
    }

    /// <summary>
    /// Both facts survive: the mistake and the correction are each readable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the drawn end state's central claim, and the reason a
    /// correction is not an undo.</b> Somebody asking in November why 214 shows
    /// a departure clean on the 24th finds the answer in the list — the
    /// departure that was recorded, and the correction that followed it. A
    /// transition that erased the first would leave the room's cleaning record
    /// with no cause anywhere.
    /// </para>
    /// <para>
    /// Asserted on the events rather than on the stay, because the stay holds
    /// only where it ended up; the pair of facts is the record.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task The_mistake_and_the_correction_are_both_kept()
    {
        await using var harness = await Ready();
        var stay = await Departed(harness, throughTheService: true);

        await Correct(harness, stay.Id, stay.Version,
            to: nameof(StayLifecycle.InHouse), reason: "checked out in error");

        var subjects = harness.Events.Types;

        Assert.Contains("stay.departed", subjects);
        Assert.Contains("stay.corrected", subjects);
    }

    /// <summary>A no-show reinstated — the second correction the frames draw.</summary>
    /// <remarks>
    /// Here to prove the door is not shaped to the one correction it was
    /// written for. A command admitting only <c>InHouse</c> would pass every
    /// test above and refuse this, which is the affordance N1's end state
    /// draws as <i>Reinstate…</i>.
    /// </remarks>
    [Fact]
    public async Task A_no_show_can_be_reinstated()
    {
        await using var harness = await Ready();
        var stay = await Seeded(harness);

        stay.Lifecycle = StayLifecycle.NoShow;
        await harness.Db.SaveChangesAsync();

        var answer = await Correct(harness, stay.Id, stay.Version,
            to: nameof(StayLifecycle.Booked), reason: "the guest did arrive; the night desk missed them");

        Assert.Equal(nameof(StayLifecycle.Booked), Read<string>(answer, "lifecycle"));
    }

    /// <summary>
    /// A correction with no reason is refused, in the service's own words.
    /// </summary>
    /// <remarks>
    /// The dialog's required field and this refusal agree because they are the
    /// same rule, not because two places were kept in step — so the assertion
    /// is on the service's sentence, which says <i>why</i>.
    /// </remarks>
    [Fact]
    public async Task A_correction_with_no_reason_is_refused()
    {
        await using var harness = await Ready();
        var stay = await Departed(harness);

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Correct(harness, stay.Id, stay.Version,
                to: nameof(StayLifecycle.InHouse), reason: "   "));

        Assert.Contains("reason", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A state this application does not have is refused by name.</summary>
    /// <remarks>
    /// <b>Not defaulted.</b> Parsing an unknown state into the enum's first
    /// member would write some other lifecycle onto the stay and report
    /// success — a correction that corrupts the thing it was asked to put
    /// right, and the only visible sign would be a stay in a state nobody
    /// chose.
    /// </remarks>
    [Fact]
    public async Task An_unknown_state_is_refused_rather_than_defaulted()
    {
        await using var harness = await Ready();
        var stay = await Departed(harness);

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Correct(harness, stay.Id, stay.Version, to: "Levitating", reason: "a reason"));

        Assert.Contains("Levitating", refused.Message, StringComparison.Ordinal);

        harness.Db.ChangeTracker.Clear();
        var after = await harness.Db.Stays.FirstAsync(s => s.Id == stay.Id);
        Assert.Equal(StayLifecycle.Departed, after.Lifecycle);
    }

    /// <summary>A state whose spelling has drifted is refused, not understood.</summary>
    [Fact]
    public async Task A_state_in_the_wrong_case_is_refused()
    {
        await using var harness = await Ready();
        var stay = await Departed(harness);

        await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Correct(harness, stay.Id, stay.Version, to: "inhouse", reason: "a reason"));
    }

    /// <summary>A body with no version is refused by name, not as a stale one.</summary>
    [Fact]
    public async Task A_body_with_no_version_is_refused_by_name()
    {
        await using var harness = await Ready();
        var stay = await Departed(harness);

        var body = JsonSerializer.SerializeToElement(new
        {
            stayId = stay.Id.ToString(),
            to = nameof(StayLifecycle.InHouse),
            reason = "a reason",
        });

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Command(harness).RunAsync(harness.Scope(), body, CancellationToken.None));

        Assert.Contains("version", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A version somebody else has moved past is refused.</summary>
    [Fact]
    public async Task A_stale_version_is_refused()
    {
        await using var harness = await Ready();
        var stay = await Departed(harness);

        await Assert.ThrowsAsync<HotelOS.Platform.ConcurrencyException>(
            () => Correct(harness, stay.Id, stay.Version - 1,
                to: nameof(StayLifecycle.InHouse), reason: "a reason"));
    }

    private static Task<object?> Correct(
        DeskHarness harness, Guid stayId, long version, string to, string reason)
    {
        var body = JsonSerializer.SerializeToElement(new
        {
            stayId = stayId.ToString(),
            version,
            to,
            reason,
        });

        return Command(harness).RunAsync(harness.Scope(), body, CancellationToken.None);
    }

    private static CorrectCommand Command(DeskHarness harness)
        => new(Lifecycle(harness));

    private static StayLifecycleService Lifecycle(DeskHarness harness)
        => new(harness.Db, harness.Authorizer, harness.Events, harness.Clock);

    private static async Task<RoomStay> Seeded(DeskHarness harness)
        => await harness.SeedStayAsync(new DateTimeOffset(
            new DateOnly(2026, 9, 24).ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero));

    /// <summary>
    /// A stay that has departed — through the service where the test needs the
    /// departure to be a recorded fact, directly where it only needs the state.
    /// </summary>
    private static async Task<RoomStay> Departed(
        DeskHarness harness, bool throughTheService = false)
    {
        var stay = await Seeded(harness);

        stay.Lifecycle = StayLifecycle.InHouse;
        await harness.Db.SaveChangesAsync();

        if (!throughTheService)
        {
            stay.Lifecycle = StayLifecycle.Departed;
            await harness.Db.SaveChangesAsync();
            return stay;
        }

        return await Lifecycle(harness).CheckOutAsync(
            harness.Scope(), stay.Id, stay.Version, CancellationToken.None);
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
