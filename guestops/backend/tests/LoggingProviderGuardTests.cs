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
        Assert.Matches(new Regex(@"UseSerilog\s*\("), Code());
    }

    [Fact]
    public void The_console_sink_is_declared_before_configuration_is_read()
    {
        var code = Code();
        var console = code.IndexOf("WriteTo.Console", StringComparison.Ordinal);
        var read = code.IndexOf("ReadFrom.Configuration", StringComparison.Ordinal);

        Assert.True(console >= 0, "no console sink: the Kernel captures stdout and reads nothing else");
        Assert.True(
            read < 0 || console < read,
            "the console sink must precede ReadFrom.Configuration — appsettings.json "
            + "declares no Serilog section, so configuration alone contributes none");
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
        var wired = code.IndexOf("UseSerilog", StringComparison.Ordinal);
        var read = code.IndexOf("PlatformEnvironment.Read", StringComparison.Ordinal);

        Assert.True(wired >= 0 && read >= 0, "both the provider and the platform read must be present");
        Assert.True(wired < read, "the provider is registered after the platform environment read");
    }
}
