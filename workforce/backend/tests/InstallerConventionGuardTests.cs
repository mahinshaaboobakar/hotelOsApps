using HotelOS.Workforce.Tests;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The harness may not write to the installed product's cluster — INSTALL-Q88.
/// </summary>
/// <remarks>
/// <para>
/// <b>No fixture and no database.</b> These assert a refusal that must happen
/// <i>before</i> a connection is opened, so a test that needed PostgreSQL to run
/// would be testing the wrong moment — and would pass on a machine where 15432
/// happened to be closed, which is the machine this defect does not occur on.
/// </para>
/// <para>
/// The rule protects something outside this process: a real property's
/// database, on the same machine, reachable right now. That is the class of
/// rule that goes in code rather than in prose beside the value it fails to
/// constrain — which is what the two comments this replaced were doing.
/// </para>
/// </remarks>
public sealed class InstallerConventionGuardTests
{
    private const string InstalledProduct =
        "Host=127.0.0.1;Port=15432;Database=postgres;Username=postgres;Password=devroot";

    [Fact]
    public async Task Creating_roles_on_the_installed_product_is_refused()
    {
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InstallerConvention.EnsureRolesAsync(InstalledProduct, "irrelevant"));

        // Names the port and what is behind it, because the reader is somebody
        // who set the variable for a reason and needs to know why not this one.
        Assert.Contains("15432", refused.Message, StringComparison.Ordinal);
        Assert.Contains("INSTALLED product", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dropping_roles_on_the_installed_product_is_refused()
    {
        // The teardown is guarded too. It runs unconditionally now — including
        // after a failed run — so an unguarded drop would be a suite deleting
        // roles a real property depends on, which is worse than the create.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => InstallerConvention.DropRolesAsync(
                InstalledProduct, new InstallerConvention.Provisioned(true, true)));
    }

    [Fact]
    public async Task A_connection_naming_no_port_is_not_the_installed_product()
    {
        // The guard parses rather than matches, so it reports Npgsql's default
        // for a string that names no port. This holds that a connection
        // assembled without an explicit port cannot silently be 15432 — and it
        // fails on a connect error rather than the refusal, which is the point.
        var refused = await Record.ExceptionAsync(
            () => InstallerConvention.EnsureRolesAsync(
                "Host=127.0.0.1;Database=postgres;Username=postgres;Password=devroot",
                "irrelevant"));

        Assert.False(
            refused is InvalidOperationException guard
            && guard.Message.Contains("INSTALLED product", StringComparison.Ordinal),
            "a portless connection string was refused as the installed product's");
    }
}
