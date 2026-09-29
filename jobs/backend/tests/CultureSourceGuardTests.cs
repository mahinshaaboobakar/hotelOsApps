using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// Nothing crossing the wire is written or read under the server's culture —
/// ADR 0174 §<c>NUM-Q4</c>.
/// </summary>
/// <remarks>
/// <para>
/// The addendum's rule, quoted: <i>"Values crossing service/API/wire boundaries
/// MUST be parsed using the wire contract's invariant representation and MUST
/// NOT be interpreted using machine/user culture. Locale-dependent formatting
/// is permitted only at the reader-facing presentation boundary."</i> A
/// projection writing a day for the screen to read is not that boundary — the
/// screen is — so the rule reaches the <b>writing</b> as well as the reading,
/// and Jobs' exposure was on the side nobody had counted.
/// </para>
/// <para>
/// <b>Measured 2026-09-29</b>, running Jobs' own call shapes under six cultures
/// a server can be configured to:
/// </para>
/// <code>
/// culture   ToString("yyyy-MM-dd")   ToString("o")            Parse(iso, invariant)
/// en-GB     2026-09-20               2026-09-02T11:10:00…     2026-09-20
/// th-TH     2569-09-20               2026-09-02T11:10:00…     2026-09-20
/// ar-SA     1448-04-09               2026-09-02T11:10:00…     2026-09-20
/// fa-IR     1405-06-29               2026-09-02T11:10:00…     2026-09-20
/// </code>
/// <para>
/// So the three shapes are not one finding. <c>"o"</c> is <b>invariant by
/// specification and measured so</b> — Jobs' thirty round-trip sites were never
/// exposed, and this guard deliberately does not name them. The custom pattern
/// takes <c>CurrentCulture</c>'s <i>calendar</i>, which is how a scheduled day
/// left this service as <c>2569-09-20</c>. And a pinned parse is safe where an
/// unpinned one is not: <c>DateOnly.Parse("2026-09-20")</c> throws under ar-SA
/// and answers <b>1483</b>-09-20 under th-TH, which is the reading
/// <c>JobsGrpcService.ParseOptionalDate</c> already cites.
/// </para>
/// <para>
/// <b>Red before its green</b> (2026-09-29): four lines — <c>Views.Job</c>,
/// <c>BoardProjection.ScheduledAsync</c> and <c>JobProjection.PriorityAndTime</c>
/// writing a scheduled day, and <c>SettingsProjection</c>'s average rating,
/// which measured <c>4٫5</c> under ar-SA and <c>4,5</c> under de-DE.
/// </para>
/// <para>
/// <b>What it cannot see.</b> It reads lines, so a call split across two of them
/// passes; it reads <c>src</c> only, so a fixture is never named; and a value
/// interpolated into a sentence is <see cref="ZoneSourceGuardTests"/>'s fourth
/// shape rather than one of these.
/// </para>
/// </remarks>
public class CultureSourceGuardTests
{
    private static readonly (string What, Regex Shape)[] Shapes =
    [
        ("a day or a time written under the server's calendar",
            new Regex(@"\.ToString\(""[^""]*(?:yyyy|MM|dd|HH|hh|mm|ss)[^""]*""\s*\)")),
        ("a number written under the server's decimal mark",
            new Regex(@"\.ToString\(""[0#][^""]*""\s*\)")),
        ("a date or time read under the server's calendar",
            new Regex(@"(?:(?:DateOnly|TimeOnly|DateTime|DateTimeOffset)\.(?:Try)?Parse(?:Exact)?|Convert\.ToDateTime)\((?![^;]*Culture)")),
        ("a number read under the server's decimal mark",
            new Regex(@"(?:decimal|double|float)\.(?:Try)?Parse\((?![^;]*Culture)")),
    ];

    [Fact]
    public void Nothing_on_the_wire_is_written_or_read_under_the_servers_culture()
    {
        var found = new List<string>();
        foreach (var (file, lines) in SourceUnderTest.Files())
        {
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
    public void Every_shape_names_a_planted_line_and_leaves_the_invariant_ones()
    {
        var planted = new[]
        {
            @"ScheduledFor = j.ScheduledFor?.ToString(""yyyy-MM-dd"") ?? string.Empty,",
            @"new ModuleViews.DetailView(""Average"", average.ToString(""0.0""))",
            @"var day = DateOnly.Parse(body.Text(""on""));",
            @"var rate = decimal.Parse(body.Text(""rate""));",
        };
        Assert.Equal(Shapes.Length, planted.Length);
        for (var i = 0; i < Shapes.Length; i++)
        {
            Assert.True(Shapes[i].Shape.IsMatch(planted[i]), $"{Shapes[i].What} did not name its own planted line");
        }

        // The lines that are already right, and that a widened pattern would
        // start naming: the round-trip format, a pinned parse, an identifier,
        // and a count. Measured invariant under all six cultures above.
        var innocent = new[]
        {
            @"new(""Created"", job.CreatedAt.ToString(""o"")),",
            @"var day = DateOnly.Parse(value, CultureInfo.InvariantCulture);",
            @"var id = Guid.Parse(row.Id);",
            @"new(""Assignments"", rows.Assignments.Count.ToString()),",
        };
        foreach (var line in innocent)
        {
            foreach (var (what, shape) in Shapes)
            {
                Assert.False(shape.IsMatch(line), $"{what} named a line that is already invariant: {line}");
            }
        }
    }
}
