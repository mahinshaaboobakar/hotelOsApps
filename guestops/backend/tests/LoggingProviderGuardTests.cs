using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// A logger is wired to a sink, so what this application logs reaches somebody.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every line this application logged was written and dropped.</b>
/// <c>DomainExceptionInterceptor</c> — the platform's, registered in
/// <c>Program.cs</c> — calls <c>log.LogError(e, "unhandled error in {Method}", …)</c>
/// on every unhandled fault. No provider was ever configured, so
/// <c>logspps\guestops\current.jsonl</c> held nothing since 20 September while
/// the application ran, answered on its port, and drew
/// <i>"Service fault — GuestOps could not build today"</i> on a desk.
/// </para>
/// <para>
/// <b>Three instruments were silent and this was the cause of one of them.</b>
/// The Kernel's capture reads this child's stdout and there was nothing on it —
/// it was not at fault and must not be sent for. The same interceptor in
/// <c>integration-service</c> produced <c>unhandled error in
/// /IntegrationService/…</c> that same morning, because that service wires a
/// provider: same code, two services, one visible.
/// </para>
/// <para>
/// <b>The console sink must come FIRST, before configuration.</b> This is
/// Workforce's finding at its own <c>Program.cs:123</c> and not a style
/// preference — <c>appsettings.json</c> here declares no <c>Serilog</c> section,
/// so <c>ReadFrom.Configuration</c> contributes no sink at all. Read
/// configuration alone and Serilog finds none, and the application logs nothing:
/// the same silence by a different route, which is why the ORDER is asserted and
/// not merely the presence.
/// </para>
/// <para>
/// <b>What this cannot see.</b> <c>UseSerilog</c> registers with the host and the
/// host does not exist until <c>Build()</c>, so the <c>PlatformEnvironment.Read()</c>
/// and the refusal beneath it — the likeliest reasons an INSTALLED application
/// stops — still bypass it. Workforce recorded that at its <c>Program.cs:57</c>.
/// A startup failure here is still silent, and that gap is named rather than
/// left to be met as a second silence.
/// </para>
/// <para>
/// A text guard is the right instrument here because the defect is an ABSENCE —
/// a declaration nobody made — which is detectable in the source. It is the
/// wrong instrument for whether a line actually reaches stdout, and that is not
/// claimed.
/// </para>
/// </remarks>
public sealed class LoggingProviderGuardTests
{
    private static string Program([System.Runtime.CompilerServices.CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "src", "Program.cs"));

    /// <summary>Source with line comments removed — a comment is a record, not a wiring.</summary>
    private static string Code()
        => string.Join(
            '\n',
            File.ReadAllLines(Program()).Select(line =>
            {
                var at = line.IndexOf("//", StringComparison.Ordinal);
                return at < 0 ? line : line[..at];
            }));

    [Fact]
    public void A_logging_provider_is_configured_at_all()
    {
        Assert.Matches(new Regex(@"UseHotelOsLogging\s*\("), Code());
    }

    /// <summary>
    /// And it is the PLATFORM's, not a sink declared here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This replaces <c>The_console_sink_is_declared_before_configuration_is_read</c>,
    /// which asserted <c>WriteTo.Console</c> before <c>ReadFrom.Configuration</c>.
    /// Both now live in <c>HotelOS.Platform.PlatformLogging</c>, so the old
    /// assertion would read two absences as an ordering and pass on a file with
    /// no logging at all — ADR 0034: a test encoding a superseded contract is
    /// updated, not worked around.
    /// </para>
    /// <para>
    /// <b>The guarantee it loses is not needed and the one it gains is
    /// stronger.</b> The ordering was a workaround for the risk that
    /// <c>appsettings.json</c> contributes no sink; <c>UseHotelOsLogging</c>
    /// adds one unconditionally, so the risk is gone. What can now go wrong is
    /// the opposite: somebody adds <c>.WriteTo.Console()</c> back for a local
    /// debugging session, and every captured line returns to <c>unparsed</c>
    /// with no level — the defect this application was measured at 53,351 of
    /// 53,351 records before the change. A plain-text sink here is the
    /// regression, so its ABSENCE is what is asserted.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_console_sink_is_declared_here_because_the_shape_is_the_platforms()
    {
        var code = Code();

        Assert.DoesNotContain("WriteTo.Console", code, StringComparison.Ordinal);
        Assert.DoesNotContain("UseSerilog", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// The provider is registered before anything that can throw after the host exists.
    /// </summary>
    /// <remarks>
    /// A logger configured after the thing that can fail is a logger for the happy
    /// path — Workforce's sentence. <c>PlatformEnvironment.Read()</c> is the
    /// likeliest failure, and while it runs before <c>Build()</c> and so escapes
    /// this logger either way, registering after it would ALSO lose every
    /// post-build failure in between.
    /// </remarks>
    [Fact]
    public void The_provider_is_registered_before_the_platform_environment_is_read()
    {
        var code = Code();
        var wired = code.IndexOf("UseHotelOsLogging", StringComparison.Ordinal);
        var read = code.IndexOf("PlatformEnvironment.Read", StringComparison.Ordinal);

        Assert.True(wired >= 0 && read >= 0, "both the provider and the platform read must be present");
        Assert.True(wired < read, "the provider is registered after the platform environment read");
    }
}
