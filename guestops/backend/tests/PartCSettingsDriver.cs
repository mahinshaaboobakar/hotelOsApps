using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Part C's driver for <c>SaveSettings</c> — every combination its wire admits.
/// </summary>
/// <remarks>
/// <para>
/// <b>Part C creates data; it does not press controls</b> — ADR 0358 as the owner
/// ruled it, *"call the apps apis and put data — with all combination"*. Operating
/// the screens is Part B and Part B is the owner's walk. So this finishes when the
/// data exists in every shape, not when a control has been pressed.
/// </para>
/// <para>
/// <b>Through the API, never the database</b> — ADR 0228: *"a seeder is a TEST of
/// the write path, not a shortcut around it."* Every row below is written by
/// <c>SettingsService.SaveAsync</c>, so its validation, its upsert and its version
/// are exercised. A combination the API refuses is a finding at the moment it is
/// refused.
/// </para>
/// <para>
/// <b>Why <c>SaveSettings</c> is the first slice.</b> Of the 12 writes it carries
/// the densest input space: 13 request fields, <b>three of the five repeated fields
/// on the whole write path</b>, and <b>two of the 18 refusals the application
/// states</b>. So one RPC covers empty·single·many, a boundary, and a rejection
/// with its accepting counterpart.
/// </para>
/// <para>
/// <b>The axis limits are measured and they bound what this can claim.</b> The
/// two-absences axis is drivable at 8 of 114 leaf positions and in exactly one
/// scalar — proto3 gives a plain scalar implicit presence, so for the rest *never
/// sent* and *sent empty* are the same bytes. <b>None of the eight is on
/// <c>SaveSettings</c></b>, so this slice does not drive that axis at all, and says
/// so rather than appearing to.
/// </para>
/// </remarks>
public class PartCSettingsDriver
{
    /// <summary>empty · single · many, on each of the three lists — nine combinations.</summary>
    /// <remarks>
    /// These are the three stored selections GUEST-Q15 ruled application state. The
    /// shape on the wire is unchanged by that ruling, so Part C drives them the same
    /// way wherever they live.
    /// <para>
    /// <b>`many` is three rather than two</b>: two distinguishes empty from
    /// non-empty and nothing more, and a screen that renders the first and last of a
    /// list correctly while dropping the middle passes at two.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Each_list_takes_empty_single_and_many(int count)
    {
        await using var harness = await DeskHarness.CreateAsync();
        var scope = harness.Scope();
        var values = Enumerable.Range(1, count).Select(n => $"doc-{n}").ToArray();

        var saved = await harness.Settings.SaveAsync(
            scope,
            Edit(requiredHome: values, requiredVisitors: values, acceptedIds: values),
            version: 0,
            default);

        Assert.Equal(count, saved.RequiredForHomeCountry.Count);
        Assert.Equal(count, saved.RequiredForVisitors.Count);
        Assert.Equal(count, saved.AcceptedIdTypes.Count);
        Assert.True(saved.Stored);
    }

    /// <summary>The three lists are independent — a shape nine identical lists cannot show.</summary>
    /// <remarks>
    /// Driving all three with the same array proves each is persisted and nothing
    /// about whether they are <i>separate</i>. A service that wrote one list into all
    /// three would pass every case above.
    /// </remarks>
    [Fact]
    public async Task The_three_lists_do_not_share_a_value()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var saved = await harness.Settings.SaveAsync(
            harness.Scope(),
            Edit(requiredHome: ["a"], requiredVisitors: ["b", "c"], acceptedIds: ["d", "e", "f"]),
            version: 0,
            default);

