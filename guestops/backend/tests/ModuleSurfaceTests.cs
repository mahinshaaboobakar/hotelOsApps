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
}
