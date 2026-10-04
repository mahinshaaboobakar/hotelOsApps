using System.Text.Json;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// GuestOps' module door, driven over HTTP — the tier this application did not have.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three faults on this door were found by a person pressing tabs.</b> Two of
/// them — Setup answering 404 and the registration card answering 404 — are
/// asserted here at the door, because that is the only place they were ever
/// visible: the service suites construct services directly and never meet the
/// envelope, and Part A compares renderings of fixtures.
/// </para>
/// <para>
/// <b>The first two tests exist to make the rest mean something.</b> A harness that
/// answered 200 to everything, token or no token, would pass every assertion below
/// while proving nothing — so the refusal is asserted before the answers.
/// </para>
/// </remarks>
public class ModuleSurfaceTests
{
    /// <summary>No bearer, no answer — and this is what makes the rest evidence.</summary>
    /// <remarks>
    /// <c>ModuleEnvelope.cs:305</c> maps <c>AuthenticationFailedException</c> to
    /// <c>Results.Unauthorized()</c>. If this returned 200 the harness would be
    /// authenticating nothing, and every passing test below would be measuring a
    /// door that is simply open.
    /// </remarks>
    [Fact]
    public async Task A_call_with_no_token_is_refused()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, _) = await surface.CallAsync(
            "reservation.read", "today", new { page = 0, pageSize = 25 }, withToken: false);

        Assert.Equal(401, status);
    }

    /// <summary>The same call with a bearer this suite minted is served.</summary>
    [Fact]
    public async Task A_call_with_a_token_is_served()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, body) = await surface.CallAsync(
            "reservation.read", "today", new { page = 0, pageSize = 25 });

        Assert.Equal(200, status);
        Assert.NotNull(body);
    }

    /// <summary>
    /// **Setup answers on a property with no settings row — the deadlock, at the door.**
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the owner's finding of 2026-09-30 as a test. <c>LoadAsync</c> threw
    /// <c>NotFoundException</c> for an unconfigured property; the envelope answers a
    /// domain not-found with 404; and the Setup screen is the only surface that can
    /// create the row. One absent row therefore 404'd four surfaces including its
    /// own remedy.
    /// </para>
    /// <para>
    /// <b>The scratch database is empty, so this drives exactly that state</b> — no
    /// settings row exists unless something writes one, and nothing here does.
    /// GUEST-Q15 and <c>7ba3d74d</c> are what make it 200.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Setup_answers_on_a_property_that_has_never_been_configured()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, body) = await surface.CallAsync("desk.configure", "setup");

        Assert.Equal(200, status);
        Assert.NotNull(body);
    }

    /// <summary>
    /// And it answers with the DECLARED defaults, not with an empty shape.
    /// </summary>
    /// <remarks>
    /// A 200 carrying nothing would satisfy the test above and leave the screen as
    /// blank as a 404 did. The card series' prefix is asserted because it is the one
    /// value that disagreed between the manifest and the entity — the manifest
    /// declared <c>GRC-</c> and the entity held <c>string.Empty</c> — so it is the
    /// value most likely to regress silently.
    /// </remarks>
    [Fact]
    public async Task And_carries_the_declared_defaults_rather_than_an_empty_shape()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (_, body) = await surface.CallAsync("desk.configure", "setup");

        Assert.NotNull(body);
        Assert.Contains("GRC-", body.Value.ToString(), StringComparison.Ordinal);
    }

    /// <summary>A method the application does not serve is refused, never 500.</summary>
    /// <remarks>
    /// Each capability handler ends in a <c>_ =&gt;</c> arm throwing
    /// <c>InvalidRequestException</c>. A 500 here would reach the bundle as
    /// <c>internal</c> and the screen would say GuestOps had broken, for a request
    /// that was simply wrong — ADR 0041's boundary.
    /// </remarks>
    [Fact]
    public async Task A_method_this_application_does_not_serve_is_refused_not_faulted()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, _) = await surface.CallAsync("reservation.read", "nothingIsCalledThis");

        Assert.NotEqual(500, status);
        Assert.InRange(status, 400, 499);
    }

    /// <summary>
    /// A capability the manifest never declared is refused at the door.
    /// </summary>
    /// <remarks>
    /// There is no route for it, so this proves the door is capability-keyed rather
    /// than a single handler branching on a string — which is what makes a declined
    /// permission cost its capability and nothing else.
    /// </remarks>
    [Fact]
    public async Task A_capability_this_application_never_declared_has_no_door()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, _) = await surface.CallAsync("nothing.declared", "anything");

        Assert.Equal(404, status);
    }

    /// <summary>
    /// Settling a disagreement has a door at all — which it did not until
    /// 2026-10-04, while the service behind it was complete and tested.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the only test that can fail if the arm is removed.</b>
    /// <c>ReconciliationCommandTests</c> constructs the command directly, so it
    /// proves the capability and says nothing about whether a caller can reach
    /// it; <c>ManifestRoutingTests</c> walks CAPABILITIES, and
    /// <c>stay.override</c> already had a door through <c>correct</c>. The
    /// missing thing was a METHOD, and nothing in the estate asserted one.
    /// </para>
    /// <para>
    /// <b>404 and the word are a double discriminator.</b> The scratch database
    /// holds no disagreement, so a routed call reaches <c>ClearAsync</c> and
    /// fails its own lookup — <i>"disagreement ‹id› was not found"</i>, 404. An
    /// UNSERVED method on a declared capability is an
    /// <c>InvalidRequestException</c> and answers 400 with <i>"is not a method
    /// this application serves"</i>, so neither the status nor the message can be
    /// reached by the unrouted path.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Settling_a_disagreement_has_a_door()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, body) = await surface.CallAsync(
            "stay.override", "clear",
            new { disagreementId = Guid.CreateVersion7(), side = "ours" });

        Assert.Equal(404, status);
        Assert.Contains("disagreement", body?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Saving this application's configuration has a door — which it did not
    /// until 2026-10-04, while the write was served over gRPC the whole time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The only test that can fail if the arm is removed.</b>
    /// <c>SettingsCommandTests</c> constructs the command directly;
    /// <c>ManifestRoutingTests</c> walks CAPABILITIES, and <c>desk.configure</c>
    /// already had a door through <c>setup</c>. The missing thing was a METHOD.
    /// </para>
    /// <para>
    /// <b>400 and the word are the discriminator.</b> A routed call reaches the
    /// command and fails its own body check — <i>"saving settings needs the
    /// version the configuration was read at"</i>, an
    /// <c>InvalidRequestException</c>. An UNSERVED method is the same exception
    /// type with a different sentence, so the STATUS cannot separate them and the
    /// message can: an unrouted call can only say <i>"is not a method this
    /// application serves"</i>.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Saving_the_configuration_has_a_door()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, body) = await surface.CallAsync("desk.configure", "configure", new { });
        var said = body?.ToString() ?? string.Empty;

        Assert.InRange(status, 400, 499);
        Assert.Contains("saving settings needs", said);
        Assert.DoesNotContain("is not a method this application serves", said);
    }

    /// <summary>
    /// Setup answers with the five sections the approved page draws, three of them
    /// carrying a reason — ADR 0378.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Written because changing this list from three to five broke no test.</b>
    /// It sent <c>Registration · Card series · Reporting</c> while <c>f17</c> draws
    /// <c>Registration · Guest reporting · Stop-sell · Stay defaults</c> and
    /// <c>fV2</c> adds <c>Reasons</c> — so the harness rendered a strip no property
    /// would, and nothing in either suite could see it.
    /// </para>
    /// <para>
    /// <b>Over the door rather than against the view</b>, because the question is
    /// what a bundle receives. A reason is what makes an undesigned tab drawn and
    /// disabled rather than removed or live-and-inert.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Setup_answers_with_the_five_sections_the_page_draws()
    {
        await using var surface = await ModuleSurface.StartAsync();

        var (status, body) = await surface.CallAsync("desk.configure", "setup");

        Assert.Equal(200, status);
        var sections = body!.Value.GetProperty("sections").EnumerateArray().ToList();

        Assert.Equal(
            new[] { "Registration", "Guest reporting", "Stop-sell", "Reasons", "Stay defaults" },
            sections.Select(one => one.GetProperty("label").GetString()).ToArray());

        // Three carry a reason and two do not: a strip that disabled everything
        // would satisfy a count, and a working tab must not have to declare that
        // nothing is wrong with it.
        var reasons = sections
            .Where(one => one.TryGetProperty("reason", out _))
            .Select(one => one.GetProperty("reason").GetString())
            .ToList();

        Assert.Equal(3, reasons.Count);
        Assert.Equal(3, reasons.Distinct().Count());
        Assert.All(reasons, reason => Assert.DoesNotContain("coming", reason!.ToLowerInvariant()));
    }
}
