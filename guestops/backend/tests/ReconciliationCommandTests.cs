using System.Text.Json;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The door onto GUEST-Q3's clear — which did not exist until 2026-10-04 while
/// the service behind it was complete and tested.
/// </summary>
/// <remarks>
/// <c>ReconciliationTests</c> proves the SERVICE. This proves a caller can reach
/// it: the band on frame 3 drew both controls off because no method served them,
/// and a service with no door is invisible to every check in the estate.
/// </remarks>
public sealed class ReconciliationCommandTests
{
    [Fact]
    public async Task Keeping_ours_settles_the_row_and_publishes_nothing()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var row = await StandingAsync(harness);
        harness.Events.Types.Clear();

        var answer = await Clear(harness, row.Id, "ours");

        Assert.Equal(nameof(DisagreementState.ClearedOurs), Read(answer, "state"));
        Assert.Empty(harness.Events.Types);
    }

    /// <summary>
    /// Taking the PMS's side publishes the correction a room move does.
    /// </summary>
    /// <remarks>
    /// Asserted through the DOOR rather than the service, because this is the
    /// arm that reaches other applications: a door that routed both sides to
    /// <c>Ours</c> would pass the test above and announce nothing here.
    /// </remarks>
    [Fact]
    public async Task Taking_the_pms_side_settles_the_row_and_announces_it()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var row = await StandingAsync(harness);
        harness.Events.Types.Clear();

        var answer = await Clear(harness, row.Id, "pms");

        Assert.Equal(nameof(DisagreementState.ClearedPms), Read(answer, "state"));
        Assert.NotEmpty(harness.Events.Types);
    }

    /// <summary>Both values come back, whichever side won.</summary>
    /// <remarks>
    /// The screen redraws the history from these. A response carrying only the
    /// winner would make the losing value unrecoverable at the moment somebody
    /// wants to explain the choice — which is the ruling's own reasoning for
    /// keeping both on the row.
    /// </remarks>
    [Fact]
    public async Task Both_values_come_back_whichever_side_won()
    {
        await using var harness = await InboundHarness.CreateAsync();
        var row = await StandingAsync(harness);

        var answer = await Clear(harness, row.Id, "ours");

        Assert.Equal(row.OurValue, Read(answer, "ours"));
        Assert.Equal(row.PmsValue, Read(answer, "pms"));
    }

    [Fact]
    public async Task A_side_the_application_does_not_have_is_refused_by_name()
    {
        // Defaulting it would decide a reconciliation nobody asked for — and
        // the PMS side publishes, so a wrong default announces a room change.
        await using var harness = await InboundHarness.CreateAsync();
        var row = await StandingAsync(harness);

        // **Cleared, because the ARRANGEMENT publishes.** Seeding the stay
        // emits `stay.created`, so asserting emptiness without this measured
        // the setup and called it the refusal — which is how it failed on its
        // first run, correctly: an assertion must claim only what it measured.
        harness.Events.Types.Clear();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Clear(harness, row.Id, "Ours"));

        Assert.Contains("'Ours' is not a side", refused.Message);
        Assert.Empty(harness.Events.Types);
    }

    [Fact]
    public async Task A_body_with_no_row_is_refused_before_anything_runs()
    {
        await using var harness = await InboundHarness.CreateAsync();
        await StandingAsync(harness);

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => new ReconciliationCommand(harness.Reconciliation).RunAsync(
                harness.Scope(),
                JsonDocument.Parse("""{"side":"ours"}""").RootElement,
                CancellationToken.None));

        Assert.Contains("the row it is about", refused.Message);
    }

    /// <summary>A disagreement standing on a stay, through the real path.</summary>
    private static async Task<StayDisagreement> StandingAsync(InboundHarness harness)
    {
        var scope = harness.Scope();
        await harness.SeedOverriddenStayAsync(ours: StayLifecycle.InHouse);

        await harness.Inbound.ApplyAsync(
            scope,
            InboundHarness.Fact(StayLifecycle.Departed, room: InboundHarness.Room),
            CancellationToken.None);

        return await harness.Db.Disagreements.SingleAsync();
    }

    private static Task<object?> Clear(InboundHarness harness, Guid id, string side)
        => new ReconciliationCommand(harness.Reconciliation).RunAsync(
            harness.Scope(),
            JsonDocument.Parse($$"""{"disagreementId":"{{id}}","side":"{{side}}"}""").RootElement,
            CancellationToken.None);

    private static string? Read(object? answer, string name)
        => answer?.GetType().GetProperty(name)?.GetValue(answer)?.ToString();
}
