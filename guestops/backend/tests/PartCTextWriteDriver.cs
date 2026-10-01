using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Part C's driver for the three writes whose whole input is required text —
/// <c>RecordFiling</c>, <c>LogRequest</c> and <c>AddNote</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Part C creates data; it does not press controls</b> — ADR 0358. Every row goes
/// through the application's own service (ADR 0228).
/// </para>
/// <para>
/// <b>What the existing suites already cover, so this does not re-assert it.</b>
/// <c>DeskTests</c> drives <c>A_filing_without_a_receipt_is_refused</c>,
/// <c>Filing_a_stay_outside_the_policy_is_refused</c>,
/// <c>A_recorded_filing_survives_a_later_correction</c>,
/// <c>Filing_asks_for_the_reporting_permission</c>,
/// <c>A_note_is_recorded_against_the_stay</c>, and <b>both values of
/// <c>hand_off</c></b> — <c>A_handed_off_request_is_announced_with_a_correlation_id</c>
/// and <c>A_request_that_is_not_work_publishes_nothing</c>. So the boolean axis on
/// <c>LogRequest</c> is driven and is cited rather than duplicated (ADR 0054).
/// </para>
/// <para>
/// <b>What is left is one shape, in three places: a required string, blank.</b>
/// <c>RecordFiling</c>'s <c>authority</c> is refused by name and <b>nothing drives
/// it</b> — the existing filing refusal is about the <i>reference</i>. Neither
/// <c>LogRequest</c>'s nor <c>AddNote</c>'s <c>text</c> is driven at all.
/// </para>
/// <para>
/// <b>Three blank forms each, because "blank" is not one value.</b> Every one of
/// these guards is <c>IsNullOrWhiteSpace</c>, so a theory using only <c>""</c> would
/// pass against a weaker <c>== ""</c> and say nothing about the space a desk
/// actually types. <i>The empty·single·many axis does not apply here</i>: none of
/// these requests carries a repeated field, and proto3 gives a plain <c>string</c>
/// implicit presence, so <i>never sent</i> and <i>sent empty</i> are the same bytes.
/// </para>
/// </remarks>
public sealed class PartCTextWriteDriver
{
    /// <summary>The three forms of blank that a required string has to refuse.</summary>
    /// <remarks>
    /// <para>
    /// <c>null</c> is included because the C# surface admits it even where the wire
    /// cannot express it: a connector mapping an absent field to <c>null</c> reaches
    /// the service exactly here.
    /// </para>
    /// <para>
    /// <b>⚠ LOOPED IN ONE TEST RATHER THAN A THEORY, AND THE REASON IS A CEILING.</b>
    /// As three <c>MemberData</c> rows these were three invocations, each creating its
    /// own scratch database. Together with the lifecycle matrix the suite went from
    /// ~167 scratch databases to ~232 and <b>18 tests failed on
    /// <c>Npgsql … Exception while reading from stream</c> in suites this block never
    /// touched</b> — <c>DeskHarness</c>'s own words: provisioning on every harness
    /// <i>"exhausted <c>hotelos_migrator</c>'s connection limit."</i> So one database
    /// per test, and <b>every assertion names the form it was driving</b>, because the
    /// failure message is the deliverable.
    /// </para>
    /// </remarks>
    private static readonly string?[] Blank = [null, "", " "];

    /// <summary>
    /// <c>RecordFiling</c> refuses a blank AUTHORITY — the guard nothing drove.
    /// </summary>
    /// <remarks>
    /// <b>A filing names who it was made to.</b> A reference with no authority is a
    /// receipt number against nobody, and an inspection asks which office holds it.
    /// <c>DeskTests</c> covers the reference; this covers the other half of the same
    /// method.
    /// </remarks>
    [Fact]
    public async Task A_filing_with_no_authority_is_refused()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await Reportable(harness);

