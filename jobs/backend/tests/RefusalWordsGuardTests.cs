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
    ];

    [Fact]
    public void Every_refusal_a_person_reads_is_in_plain_words()
    {
        var found = new List<string>();
        foreach (var file in Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
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
    public void The_guard_reads_a_thrown_message()
    {
        var sample = "throw new InvalidRequestException(\"location_id is not a place at this property\");";
        Assert.Equal("location_id is not a place at this property", Thrown.Match(sample).Groups[1].Value);
        Assert.Matches(Shapes[0].Shape, "location_id");
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
