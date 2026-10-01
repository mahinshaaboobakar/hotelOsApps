using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// A property nobody has configured is answered, not refused — GUEST-Q15.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect these exist for cascaded across four surfaces from one absent
/// row.</b> <c>LoadAsync</c> threw <c>NotFoundException</c>, the module envelope
/// answers a domain not-found with 404, and five call sites read it: the Setup
/// screen, the registration card, the capture write and its read-back. So a
/// fresh property's Setup screen 404'd — <i>and the Setup screen is the only
/// surface that can create the row.</i> The one remedy was inside the blast
/// radius.
/// </para>
/// <para>
/// <b>Nothing asserted the old behaviour.</b> No test named it, so the 404 was
/// held up by a single <c>??</c> and discovered by the owner pressing a tab.
/// That is why the replacement is asserted here in four directions rather than
/// one: the answer, the distinction it must preserve, the declared values it
/// must carry, and the increment it must refuse.
/// </para>
/// <para>
/// <b>The save was always an upsert</b> — <c>SaveAsync</c> creates the row when
/// absent — so only the read blocked. The door was unlocked and the handle was
/// on the inside, which is why the remedy is a factory rather than a migration.
/// </para>
/// </remarks>
public class UnconfiguredSettingsTests
{
    /// <summary>The read answers rather than refusing — the deadlock itself.</summary>
    [Fact]
    public async Task An_unconfigured_property_is_answered_with_the_declared_defaults()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var it = await harness.Settings.GetAsync(harness.Scope(), default);

        Assert.NotNull(it);
        Assert.Equal(DeskHarness.Property, it.PropertyId);
    }

    /// <summary>
    /// And it says it is the defaults, which is the half that keeps it honest.
    /// </summary>
    /// <remarks>
    /// A screen drawing these as the property's own settings would be the
    /// stand-in a failed read must never produce. The two facts — <i>configured
    /// to require nothing</i> and <i>nobody has configured it</i> — have
    /// different remedies, so they are two values rather than one.
    /// </remarks>
    [Fact]
    public async Task And_says_it_is_the_defaults_rather_than_this_propertys_settings()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var it = await harness.Settings.GetAsync(harness.Scope(), default);

        Assert.False(it.Stored);
    }

    /// <summary>
    /// A row that exists reads as stored — the other arm, which EF would get
    /// wrong on its own.
    /// </summary>
    /// <remarks>
    /// <c>Stored</c> is not a column, so EF materialises a real row with the CLR
    /// default <c>false</c>. Without <c>LoadAsync</c> setting it, a configured
    /// property would claim to be unconfigured — the fixture-satisfies-the-type
    /// failure, one layer down. This is the test that could have caught it.
    /// </remarks>
    [Fact]
    public async Task A_stored_row_reads_as_stored_even_though_it_is_not_a_column()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var scope = harness.Scope();

        await harness.Settings.SaveAsync(scope, Edit(), version: 0, default);
        var it = await harness.Settings.GetAsync(scope, default);

        Assert.True(it.Stored);
    }

    /// <summary>
    /// The defaults carry what the manifest declared, not the C# defaults.
    /// </summary>
    /// <remarks>
    /// <b>Two of the eight disagreed.</b> <c>manifest.yaml</c> declared
    /// <c>signature_required: true</c> and <c>card_prefix: "GRC-"</c> while the
    /// entity held <c>false</c> and <c>string.Empty</c> — two homes for one
    /// value, already drifted. When GUEST-Q15 ruled the eight application state
    /// and the declarations left the manifest, inheriting the C# defaults would
    /// have silently changed the approved answer for every property that has not
    /// configured itself. Asserted so it cannot drift back.
    /// </remarks>
    [Fact]
    public async Task The_defaults_are_the_values_the_manifest_declared()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var it = await harness.Settings.GetAsync(harness.Scope(), default);

        Assert.True(it.SignatureRequired);
        Assert.Equal("GRC-", it.CardNumberPrefix);
        Assert.Equal(24, it.ReportingDueHours);
        Assert.Equal(ReportingScope.FromOutside, it.ReportingAppliesTo);
    }

    /// <summary>
    /// Minting a card number from the defaults is refused by name.
    /// </summary>
    /// <remarks>
    /// The defaults are <b>untracked</b>, so incrementing them would mint a
    /// number the context cannot save and the next card would repeat it. A gap in
    /// a registration series is a question a property gets asked at an
    /// inspection; a repeat is worse.
    /// <para>
    /// <b>This said "Nothing calls <c>MintCardNumber</c> today, so this guards a
    /// hazard that is not live", and it was false when written</b> (2026-10-01).
    /// <c>RegistrationService.cs:75</c> mints on a card's first write, so the
    /// hazard was live on the desk's main path — and the same false sentence was
    /// written into the method's own remarks, which is why it is corrected in both
    /// places rather than in the one that was reported. <i>A claim gets copied;
    /// grep the sentence, not the file.</i>
    /// </para>
    /// <para>
    /// The guard caught it immediately, which is the thing to notice: the suite
    /// failed at <c>DeskTests</c> rather than a property discovering a duplicate
    /// card number at an inspection.
    /// </para>
    /// </remarks>
    [Fact]
    public void Minting_a_card_number_from_the_defaults_is_refused()
    {
        var defaults = GuestOpsSettings.DefaultsFor(DeskHarness.Property);

        var refused = Assert.Throws<PreconditionFailedException>(
            () => SettingsService.MintCardNumber(defaults));

        Assert.Contains("settings have been saved", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>A stored row still mints, so the guard did not close the door.</summary>
    /// <remarks>
    /// The other half of the refusal above. A guard that refused every caller
    /// would pass the test above and break the feature — the
    /// check-that-refuses-its-own-callers failure.
    /// </remarks>
    [Fact]
    public async Task A_stored_row_still_mints()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var scope = harness.Scope();
        await harness.Settings.SaveAsync(scope, Edit(), version: 0, default);
        var stored = await harness.Settings.GetAsync(scope, default);

        var number = SettingsService.MintCardNumber(stored);

        Assert.Equal("GRC-1", number);
    }

    /// <summary>A minimal valid edit — two fields are validated, so both are set.</summary>
    private static SettingsEdit Edit() => new(
        HomeCountry: "IN",
        RequiredForHomeCountry: [],
        RequiredForVisitors: [],
        AcceptedIdTypes: [],
        SignatureRequired: true,
        PrintOnCheckIn: false,
        CardNumberPrefix: "GRC-",
        ReportingRequired: false,
        ReportingAppliesTo: ReportingScope.FromOutside,
        ReportingAuthority: null,
        ReportingDueHours: 24);
}
