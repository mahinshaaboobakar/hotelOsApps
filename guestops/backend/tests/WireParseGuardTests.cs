using System.Text.RegularExpressions;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// No wire value is parsed under the machine's culture.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thirteen call sites used <c>DateOnly.TryParse</c> and
/// <c>TimeOnly.TryParse</c>, and both parse under
/// <c>CultureInfo.CurrentCulture</c>.</b> What a value meant therefore depended
/// on the locale of the machine the service was running on: <c>03/04/2026</c>
/// is the third of April on one and the fourth of March on another, and nothing
/// downstream can say which it was.
/// </para>
/// <para>
/// <b>It was found by a test that expected a refusal.</b> A registration card
/// was sent <c>14/03/86</c> for a date of birth — a format this application's
/// contract does not have — and stored 14 March 1986. The card said the field
/// was filled; nobody had typed that date.
/// </para>
/// <para>
/// <b>By call shape, not by file.</b> A guard naming the nine files that held
/// the defect would stop checking the tenth somebody writes next week — the
/// population is *code that parses a day or a time*, and the only way to find
/// it is the call.
/// </para>
/// <para>
/// <b>There is no exemption, and there used to be one.</b> While the exact
/// parse lived in this service, the file implementing it had to be excluded
/// from its own rule. <see cref="Iso8601"/> is now the platform SDK's, so
/// nothing in this tree is allowed to parse a day or a time at all, and the
/// rule is a flat zero with nothing to argue about. *An exemption list is how a
/// guard dies* — this one lost its only entry by the implementation leaving the
/// tree, which is the best way for an exemption to go.
/// </para>
/// <para>
/// <b>THIS RULE IS DELIBERATELY STRICTER THAN THE ESTATE-WIDE ONE, AND THE TWO
/// MUST NOT BE HARMONISED.</b> <c>scripts/check_wire_parsing.py</c> in the
/// platform repository reports a parse that <i>does not name a culture</i>,
/// because a component that has not yet been read may hold a parse of something
/// a person typed, which can legitimately be cultural — that judgement belongs
/// to whoever owns the component. GuestOps has been read, every site converted,
/// and every remaining wire value goes through <see cref="Iso8601"/>; so here
/// the honest rule is *no such call at all*. **Do not relax this to match the
/// script, and do not tighten the script to match this**: they answer different
/// questions about differently-audited trees.
/// </para>
/// <para>
/// <b>Comments are not read.</b> The remarks that name these calls are the
/// record of what the code used to do, and a guard forbidding them would forbid
/// keeping the history — the same rule <c>RenderedDateGuardTests</c> keeps.
/// </para>
/// </remarks>
public sealed class WireParseGuardTests
{
    private static readonly Regex CultureParse = new(
        @"\b(?:DateOnly|TimeOnly|DateTime|DateTimeOffset)\.TryParse\s*\(",
        RegexOptions.Compiled);

    /// <summary>Nothing in this service parses a day or a time itself.</summary>
    [Fact]
    public void Nothing_parses_a_day_or_a_time_under_the_machines_culture()
    {
        var offenders = Sources()
            .Select(file => (file, hits: Code(File.ReadAllText(file))
                .Count(line => CultureParse.IsMatch(line))))
            .Where(found => found.hits > 0)
            .Select(found => $"{Path.GetFileName(found.file)} ({found.hits})")
            .ToArray();

        Assert.Empty(offenders);
    }

    /// <summary>
    /// The guard is looking at something.
    /// </summary>
    /// <remarks>
    /// <b>A walk that quietly stopped finding files would pass the check above
    /// on the shrinking set it still had.</b> The count is asserted as a floor
    /// rather than a number, because a number in a test is a second place the
    /// size of this application is written down.
    /// </remarks>
    [Fact]
    public void The_guard_reads_the_whole_service()
        => Assert.True(Sources().Count() > 50, "the source walk found almost nothing");

    /// <summary>
    /// The exact parse refuses what the culture-dependent one accepted.
    /// </summary>
    /// <remarks>
    /// <b>The positive control.</b> Without it, the guard above is equally
    /// consistent with a regex that matches nothing — and the first value here
    /// is the one that was silently accepted.
    /// </remarks>
    [Theory]
    [InlineData("14/03/86")]
    [InlineData("03/04/2026")]
    [InlineData("14 March 1986")]
    [InlineData("1986-3-14")]
    public void A_day_that_is_not_the_wires_form_is_not_a_day(string written)
        => Assert.Null(Iso8601.Day(written));

    [Theory]
    [InlineData("2026-09-23", 2026, 9, 23)]
    [InlineData("1986-03-14", 1986, 3, 14)]
    public void A_day_in_the_wires_form_is_read(string written, int year, int month, int day)
        => Assert.Equal(new DateOnly(year, month, day), Iso8601.Day(written));

    [Theory]
    [InlineData("18:00", 18, 0, 0)]
    [InlineData("18:00:30", 18, 0, 30)]
    public void A_time_in_the_wires_form_is_read(string written, int hour, int minute, int second)
        => Assert.Equal(new TimeOnly(hour, minute, second), Iso8601.Time(written));

    [Theory]
    [InlineData("6 PM")]
    [InlineData("18.00")]
    [InlineData("")]
    public void A_time_that_is_not_the_wires_form_is_not_a_time(string written)
        => Assert.Null(Iso8601.Time(written));

    /// <summary>Every source file of the service, wherever it lives.</summary>
    /// <remarks>
    /// Walked from the tree rather than listed, so a directory added later is
    /// covered on the day it appears. Build output is skipped: it holds
    /// generated proto code nobody writes.
    /// </remarks>
    private static IEnumerable<string> Sources(
        [System.Runtime.CompilerServices.CallerFilePath] string here = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "src"));

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
    }

    /// <summary>The code of a file — block comments blanked, line comments cut.</summary>
    private static IEnumerable<string> Code(string text)
    {
        var withoutBlocks = Regex.Replace(
            text,
            @"/\*.*?\*/",
            match => new string('\n', match.Value.Count(character => character == '\n')),
            RegexOptions.Singleline);

        return withoutBlocks.Split('\n').Select(line =>
        {
            var at = line.IndexOf("//", StringComparison.Ordinal);
            return at < 0 ? line : line[..at];
        });
    }
}
