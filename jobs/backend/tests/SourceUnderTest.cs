namespace HotelOS.Jobs.Tests;

/// <summary>The files a source guard reads — Jobs' own, without what a build wrote.</summary>
/// <remarks>
/// <para>
/// Two guards now read the same tree on different axes — the property's zone
/// (<see cref="ZoneSourceGuardTests"/>) and the server's culture
/// (<see cref="CultureSourceGuardTests"/>) — and both must skip the same
/// directories. A second copy of that list is how one guard quietly starts
/// reading <c>obj</c> and reporting the compiler's generated files, or stops
/// reading a directory the other still covers and goes green without ever
/// saying it narrowed.
/// </para>
/// <para>
/// <b>Migrations are exempt by the same reasoning both guards already used:</b>
/// an applied migration is immutable, so a finding in one cannot be fixed.
/// </para>
/// </remarks>
internal static class SourceUnderTest
{
    /// <summary>Every hand-written file under <c>jobs/backend/src</c>, with its lines.</summary>
    internal static IEnumerable<(string File, string[] Lines)> Files()
    {
        foreach (var file in Directory.EnumerateFiles(Root(), "*.cs", SearchOption.AllDirectories))
        {
            var sep = Path.DirectorySeparatorChar;
            if (file.Contains($"{sep}bin{sep}") || file.Contains($"{sep}obj{sep}") || file.Contains($"{sep}Migrations{sep}"))
            {
                continue;
            }

            yield return (file, File.ReadAllLines(file));
        }
    }

    /// <summary><c>jobs/backend/src</c>, found from the test binaries rather than from a relative path.</summary>
    internal static string Root()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var src = Path.Combine(dir.FullName, "src");
            if (File.Exists(Path.Combine(src, "HotelOS.Jobs.csproj"))) return src;
        }

        throw new InvalidOperationException("jobs/backend/src was not found above the test binaries");
    }
}
