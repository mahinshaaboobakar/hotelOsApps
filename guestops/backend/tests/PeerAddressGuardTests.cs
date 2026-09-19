using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// GuestOps discovers its peers through the Kernel; it writes none of their
/// addresses down.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> On the owner's first run of 0.3.1 every Context call
/// was refused: the client dialled <c>configuration["Context:Endpoint"] ??
/// "https://127.0.0.1:20054"</c>, an "interim with an expiry" that outlived its
/// expiry, while development Context had moved to 25154. The same sweep found
/// the bus configured ahead of the Kernel's word. <i>A peer address this
/// application writes down is one it can fall back to.</i>
/// </para>
/// <para>
/// <b>It reads code, not comments, and that is deliberate.</b> The fix's own
/// comments quote the removed literal so the reversal can be checked; a guard
/// failing on them would forbid keeping the record, which is how a corrected
/// claim survives its correction. A comment describing what WAS is a record; a
/// literal in code is a live claim — the line is drawn by role.
/// </para>
/// <para>
/// <b>Two files are outside it, each for a stated reason.</b>
/// <c>Infrastructure/DesignTimeFactory.cs</c> is EF's design-time tooling and
/// never runs in the application. <c>appsettings.json</c>'s connection string
/// serves a checkout's <c>migrate</c>, and under a Kernel <c>Program.cs</c>
/// refuses to start without the Kernel's own — so it cannot be fallen back to.
/// </para>
/// </remarks>
public sealed class PeerAddressGuardTests
{
    /// <summary>What a written-down peer looks like in C#.</summary>
    /// <remarks>
    /// A scheme followed by a WRITTEN host — not by an interpolated service
    /// name. <c>new Uri($"https://{ContextName}")</c> carries the peer's name for
    /// <c>Host:</c> and TLS while the connect callback dials wherever the Kernel
    /// says (<c>PlatformEndpoint.Uri</c>'s own remark); the first draft of this
    /// guard flagged it, which was the rule reaching past what an address is.
    /// </remarks>
    private static readonly Regex Written = new(
        """https?://(?!\{)(?!www\.w3\.org)|nats://|127\.0\.0\.1|\blocalhost\b|Endpoint"\s*\]|Host=""",
        RegexOptions.Compiled);

    private static readonly string[] Outside = ["DesignTimeFactory.cs"];

    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "src"));

    /// <summary>The code of a C# file — line and block comments removed, lines kept.</summary>
    private static string[] Code(string text)
    {
        var withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", m => new string('\n', m.Value.Count(c => c == '\n')), RegexOptions.Singleline);
        return [.. withoutBlocks.Split('\n').Select(line =>
        {
            var at = line.IndexOf("//", StringComparison.Ordinal);

            // `https://` contains `//` too: only a `//` that is not part of a
            // scheme starts a comment.
            while (at > 0 && line[at - 1] == ':')
            {
                at = line.IndexOf("//", at + 2, StringComparison.Ordinal);
            }

            return at < 0 ? line : line[..at];
        })];
    }

    private static IEnumerable<string> Offences(string file, string text)
        => Code(text)
            .Select((line, index) => (line, index))
            .Where(pair => Written.IsMatch(pair.line))
            .Select(pair => $"{file}:{pair.index + 1}: {pair.line.Trim()}");

    [Fact]
    public void No_peer_address_is_written_in_the_application()
    {
        var files = Directory
            .EnumerateFiles(Source(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => !Outside.Contains(Path.GetFileName(f)))
            .ToList();

        // The population is asserted, so a walk that found nothing cannot pass.
        Assert.True(files.Count > 50, $"walked {files.Count} files under {Source()}");

        var offences = files
            .SelectMany(f => Offences(Path.GetRelativePath(Source(), f), File.ReadAllText(f)))
            .ToList();

        Assert.True(offences.Count == 0,
            "a peer address is written down rather than discovered:\n" + string.Join("\n", offences));
    }

    [Fact]
    public void The_guard_fails_on_the_line_it_replaced()
    {
        // The exact line from 0.3.1 — the positive control. A guard never shown
        // failing is a green with no provenance.
        const string Removed =
            """new Uri(configuration["Context:Endpoint"] ?? "https://127.0.0.1:20054"));""";

        Assert.NotEmpty(Offences("fixture", Removed));
        Assert.NotEmpty(Offences("fixture", """?? "nats://127.0.0.1:24222";"""));

        // A literal service host is still an address written down.
        Assert.NotEmpty(Offences("fixture", """client.Address = new Uri("https://context:25154");"""));

        // And it does not fire on a record of the removed line, nor on the
        // name-only URI discovery uses.
        Assert.Empty(Offences("fixture", """// it dialled "https://127.0.0.1:20054" until 2026-09-19"""));
        Assert.Empty(Offences("fixture", """client.Address = new Uri($"https://{ContextName}");"""));
    }
}
