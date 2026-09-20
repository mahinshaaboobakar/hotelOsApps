using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// No day and no wall-clock time is computed in UTC — the property's zone governs
/// (ADR 0174; the owner, 2026-09-19: "every app works in the property's
/// timezone").
/// </summary>
/// <remarks>
/// <para>
/// Reads Jobs' own source, because the two shapes are call shapes and a test of
/// one site proves nothing about the next one written. GG's Workforce fix
/// (<c>b5c5ffc</c>, <c>PropertyCalendar</c>) is the reference: "today" is the
/// property's calendar day, and a wall-clock time becomes an instant only
/// through the property's zone, never <c>TimeSpan.Zero</c>.
/// </para>
/// <para>
/// <b>Red before its green</b> (2026-09-19): five lines — AssignmentService
/// (the AUTO pick's day), DayStart (the no-zone fallback), PresenceService
/// (service hours against the UTC time of day), JobService (a scheduled day's
/// midnight) and WidgetProjection (the no-zone midnight).
/// </para>
/// </remarks>
public class ZoneSourceGuardTests
{
    private static readonly (string What, Regex Shape)[] Shapes =
    [
        ("a day or a time of day taken from the UTC clock", new Regex(@"(DateOnly|TimeOnly)\.FromDateTime\([^;]*\.UtcDateTime")),
        ("a day taken from the UTC clock", new Regex(@"\.UtcDateTime\.Date\b")),
        ("a wall-clock time made an instant at offset zero", new Regex(@"new DateTimeOffset\([^;]*TimeSpan\.Zero\)")),
        // KK's finding (Room Care, 2026-09-19): a time formatted into a sentence on
        // the server is fixed to one form before any property reads it.
        ("a date or time formatted into words on the server", new Regex(@"\{[^}]*:\s*(?:yyyy|HH|dd)[^}]*\}")),
    ];

    [Fact]
    public void No_day_or_wall_clock_time_is_computed_in_UTC()
    {
        var found = new List<string>();
        foreach (var file in Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                foreach (var (what, shape) in Shapes)
                {
                    if (shape.IsMatch(code)) found.Add($"{Path.GetFileName(file)}:{i + 1} — {what}");
                }
            }
        }

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
    }

    [Fact]
    public void Every_shape_names_a_planted_line_and_leaves_an_innocent_one()
    {
        // A positive control for EACH shape, not for two of four (KK's finding,
        // 2026-09-20: a pattern with no control is a pattern that can lose its
        // \b and match nothing for the rest of its life — both of us shipped one).
        var planted = new[]
        {
            "var day = DateOnly.FromDateTime(now.UtcDateTime);",
            "var since = now.UtcDateTime.Date;",
            "new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)",
            "$\"hold date {until:yyyy-MM-dd} passed\"",
        };
        Assert.Equal(Shapes.Length, planted.Length);
        for (var i = 0; i < Shapes.Length; i++)
        {
            Assert.True(Shapes[i].Shape.IsMatch(planted[i]), $"{Shapes[i].What} did not name its own planted line");
        }

        // And an innocent line none of them may name: a stored instant, in UTC by contract.
        const string innocent = "var now = clock.GetUtcNow();";
        foreach (var (what, shape) in Shapes)
        {
            Assert.False(shape.IsMatch(innocent), $"{what} named a line that is correct as UTC");
        }
    }

    private static string SourceRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var src = Path.Combine(dir.FullName, "src");
            if (File.Exists(Path.Combine(src, "HotelOS.Jobs.csproj"))) return src;
        }

        throw new InvalidOperationException("jobs/backend/src was not found above the test binaries");
    }
}
