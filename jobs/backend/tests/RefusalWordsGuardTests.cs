using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// A refusal a person reads says it in plain words: no wire field name, no
/// document citation, no system's name (owner, 2026-09-19).
/// </summary>
/// <remarks>
/// An <c>InvalidRequestException</c> travels as the <c>invalid</c> kind, which the
/// SDK marks <c>isForPeople</c>, and the screen shows its message as written
/// (<c>act()</c> → <c>done.refused</c>). So each message is screen text whenever
/// it fires. <b>Red before its green</b> (2026-09-19): 27 messages naming a wire
/// field — <c>location_id is not a place at this property</c> and its kind.
/// </remarks>
public class RefusalWordsGuardTests
{
    private static readonly Regex Thrown = new(@"new InvalidRequestException\(\s*\$?""((?:[^""\\]|\\.)*)""");

    private static readonly (string What, Regex Shape)[] Shapes =
    [
        ("a wire field's name", new Regex(@"\b[a-z]+(?:_[a-z]+)+\b")),
        ("a document citation", new Regex(@"§|\(S\d+|\bADR\b|\b[A-Z]+-Q\d+")),
        ("a system's name", new Regex(@"Master Data|Kernel|OpenFGA|Temporal")),
        // KK's additions from Room Care (d540eb9b's round, 2026-09-19): what a
        // literal-only reading could not see.
        ("a wire token", new Regex(@"\b[A-Z]{2,}(?:_[A-Z]+)+\b|\b(?:RAISED|SCHEDULED|ASSIGNED|ACCEPTED|RESOLVED|CLOSED|CANCELLED|REOPENED)\b")),
        ("a capability's id", new Regex(@"\bjob\.[a-z]+\b")),
        ("a hole that echoes the request or prints a raw value",
            new Regex(@"\{(?!job\.JobNumber\}|item\.Name\}|resolution\.Name\}|code\}|Said\.)[^}]+\}")),
    ];

    [Fact]
    public void Every_refusal_a_person_reads_is_in_plain_words()
    {
        var found = new List<string>();
        foreach (var file in Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories))
        {
            // Grpc/ answers other services over the wire, not a person on a screen.
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}Grpc{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            foreach (Match thrown in Thrown.Matches(File.ReadAllText(file)))
            {
                var words = thrown.Groups[1].Value;
                foreach (var (what, shape) in Shapes)
                {
                    if (shape.Match(words) is { Success: true } hit) found.Add($"{Path.GetFileName(file)} — {what} ({hit.Value}): \"{words}\"");
                }
            }
        }

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
    }

    [Fact]
    public void The_guard_reads_a_thrown_message_and_every_shape_names_its_own()
    {
        var sample = "throw new InvalidRequestException(\"location_id is not a place at this property\");";
        Assert.Equal("location_id is not a place at this property", Thrown.Match(sample).Groups[1].Value);

        // One planted example per shape (KK, 2026-09-20): five of these six had no
        // control, and a pattern with no control can lose its boundaries and match
        // nothing without a single test going red.
        var planted = new[]
        {
            "location_id is not a place at this property",
            "a step cannot have steps — one level only (S1 D2)",
            "the property has no code in Master Data",
            "job MRN-ENG-1 is IN_PROGRESS and cannot be held",
            "job.read has no method",
            "{request.Method} is not a method",
        };
        Assert.Equal(Shapes.Length, planted.Length);
        for (var i = 0; i < Shapes.Length; i++)
        {
            Assert.True(Shapes[i].Shape.IsMatch(planted[i]), $"{Shapes[i].What} did not name its own planted message");
        }

        // A sentence every shape must leave alone — plain words, a job number, a hole through Said.
        const string innocent = "job MRN-ENG-142 is {Said.Status(job.JobStatus)} and can't be held";
        foreach (var (what, shape) in Shapes)
        {
            Assert.False(shape.IsMatch(innocent), $"{what} named a refusal that is already in plain words");
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
