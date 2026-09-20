using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// No view renders a date or a time — the screen does, for the property.
/// </summary>
/// <remarks>
/// <para>
/// <b>By call shape, not by name.</b> A fixed pattern reaches the wire three
/// ways in this code, and the guard reads all three: <c>ToString("d MMM")</c>,
/// an interpolation hole <c>{to:d MMM}</c>, and a bare component
/// <c>{from.Day}</c> composed into prose. What stays legal is the value: a
/// round-trip instant (<c>"O"</c>), an ISO day (<c>"yyyy-MM-dd"</c>), and a
/// Guid's <c>"N"</c>.
/// </para>
/// <para>
/// <b>Comments are not read</b>, because the comments that name these patterns
/// are the records of what each view used to send — a guard that forbade them
/// would forbid keeping the history. Code is what reaches the wire.
/// </para>
/// <para>
/// <b>The remainder is declared, and walked both ways.</b> The views were
/// converted one wire change at a time (2026-09-19); the ones not yet converted
/// are listed with the number of renderings each still holds. A listed file that
/// has changed count fails, as does an unlisted file with any — so the list
/// cannot quietly go stale in either direction, and it is empty when the work
/// is done.
/// </para>
/// </remarks>
public sealed class RenderedDateGuardTests
{
    /// <summary>Views still sending a rendering, and how many each holds.</summary>
    private static readonly Dictionary<string, int> NotYetConverted = new()
    {
        ["BookingView.cs"] = 7,
        ["BookingsView.cs"] = 2,
        ["CancelPlanView.cs"] = 6,
    };

    private static readonly Regex Rendered = new(
        """ToString\("(?!O"|N"|yyyy-MM-dd")[^"]*[dMHhmy][^"]*"|\{[^{}"]*:[dMHhmy][dMHhmy .,/:-]*\}|\{[^{}"]*\.(?:Day|Month|Year|Hour|Minute|DayOfWeek)\}""",
        RegexOptions.Compiled);

    private static string Module([System.Runtime.CompilerServices.CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "src", "Module"));

    /// <summary>The code of a file — block comments blanked, line comments cut.</summary>
    private static IEnumerable<string> Code(string text)
    {
        var withoutBlocks = Regex.Replace(
            text, @"/\*.*?\*/", m => new string('\n', m.Value.Count(c => c == '\n')), RegexOptions.Singleline);

        return withoutBlocks.Split('\n').Select(line =>
        {
            var at = line.IndexOf("//", StringComparison.Ordinal);
            return at < 0 ? line : line[..at];
        });
    }

    private static int Renderings(string text) => Code(text).Sum(line => Rendered.Matches(line).Count);

    [Fact]
    public void No_view_renders_a_date_except_those_declared_not_yet_converted()
    {
        var files = Directory.EnumerateFiles(Module(), "*.cs").ToList();

        // The population is asserted, so a walk that found nothing cannot pass.
        Assert.True(files.Count >= 20, $"walked {files.Count} files under {Module()}");

        var wrong = files
            .Select(f => (name: Path.GetFileName(f), found: Renderings(File.ReadAllText(f))))
            .Select(f => (f.name, f.found, declared: NotYetConverted.GetValueOrDefault(f.name)))
            .Where(f => f.found != f.declared)
            .Select(f => f.declared == 0
                ? $"{f.name}: {f.found} rendered date(s) — send the value; the screen formats it (page 64 §11)"
                : $"{f.name}: declared {f.declared}, found {f.found} — update NotYetConverted")
            .ToList();

        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void The_guard_fails_on_each_shape_it_replaced()
    {
        // Positive controls, one per shape, taken from the views as they were.
        Assert.Equal(2, Renderings("""date = one.OccurredAt.ToString("d MMM"), time = one.OccurredAt.ToString("HH:mm"),"""));
        Assert.Equal(2, Renderings("""? $"{count} · {from:d MMM} → {to:d MMM}" """));
        Assert.Equal(2, Renderings("""? $"{from.Day} – {to:d MMM} · " """));

        // And it passes the values, a ternary inside a hole, and a record.
        Assert.Equal(0, Renderings("""at = one.OccurredAt.ToString("O"), day = d.ToString("yyyy-MM-dd"), id.ToString("N")"""));
        Assert.Equal(0, Renderings("""$"{Spell(count)} {(count == 1 ? "stay" : "stays")}" """));
        Assert.Equal(0, Renderings("""// This sent ToString("d MMM") until 2026-09-19"""));
    }
}
