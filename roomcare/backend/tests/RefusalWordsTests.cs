using System.Text.RegularExpressions;
using HotelOS.RoomCare.Application.Standard;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>
/// A refusal's words are for a person at the property. An <c>InvalidRequestException</c> travels as the SDK's
/// <c>invalid</c> kind, which is <c>isForPeople</c>, and the screen shows its message as written (ADR 0041). So its
/// words follow the owner's developer-content ruling (2026-09-19): no wire field, enum token, capability id, citation
/// or system name, and no time in UTC. HH's Jobs guard (RefusalWordsGuardTests) is the shape; this one also reads the
/// holes an interpolated message fills at run time and the enum words a person would read as code.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read from source, every message whole.</b> An expression can span lines and join pieces, so it is read to its
/// closing parenthesis. The population is derived: every file under <c>backend/src</c> but <c>bin</c>, <c>obj</c> and
/// the generated migrations.
/// </para>
/// <para>
/// <b>Not read here, and why.</b> A <c>PermissionDeniedException</c> crosses as the envelope's fixed "permission
/// denied", never its message. The SDK's own <c>NotFoundException</c> and <c>ConcurrencyException</c> build their
/// words themselves, <c>"{kind} {id} …"</c>, a type name and a raw id. They are the platform's to reword, and are
/// reported, not guarded here.
/// </para>
/// </remarks>
public sealed class RefusalWordsTests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src"));

    private static readonly (string What, Regex Shape)[] Shapes =
    [
        ("a wire field or table name", new(@"\b[a-z]+(?:_[a-z]+)+\b")),
        ("a wire field in camelCase", new(@"\b[a-z]+[A-Z][A-Za-z]*\b")),
        ("an enum token", new(@"\b[A-Z]{2,}_[A-Z_]+\b|\b[A-Z]{4,}\b")),
        ("a time said in UTC", new(@"\bUTC\b")),
        ("a capability id", new(@"\b(?:roomcare|room)\.[a-z]+\b")),
        ("a document citation", new(@"§|\(S\d|\bADR\b|\b[A-Z]+-Q\d+")),
        ("a system's name", new(@"Master Data|Kernel|OpenFGA|Context Service|Integration Hub")),
        ("a request's own name filled in", new(@"\{(?:name|request\.Method)\}")),
        ("a wire value quoted back", new(@"'\{[^}]*\}'")),
        ("a format pattern", new(@"HH:mm|yyyy-MM-dd|ISO 8601")),
    ];

    /// <summary>Every <c>new InvalidRequestException(…)</c> in a source text, whole, with its line.</summary>
    private static IEnumerable<(int Line, string Message)> Messages(string text)
    {
        foreach (Match m in Regex.Matches(text, @"new InvalidRequestException\("))
        {
            int depth = 1, j = m.Index + m.Length;
            var inString = false;
            for (; j < text.Length && depth > 0; j++)
            {
                var c = text[j];
                if (inString)
                {
                    if (c == '\\') j++;
                    else if (c == '"') inString = false;
                }
                else if (c == '"') inString = true;
                else if (c == '(') depth++;
                else if (c == ')') depth--;
            }

            yield return (text[..m.Index].Count(ch => ch == '\n') + 1, Regex.Replace(text[(m.Index + m.Length)..(j - 1)], @"\s+", " "));
        }
    }

    /// <summary>What a person would read that they should not: the literal words, and the holes an interpolation fills.</summary>
    private static IEnumerable<string> Faults(string message)
    {
        // The words: every string literal's text, with its interpolation holes kept, since a hole names what fills it.
        var words = string.Join(" ", Regex.Matches(message, "\"((?:[^\"\\\\]|\\\\.)*)\"").Select(x => x.Groups[1].Value));
        // A hole's format or expression is code, not words — except the two that fill in the request's own names.
        // A hole in quotes is kept too: it quotes the request's value back, a wire token such as 'IN_HOUSE'.
        var spoken = Regex.Replace(words, @"(?<!')\{(?!name\}|request\.Method\})[^}]*\}(?!')", " ");
        return Shapes.Where(s => s.Shape.IsMatch(spoken)).Select(s => $"{s.What}: {s.Shape.Match(spoken).Value}");
    }

    [Fact]
    public void Every_refusal_a_person_reads_is_in_their_words()
    {
        var files = Directory.EnumerateFiles(Source, "*.cs", SearchOption.AllDirectories)
            .Where(f => !Regex.IsMatch(f, @"[\\/](bin|obj|Migrations)[\\/]")).ToList();
        Assert.True(files.Count >= 80, $"read {files.Count} files under {Source}; the guard is not reading the backend");

        var messages = files.SelectMany(f => Messages(File.ReadAllText(f))
            .Select(m => (Where: $"{Path.GetRelativePath(Source, f).Replace('\\', '/')}:{m.Line}", m.Message))).ToList();
        Assert.True(messages.Count >= 60, $"found {messages.Count} refusals; the extraction is not finding them");

        var found = messages.SelectMany(m => Faults(m.Message).Select(fault => $"{m.Where} — {fault}")).ToList();
        found.AddRange(Faults($"\"{StandardService.NoInspectionApplication}\"").Select(fault => $"StandardService.NoInspectionApplication — {fault}"));
        Assert.True(found.Count == 0, $"{found.Count} refusals a person would read as code:\n{string.Join("\n", found)}");
    }

    [Fact]
    public void The_guard_sees_each_shape_it_refuses()
    {
        string[] planted =
        [
            "new InvalidRequestException(\"location_id is not a place\")",
            "new InvalidRequestException(\"notBefore is required\")",
            "new InvalidRequestException(\"a supervisor decides DND_APPROVED or CLEAN\")",
            "new InvalidRequestException($\"not before {at:HH:mm} UTC\")",
            "new InvalidRequestException($\"roomcare.assign has no method\")",
            "new InvalidRequestException(\"see ADR 0041\")",
            "new InvalidRequestException(\"that zone is not in Master Data\")",
            "new InvalidRequestException(\n    $\"{name} must be an id\")",
            "new InvalidRequestException($\"'{stay}' is not a stay\")",
            "new InvalidRequestException(\"a time must be HH:mm\")",
        ];
        var seen = planted.SelectMany(p => Messages(p)).SelectMany(m => Faults(m.Message)).Select(f => f.Split(": ")[0]).Distinct().ToList();
        Assert.Equal(Shapes.Select(s => s.What), seen);
    }
}
