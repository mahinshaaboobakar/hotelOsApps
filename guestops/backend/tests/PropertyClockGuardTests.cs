using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// No code takes a day off an instant in UTC, or turns a wall-clock time into an
/// instant at UTC — ADR 0174: every application works in the property's zone.
/// </summary>
/// <remarks>
/// <para>
/// <b>The two shapes, from the sweep of 2026-09-19</b> (the architect's call
/// shapes, no limit; Workforce's fix is the reference):
/// </para>
/// <list type="bullet">
/// <item><b>A day taken as the UTC day</b> — <c>DateOnly.FromDateTime(x.DateTime)</c>,
/// <c>x.Date</c>, <c>.UtcDateTime</c>, <c>DateTime.Now/Today/UtcNow</c>. An instant
/// read back from PostgreSQL is at offset zero, so its date is UTC's: a Kolkata
/// arrival after midnight lands the day before, a Guatemala arrival after 18:00
/// the day after. A day comes off an instant only through
/// <c>TimeZoneInfo.ConvertTime(…, zone)</c>.</item>
/// <item><b>A property wall-clock time treated as UTC</b> —
/// <c>new DateTimeOffset(…, TimeSpan.Zero)</c>. A drop time of 18:00 at the
/// property is not 18:00 UTC. A wall-clock time becomes an instant only through
/// the property's zone.</item>
/// </list>
/// <para>
/// Comments are records and are not read; code is.
/// </para>
/// </remarks>
public sealed class PropertyClockGuardTests
{
    private static readonly (string What, Regex Shape)[] Shapes =
    [
        ("a day taken off an instant without the property's zone",
            new(@"DateOnly\.FromDateTime\((?!TimeZoneInfo\.ConvertTime)|\.UtcDateTime\b|DateTime\.(Now|Today|UtcNow)\b")),
        ("a wall-clock time made an instant at UTC",
            new(@"TimeSpan\.Zero")),
    ];

    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "src"));

    private static string Code(string line)
    {
        var at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }

    private static IEnumerable<string> Offences(string file, IEnumerable<string> lines)
        => lines
            .Select((line, index) => (code: Code(line), index))
            .SelectMany(line => Shapes
                .Where(shape => shape.Shape.IsMatch(line.code))
                .Select(shape => $"{file}:{line.index + 1}: {shape.What}: {line.code.Trim()}"));

    [Fact]
    public void No_code_reads_the_day_or_the_wall_clock_in_UTC()
    {
        var files = Directory.EnumerateFiles(Source(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .ToList();
        Assert.True(files.Count > 50, $"walked {files.Count} files under {Source()}");

        var found = files
            .SelectMany(f => Offences(Path.GetRelativePath(Source(), f), File.ReadAllLines(f)))
            .ToList();

        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    [Fact]
    public void The_guard_refuses_each_shape_and_passes_the_zoned_forms()
    {
        Assert.NotEmpty(Offences("x", ["    public DateOnly? Date => At is { } at ? DateOnly.FromDateTime(at.DateTime) : null;"]));
        Assert.NotEmpty(Offences("x", ["        return new DateTimeOffset(deadline.ToDateTime(dropTime), TimeSpan.Zero);"]));
        Assert.NotEmpty(Offences("x", ["        var today = DateOnly.FromDateTime(DateTime.UtcNow);"]));

        Assert.Empty(Offences("x", ["        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, zone).DateTime);"]));
        Assert.Empty(Offences("x", ["        var now = clock.GetUtcNow();"]));
        Assert.Empty(Offences("x", ["        // It was built at TimeSpan.Zero until 2026-09-19."]));
    }
}