        foreach (var authority in Blank)
        {
            var refused = await Assert.ThrowsAsync<InvalidRequestException>(
                () => harness.Reporting.RecordFilingAsync(
                    harness.Scope(), stay.Id, authority!, "REF-1", default));

            Assert.True(
                refused.Message.Contains("authority", StringComparison.OrdinalIgnoreCase),
                $"authority {Show(authority)}: refused without naming the field — "
                + refused.Message);
        }
    }

    /// <summary>And a filing with both is recorded — the accepting counterpart.</summary>
    /// <remarks>
    /// Without this, a <c>RecordFilingAsync</c> that refused everything would pass
    /// every refusal above and the three in <c>DeskTests</c> besides.
    /// </remarks>
    [Fact]
    public async Task And_a_filing_with_an_authority_and_a_reference_is_recorded()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await Reportable(harness);

        await harness.Reporting.RecordFilingAsync(
            harness.Scope(), stay.Id, "the district police station", "REF-1", default);

        harness.Db.ChangeTracker.Clear();
        var reporting = await harness.Db.Reporting.FirstAsync(r => r.StayId == stay.Id);

        Assert.Equal(ReportingState.Filed, reporting.State);
    }

    /// <summary><c>LogRequest</c> refuses blank text.</summary>
    /// <remarks>
    /// A request with no text is a row that says somebody wanted something. The
    /// handed-off case is worse — it would announce a correlated job describing
    /// nothing — and that is why the guard is before the branch rather than inside
    /// it.
    /// </remarks>
    [Fact]
    public async Task A_request_with_no_text_is_refused()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        foreach (var text in Blank)
        {
            var refused = await Assert.ThrowsAsync<InvalidRequestException>(
                () => harness.Requests.LogAsync(
                    harness.Scope(), stay.Id, text!, handOff: false, default));

            Assert.True(
                refused.Message.Contains("text is required", StringComparison.Ordinal),
                $"text {Show(text)}: refused for the wrong reason — {refused.Message}");
        }
    }

    /// <summary>
    /// And blank text is refused for a HANDED-OFF request too, which is the case
    /// that would have announced something.
    /// </summary>
    /// <remarks>
    /// <b>Driven separately because the guard's position is the claim.</b> A guard
    /// placed after the hand-off branch would refuse the plain request and publish an
    /// empty one — so asserting the refusal on <c>handOff: true</c> AND that nothing
    /// was announced is what says the order is right. <i>A guard placed after the
    /// write it protects is not a guard.</i>
    /// </remarks>
    [Fact]
    public async Task A_handed_off_request_with_no_text_is_refused_and_announces_nothing()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => harness.Requests.LogAsync(
                harness.Scope(), stay.Id, "   ", handOff: true, default));

        Assert.Empty(harness.Events.Types);
    }

    /// <summary>And a request with text is stored — the counterpart.</summary>
    [Fact]
    public async Task And_a_request_with_text_is_stored()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var logged = await harness.Requests.LogAsync(
            harness.Scope(), stay.Id, "an extra pillow", handOff: false, default);

        Assert.Equal("an extra pillow", logged.Text);
    }

    /// <summary><c>AddNote</c> refuses blank text.</summary>
    /// <remarks>
    /// The same guard, the third place it lives. Driven rather than assumed from the
    /// other two: three services sharing a sentence is not three services sharing a
    /// check, and only running each one says which.
    /// </remarks>
    [Fact]
    public async Task A_note_with_no_text_is_refused()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        foreach (var text in Blank)
        {
            var refused = await Assert.ThrowsAsync<InvalidRequestException>(
                () => harness.Requests.AddNoteAsync(harness.Scope(), stay.Id, text!, default));

            Assert.True(
                refused.Message.Contains("text is required", StringComparison.Ordinal),
                $"text {Show(text)}: refused for the wrong reason — {refused.Message}");
        }
    }

    /// <summary>And a note with text is stored — the counterpart.</summary>
    [Fact]
    public async Task And_a_note_with_text_is_stored()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);

        var note = await harness.Requests.AddNoteAsync(
            harness.Scope(), stay.Id, "the guest asked for a late checkout", default);

        Assert.Equal("the guest asked for a late checkout", note.Text);
    }

    /// <summary>All three refuse a stay this property does not have.</summary>
    /// <remarks>
    /// <b>NotFound rather than forbidden</b> — ADR 0054's boundary, and it is driven
    /// once per service rather than once, because each holds its own query and a
    /// shared sentence is not a shared scope.
    /// </remarks>
    [Fact]
    public async Task None_of_the_three_reaches_a_stay_at_another_property()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var stay = await harness.SeedStayAsync(Arrival);
        var elsewhere = new RequestScope
        {
            PropertyId = Guid.CreateVersion7(),
            UserId = Guid.NewGuid(),
        };

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Requests.LogAsync(elsewhere, stay.Id, "a request", false, default));

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Requests.AddNoteAsync(elsewhere, stay.Id, "a note", default));

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Reporting.RecordFilingAsync(
                elsewhere, stay.Id, "an authority", "REF-1", default));
    }

    /// <summary>A stay this property must report on, so a filing is in policy.</summary>
    /// <remarks>
    /// <c>RecordFilingAsync</c> refuses a stay whose reporting state is
    /// <c>NotRequired</c> — already driven by <c>DeskTests</c> — so the arrangement
    /// has to raise a real obligation first, which it does the way the desk does: a
    /// configured property, and a visitor's nationality captured on the card.
    /// </remarks>
    private static async Task<RoomStay> Reportable(DeskHarness harness)
    {
        await harness.ConfigureAsync(reportingRequired: true);
        var stay = await harness.SeedStayAsync(Arrival);

        await harness.Registrations.CaptureAsync(
            harness.Scope(), stay.Id,
            new Application.Registrations.RegistrationEdit(Nationality: "GB"), default);

        return stay;
    }

    /// <summary>A blank form, printable — <c>""</c> and <c>" "</c> look alike.</summary>
    /// <remarks>
    /// The three forms exist because they are different inputs; a failure message that
    /// rendered them identically would lose which one failed, and the message is the
    /// deliverable.
    /// </remarks>
    private static string Show(string? value)
        => value is null ? "<null>" : $"\"{value}\" (length {value.Length})";

    private static readonly DateTimeOffset Arrival =
        new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
}
