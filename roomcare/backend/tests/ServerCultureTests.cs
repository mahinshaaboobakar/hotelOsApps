using System.Globalization;
using System.Text.RegularExpressions;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>
/// A day Room Care writes is the same day whatever culture the server runs under — ADR 0174 §NUM-Q4, the second
/// half.
/// </summary>
/// <remarks>
/// <para>
/// <b>The first half was the parse and it was closed in <c>f69bb9b</c></b>: a business date arriving on an event is
/// read with <c>Iso8601.Day</c>, which pins <see cref="CultureInfo.InvariantCulture"/>. <b>The write was not.</b>
/// <c>DateOnly.ToString("yyyy-MM-dd")</c> with no provider formats under
/// <see cref="CultureInfo.CurrentCulture"/>'s <i>calendar</i>, so on a server whose culture is not Gregorian the
/// same day leaves as a different year — and it leaves looking exactly like a day, into an event body other
/// applications read and into a view a person reads.
/// </para>
/// <para>
/// GG measured the axis on nine cultures, 2026-09-29:
/// </para>
/// <code>
/// ar-SA   UmAlQuraCalendar       a Gregorian 2026 is an 1448 on the wire
/// fa-IR   PersianCalendar        2647-11-24
/// th-TH   ThaiBuddhistCalendar   1483-09-03
/// </code>
/// <para>
/// <b>It is not a locale nicety.</b> HotelOS is sold into the GCC, so <c>ar-SA</c> is a realistic server culture
/// rather than a hypothetical one, and nothing downstream of a wrong day can disagree with it: the event store is
/// the record.
/// </para>
/// <para>
/// <b>Both halves are asserted here</b> — what the running service writes under three non-Gregorian cultures, and
/// the shape, over every source file, so the next one written is refused rather than measured later.
/// </para>
/// </remarks>
[Collection(RoomCareCollection.Name)]
public sealed class ServerCultureTests(RoomCareFixture fixture)
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src"));

    /// <summary>
    /// A day or a time written for the wire under whatever calendar the machine happens to have: a custom format
    /// holding <c>y</c>, <c>M</c>, <c>d</c>, <c>H</c>, <c>h</c>, <c>m</c> or <c>s</c> and no format provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>"o"</c> is not matched and does not need to be: the round-trip specifier is defined by ISO 8601 and is
    /// the same under every culture, which is why Room Care's thirty instant sites are already safe. A format that
    /// names a provider is not matched either — naming it is the remedy.
    /// </para>
    /// <para>
    /// <b>The interpolated form is refused too</b>, although the sweep found none of it: <c>$"{day:yyyy-MM-dd}"</c>
    /// is the same call with the same defect and no <c>ToString</c> to grep for, so a guard that named only one of
    /// the two would send the next writer to the other. <c>ClockShapeTests</c> refuses <c>{…:HH}</c> already, for
    /// the property's zone rather than for the machine's calendar — a different axis, the same shape.
    /// </para>
    /// </remarks>
    private static readonly Regex[] Unpinned =
    [
        new(@"\.ToString\(\s*""[^""]*[yMdHhms][^""]*""\s*\)"),
        new(@"\{[A-Za-z_][^}""]*:[^}""]*[yMdHhms][^}""]*\}"),
    ];

    private static IEnumerable<string> Hits(string relative, string text) =>
        from line in text.Split('\n').Select((body, i) => (Body: body, Number: i + 1))
        let code = line.Body.TrimStart()
        where !code.StartsWith("//", StringComparison.Ordinal)
        where Unpinned.Any(shape => shape.IsMatch(line.Body))
        select $"{relative}:{line.Number}";

    [Theory]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    [InlineData("ar-SA")]
    [InlineData("en-US")]
    public async Task The_day_on_a_task_announcement_is_the_property_day_whatever_culture_the_server_runs_under(string culture)
    {
        var was = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            var h = new RoomCareHarness(fixture);
            var room = h.House.Room("G01");
            await h.SeedStateAsync(room, s =>
            {
                s.Condition = Condition.Dirty; s.Occupancy = Occupancy.Vacant; s.StayStatuses = [StayStatus.CheckedOut];
                s.NextSoldAt = RoomCareHarness.Saturday(15, 0);
            });

            await h.Get<PrepareService>().PressAsync(h.As(Guid.CreateVersion7()), null, default);

            await using var db = h.Db();
            var task = db.Tasks.Single(t => t.PropertyId == h.PropertyId);
            var created = (await h.EventsAsync(task.Id)).Single(e => e.Type == EventTypes.TaskCreated);
            Assert.Equal("2026-09-05", created.Payload.GetProperty("operating_day").GetString());
        }
        finally
        {
            CultureInfo.CurrentCulture = was;
        }
    }

    [Fact]
    public void No_source_file_writes_a_day_or_a_time_under_the_machines_calendar()
    {
        var files = Directory.EnumerateFiles(Source, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .ToList();
        // The same floor ClockShapeTests measures on, and for the same reason: a guard that reads nothing passes.
        Assert.True(files.Count >= 80, $"the guard found {files.Count} source files under {Source}; it is not reading the backend");

        var found = files.SelectMany(f => Hits(Path.GetRelativePath(Source, f).Replace('\\', '/'), File.ReadAllText(f))).ToList();
        Assert.Empty(found);
    }

    [Fact]
    public void The_guard_refuses_each_shape_and_allows_the_three_that_are_right()
    {
        Assert.Equal(["Planted.cs:1", "Planted.cs:2", "Planted.cs:3"], Hits("Planted.cs", string.Join('\n',
            "OperatingDay = day.ToString(\"yyyy-MM-dd\"),",
            "Starts = window.Starts.ToString(\"HH:mm\"),",
            "var said = $\"due {task.OperatingDay:yyyy-MM-dd}\";",
            "At = instant.ToString(\"o\"),",
            "OperatingDay = day.ToString(\"yyyy-MM-dd\", CultureInfo.InvariantCulture),",
            "// OperatingDay = day.ToString(\"yyyy-MM-dd\"),")).ToList());
    }
}