        Assert.Equal(["a"], saved.RequiredForHomeCountry);
        Assert.Equal(["b", "c"], saved.RequiredForVisitors);
        Assert.Equal(["d", "e", "f"], saved.AcceptedIdTypes);
    }

    /// <summary><c>home_country</c>'s boundary, and a rejection either side of it.</summary>
    /// <remarks>
    /// `SettingsService.cs:90` — one of the 18 refusals the application states. The
    /// accepting case is driven beside both rejections, because a validator that
    /// refused everything would pass two of the three.
    /// </remarks>
    [Theory]
    [InlineData("I", false)]
    [InlineData("IN", true)]
    [InlineData("IND", false)]
    [InlineData("", false)]
    public async Task Home_country_takes_exactly_two_characters(string code, bool accepted)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var save = () => harness.Settings.SaveAsync(
            harness.Scope(), Edit(homeCountry: code), version: 0, default);

        if (accepted)
        {
            var saved = await save();
            Assert.Equal(code.ToUpperInvariant(), saved.HomeCountry);
            return;
        }

        var refused = await Assert.ThrowsAsync<InvalidRequestException>(save);
        Assert.Contains("ISO 3166-1", refused.Message, StringComparison.Ordinal);
    }

    /// <summary><c>reporting_due_hours</c>' boundary — zero and below are refused.</summary>
    /// <remarks>
    /// `SettingsService.cs:97`. <b>1 is driven, not just 24</b>: the edge of the
    /// accepted range is where an off-by-one lives, and 24 is the declared default,
    /// so a validator written as <c>&lt; 24</c> would pass a test that only used it.
    /// </remarks>
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(24, true)]
    public async Task Reporting_due_hours_must_be_positive(int hours, bool accepted)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var save = () => harness.Settings.SaveAsync(
            harness.Scope(), Edit(dueHours: hours), version: 0, default);

        if (accepted)
        {
            Assert.Equal(hours, (await save()).ReportingDueHours);
            return;
        }

        var refused = await Assert.ThrowsAsync<InvalidRequestException>(save);
        Assert.Contains("must be positive", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>Both booleans take both values — four combinations, and they are independent.</summary>
    /// <remarks>
    /// Driven as a pair rather than one at a time: a service that assigned one
    /// field's value to both would pass every single-field case.
    /// </remarks>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task The_two_booleans_are_independent(bool signature, bool print)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var saved = await harness.Settings.SaveAsync(
            harness.Scope(), Edit(signature: signature, print: print), version: 0, default);

        Assert.Equal(signature, saved.SignatureRequired);
        Assert.Equal(print, saved.PrintOnCheckIn);
    }

    /// <summary>
    /// The one place <c>SaveSettings</c> admits an absence, and it is a nullable string.
    /// </summary>
    /// <remarks>
    /// <c>ReportingAuthority</c> is <c>string?</c> on the entity, so null and empty
    /// are distinguishable <i>in the service</i> even though the proto's
    /// <c>string</c> cannot carry the difference. Driven because the entity admits
    /// it — and recorded as a service-level absence, not a wire-level one, so the
    /// 8-of-114 figure is not quietly inflated.
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("the district authority")]
    public async Task Reporting_authority_takes_null_empty_and_a_value(string? authority)
    {
        await using var harness = await DeskHarness.CreateAsync();

        var saved = await harness.Settings.SaveAsync(
            harness.Scope(), Edit(authority: authority), version: 0, default);

        Assert.Equal(authority, saved.ReportingAuthority);
    }

    /// <summary>The second save is a concurrency check, not a second create.</summary>
    /// <remarks>
    /// <para>
    /// The upsert is why the settings deadlock had a small remedy, so it is driven:
    /// create at version 0, update at the version the create returned, and a stale
    /// version is refused rather than silently winning.
    /// </para>
    /// <para>
    /// <b>The version is captured as a VALUE, and the first version of this test
    /// failed because it was not.</b> <c>SaveAsync</c> returns the entity EF is
    /// tracking, so <c>created</c> and <c>updated</c> are the same instance: the
    /// second save incremented the version through both references, and
    /// <c>updated.Version &gt; created.Version</c> compared a value with itself.
    /// <i>A tracked entity is not a snapshot</i> — asserted below, so the next
    /// author meets the reason rather than the failure.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_second_save_updates_and_a_stale_version_is_refused()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var scope = harness.Scope();

        var created = await harness.Settings.SaveAsync(scope, Edit(homeCountry: "IN"), 0, default);
        var createdVersion = created.Version;

        var updated = await harness.Settings.SaveAsync(
            scope, Edit(homeCountry: "AE"), createdVersion, default);

        // The two really are one object. Asserted rather than commented, because it
        // is the reason the captured value above is necessary.
        Assert.Same(created, updated);

        Assert.Equal("AE", updated.HomeCountry);
        Assert.True(updated.Version > createdVersion);

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => harness.Settings.SaveAsync(scope, Edit(homeCountry: "IN"), 0, default));
    }

    /// <summary>One edit, with every field defaulted to a value the service accepts.</summary>
    private static SettingsEdit Edit(
        string homeCountry = "IN",
        string[]? requiredHome = null,
        string[]? requiredVisitors = null,
        string[]? acceptedIds = null,
        bool signature = true,
        bool print = false,
        string? authority = null,
        int dueHours = 24) => new(
            HomeCountry: homeCountry,
            RequiredForHomeCountry: requiredHome ?? [],
            RequiredForVisitors: requiredVisitors ?? [],
            AcceptedIdTypes: acceptedIds ?? [],
            SignatureRequired: signature,
            PrintOnCheckIn: print,
            CardNumberPrefix: "GRC-",
            ReportingRequired: false,
            ReportingAppliesTo: ReportingScope.FromOutside,
            ReportingAuthority: authority,
            ReportingDueHours: dueHours);
}
