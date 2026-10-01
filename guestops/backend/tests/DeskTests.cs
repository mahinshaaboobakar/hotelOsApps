using HotelOS.GuestOps.Application.Registrations;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The card, the filing, the request and the note — section 2.7.
/// </summary>
public class DeskTests
{
    /// <summary>The first capture mints a number from the property's series.</summary>
    /// <remarks>
    /// <b>Minted with the card, in one commit.</b> A number taken in its own
    /// transaction would leave a gap whenever the card failed to save, and a gap
    /// in a registration series is a question a property gets asked at an
    /// inspection.
    /// </remarks>
    [Fact]
    public async Task The_first_capture_mints_a_card_number()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var captured = await harness.Registrations.CaptureAsync(
            harness.Scope(), stay.Id, new RegistrationEdit(NameAsOnId: "Rajesh Pillai"),
            CancellationToken.None);

        Assert.Equal("GRC-1", captured.Card.CardNumber);

        var settings = await harness.Db.Settings.SingleAsync();
        Assert.Equal(2, settings.NextCardNumber);
    }

    /// <summary>A second capture updates the card and does not mint again.</summary>
    /// <remarks>
    /// A card is filled in over the length of a check-in — a name, then a
    /// document, then a signature. Modelling it as a create would make the
    /// desk's second keystroke an error, and burning a number on every
    /// keystroke would shred the series.
    /// </remarks>
    [Fact]
    public async Task Capturing_again_updates_the_same_card()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        var scope = harness.Scope();

        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(NameAsOnId: "Rajesh Pillai"),
            CancellationToken.None);

        var second = await harness.Registrations.CaptureAsync(
            scope,
            stay.Id,
            new RegistrationEdit(NameAsOnId: "Rajesh Pillai", IdNumber: "X99"),
            CancellationToken.None);

        Assert.Equal("GRC-1", second.Card.CardNumber);
        Assert.Equal("X99", second.Card.IdNumber);
        Assert.Single(await harness.Db.Registrations.ToListAsync());
    }

    /// <summary>An incomplete card is reported, never refused.</summary>
    /// <remarks>
    /// A guest at the desk at midnight is served and the card is completed
    /// after. The same reasoning keeps the filing obligation from gating
    /// anything (S19b).
    /// </remarks>
    [Fact]
    public async Task An_incomplete_card_is_stored_and_its_gaps_reported()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var captured = await harness.Registrations.CaptureAsync(
            harness.Scope(), stay.Id, new RegistrationEdit(NameAsOnId: "Rajesh Pillai"),
            CancellationToken.None);

        Assert.Equal(["id_type", "id_number"], captured.Missing);
        Assert.NotNull(await harness.Db.Registrations.SingleOrDefaultAsync());
    }

    /// <summary>
    /// A visitor's card creates an obligation with a deadline; a local's does
    /// not.
    /// </summary>
    /// <remarks>
    /// <b>Recomputed on every capture, because nationality arrives late.</b> An
    /// obligation decided once at check-in would be decided before the fact that
    /// determines it was known.
    /// </remarks>
    [Fact]
    public async Task Capturing_a_visitors_nationality_raises_the_obligation()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync(homeCountry: "IN");
        var stay = await harness.SeedStayAsync(Arrival);
        var scope = harness.Scope();

        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(NameAsOnId: "Joseph Mathew"),
            CancellationToken.None);

        var before = await harness.Db.Reporting.SingleAsync();
        Assert.Equal(ReportingState.NotRequired, before.State);
        Assert.Null(before.RequiredBy);

        await harness.Registrations.CaptureAsync(
            scope,
            stay.Id,
            new RegistrationEdit(NameAsOnId: "Joseph Mathew", Nationality: "GB"),
            CancellationToken.None);

        var after = await harness.Db.Reporting.SingleAsync();
        Assert.Equal(ReportingState.Needed, after.State);
        Assert.Equal(new DateOnly(2026, 9, 2), after.RequiredBy);
        Assert.Equal("the local police station", after.Authority);
    }

    /// <summary>A filing already made is never revised by a later capture.</summary>
    /// <remarks>
    /// The row is the property's evidence of what it asserted. Changing its
    /// state because a nationality was corrected afterwards would rewrite that
    /// record, which is the one thing it exists to preserve.
    /// </remarks>
    [Fact]
    public async Task A_recorded_filing_survives_a_later_correction()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        var scope = harness.Scope();

        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(Nationality: "GB"), CancellationToken.None);

        await harness.Reporting.RecordFilingAsync(
            scope, stay.Id, "the local police station", "REF-8891", CancellationToken.None);

        // The desk corrects the nationality to a local one afterwards.
        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(Nationality: "IN"), CancellationToken.None);

        var reporting = await harness.Db.Reporting.SingleAsync();
        Assert.Equal(ReportingState.Filed, reporting.State);
        Assert.Equal("REF-8891", reporting.Reference);
    }

    /// <summary>A filing without a receipt is refused.</summary>
    /// <remarks>
    /// A filing recorded without one asserts compliance and carries no evidence
    /// of it — worse than an outstanding row, which at least tells the truth.
    /// </remarks>
    [Fact]
    public async Task A_filing_without_a_receipt_is_refused()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        var scope = harness.Scope();

        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(Nationality: "GB"), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            harness.Reporting.RecordFilingAsync(
                scope, stay.Id, "the local police station", "  ", CancellationToken.None));
    }

    /// <summary>Filing a stay the policy does not cover is refused, loudly.</summary>
    /// <remarks>
    /// It means the desk and the configuration disagree about who must be
    /// filed. Quietly accepting it would hide a misconfiguration behind an
    /// apparently complete record.
    /// </remarks>
    [Fact]
    public async Task Filing_a_stay_outside_the_policy_is_refused()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync(homeCountry: "IN");
        var stay = await harness.SeedStayAsync(Arrival);
        var scope = harness.Scope();

        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(Nationality: "IN"), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            harness.Reporting.RecordFilingAsync(
                scope, stay.Id, "the local police station", "REF-1", CancellationToken.None));
    }

    /// <summary>
    /// A stay with no arrival is still listed as outstanding — R25.
    /// </summary>
    /// <remarks>
    /// It has no computable deadline, and dropping it would make the one filing
    /// nobody can date also the one nobody sees.
    /// </remarks>
    [Fact]
    public async Task An_undated_obligation_still_appears_on_the_outstanding_list()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(arrival: null);
        var scope = harness.Scope();

        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(Nationality: "GB"), CancellationToken.None);

        var outstanding = await harness.Reporting.OutstandingAsync(
            scope, new DateOnly(2026, 9, 1), CancellationToken.None);

        var row = Assert.Single(outstanding);
        Assert.Equal(stay.Id, row.StayId);
        Assert.Null(row.RequiredBy);
    }

    /// <summary>A request handed off is announced with a correlation id.</summary>
    /// <remarks>
    /// EVT-Q3: between applications the reply is an event carrying this id,
    /// never a blocking call. A call would break the events-only rule and
    /// APPS-Q2 at once — an absent Jobs would hang the desk.
    /// </remarks>
    [Fact]
    public async Task A_handed_off_request_is_announced_with_a_correlation_id()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var request = await harness.Requests.LogAsync(
            harness.Scope(), stay.Id, "an extra pillow", handOff: true, CancellationToken.None);

        Assert.Equal(["stay.request_raised"], harness.Events.Types);
        Assert.NotNull(request.CorrelationId);
        Assert.True(request.HandedOff);
    }

    /// <summary>A request that is not work is recorded and announced to nobody.</summary>
    /// <remarks>
    /// A late checkout is answered at the desk. The request is a fact about the
    /// stay and lives here whether or not anything follows from it — which is
    /// also what an uninstalled Jobs looks like (APPS-Q2).
    /// </remarks>
    [Fact]
    public async Task A_request_that_is_not_work_publishes_nothing()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var request = await harness.Requests.LogAsync(
            harness.Scope(), stay.Id, "a late checkout", handOff: false, CancellationToken.None);

        Assert.Empty(harness.Events.Types);
        Assert.Null(request.CorrelationId);
        Assert.Null(request.JobId);
    }

    /// <summary>Jobs' reply is stored against the request that asked.</summary>
    [Fact]
    public async Task The_reply_stores_the_job_against_its_request()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var request = await harness.Requests.LogAsync(
            harness.Scope(), stay.Id, "a doctor", handOff: true, CancellationToken.None);

        var job = Guid.NewGuid();

        Assert.True(await harness.Requests.RecordJobAsync(
            request.CorrelationId!.Value, job, CancellationToken.None));

        var stored = await harness.Db.Requests.SingleAsync();
        Assert.Equal(job, stored.JobId);
    }

    /// <summary>A reply for somebody else's request is not an error.</summary>
    /// <remarks>
    /// A consumer that threw would dead-letter a message that is merely not
    /// ours. It returns false and the caller acknowledges.
    /// </remarks>
    [Fact]
    public async Task A_reply_for_an_unknown_correlation_is_ignored_quietly()
    {
        await using var harness = await DeskHarness.CreateAsync();

        Assert.False(await harness.Requests.RecordJobAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    /// <summary>A note is stored against the stay it is about.</summary>
    [Fact]
    public async Task A_note_is_recorded_against_the_stay()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        await harness.Requests.AddNoteAsync(
            harness.Scope(), stay.Id, "asked for the room to be made up late",
            CancellationToken.None);

        var note = await harness.Db.Notes.SingleAsync();
        Assert.Equal(stay.Id, note.StayId);

        // A note is this application's own record; no other application acts on
        // it, so nothing is announced.
        Assert.Empty(harness.Events.Types);
    }

    /// <summary>Capturing a card asks for the capture permission.</summary>
    [Fact]
    public async Task Capture_asks_for_the_registration_permission()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        harness.Authorizer.Permissions.Clear();

        await harness.Registrations.CaptureAsync(
            harness.Scope(), stay.Id, new RegistrationEdit(NameAsOnId: "A"),
            CancellationToken.None);

        Assert.Equal(["registration.capture"], harness.Authorizer.Permissions);
    }

    /// <summary>Filing asks for its own permission, not the card's.</summary>
    /// <remarks>
    /// Separate because it is an assertion about an external obligation rather
    /// than about our own record: the person who types the card and the person
    /// who files with an authority are not always the same.
    /// </remarks>
    [Fact]
    public async Task Filing_asks_for_the_reporting_permission()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        var scope = harness.Scope();

        await harness.Registrations.CaptureAsync(
            scope, stay.Id, new RegistrationEdit(Nationality: "GB"), CancellationToken.None);

        harness.Authorizer.Permissions.Clear();

        await harness.Reporting.RecordFilingAsync(
            scope, stay.Id, "the local police station", "REF-2", CancellationToken.None);

        Assert.Equal(["reporting.file"], harness.Authorizer.Permissions);
    }

    /// <summary>
    /// Capturing a card on an unconfigured property is refused, and the refusal
    /// names the remedy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This replaces <c>An_unconfigured_property_refuses_rather_than_inventing_defaults</c>,
    /// asserting <c>NotFoundException</c></b> — ADR 0034, a test encoding a
    /// superseded contract. Its name stated the rule GUEST-Q15 withdrew: an
    /// unconfigured <i>read</i> now answers with the declared defaults, so
    /// "refuses rather than inventing defaults" is no longer GuestOps' behaviour
    /// and a passing test under that name would be fresh evidence for a claim
    /// nobody holds. <i>It failed instead of passing quietly, which is the better
    /// of the two ways a stale name is found.</i>
    /// </para>
    /// <para>
    /// <b>What survives is the behaviour, and the behaviour did not change:
    /// capture has never worked on an unconfigured property.</b> It threw
    /// <c>NotFoundException</c> from <c>LoadAsync</c> — wrong, because the
    /// property is not missing, and the desk read 404. It now refuses from the
    /// card series, with the reason and the remedy. <b>Only the sentence the desk
    /// receives has moved</b>, which is why this is a replacement rather than a
    /// new ruling.
    /// </para>
    /// <para>
    /// <b>The exception class is asserted, not just the throw.</b> A
    /// <c>PreconditionFailedException</c> is a <c>DomainException</c>, so the
    /// envelope answers 409 <i>carrying this message</i>; the first version of the
    /// guard threw <c>InvalidOperationException</c>, which the envelope does not
    /// catch — 500, and the Shell drops the body. Asserting the type is what
    /// separates "it refused" from "the desk can read why".
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Capturing_a_card_before_the_property_is_configured_is_refused_with_the_remedy()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var refused = await Assert.ThrowsAsync<PreconditionFailedException>(() =>
            harness.Registrations.CaptureAsync(
                harness.Scope(), stay.Id, new RegistrationEdit(NameAsOnId: "A"),
                CancellationToken.None));

        // The remedy, not just the refusal — a desk told "no" with no next step
        // has to find someone who knows the schema.
        Assert.Contains("Setup", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// And the same capture SUCCEEDS once the property is configured — the
    /// accepting counterpart, without which the refusal proves nothing.
    /// </summary>
    /// <remarks>
    /// A guard that refused every capture would pass the test above. This is the
    /// positive control for it: same stay, same edit, one `ConfigureAsync` apart.
    /// </remarks>
    [Fact]
    public async Task And_succeeds_once_it_is_configured()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        await harness.ConfigureAsync();

        var card = await harness.Registrations.CaptureAsync(
            harness.Scope(), stay.Id, new RegistrationEdit(NameAsOnId: "A"),
            CancellationToken.None);

        Assert.StartsWith("GRC-", card.Card.CardNumber, StringComparison.Ordinal);
    }

    /// <summary>A home country that is not a two-letter code is refused.</summary>
    [Fact]
    public async Task Settings_refuse_a_home_country_that_is_not_a_country_code()
    {
        await using var harness = await DeskHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            harness.Settings.SaveAsync(
                harness.Scope(), Edit("India"), version: 0, CancellationToken.None));
    }

    /// <summary>A deadline before the guest arrives is refused — R18.</summary>
    [Fact]
    public async Task Settings_refuse_a_non_positive_due_offset()
    {
        await using var harness = await DeskHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            harness.Settings.SaveAsync(
                harness.Scope(), Edit("IN", dueHours: 0), version: 0, CancellationToken.None));
    }

    /// <summary>Saving settings the first time creates the row.</summary>
    [Fact]
    public async Task Saving_settings_creates_the_row_and_upper_cases_the_country()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var saved = await harness.Settings.SaveAsync(
            harness.Scope(), Edit("ae"), version: 0, CancellationToken.None);

        Assert.Equal("AE", saved.HomeCountry);
        Assert.Equal(1, saved.Version);
    }

    private static readonly DateTimeOffset Arrival =
        new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);

    private static Application.Settings.SettingsEdit Edit(
        string homeCountry, int dueHours = 24)
        => new(
            homeCountry,
            ["name_as_on_id"],
            ["name_as_on_id", "passport_number"],
            ["passport"],
            SignatureRequired: true,
            PrintOnCheckIn: false,
            CardNumberPrefix: "GRC-",
            ReportingRequired: true,
            ReportingAppliesTo: ReportingScope.FromOutside,
            ReportingAuthority: "the local police station",
            ReportingDueHours: dueHours);
}
