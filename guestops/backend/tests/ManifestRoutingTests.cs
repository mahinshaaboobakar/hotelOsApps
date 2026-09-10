using System.Text.RegularExpressions;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Every capability the manifest declares is reachable through some door.
/// </summary>
/// <remarks>
/// <para>
/// <b>The check that would have caught `desk.configure` before an operator
/// did.</b> The manifest declared it, the property approved it, the Shell
/// granted it, and <c>ModuleSurface</c> mapped three capabilities. The Setup
/// screen therefore called a route that answered <c>404</c> for as long as the
/// screen has existed, and the fallback hid it. Fixing the one route leaves the
/// class open; this closes the class.
/// </para>
/// <para>
/// <b>Derived from the manifest, never a list written beside it.</b> A list
/// would have to be kept in step with the thing it describes, and the first
/// capability added without touching it passes. The manifest is the declaration
/// and the routes are the obligation, so both sides are read from source and
/// neither is restated here.
/// </para>
/// <para>
/// <b>THIS CHECK READ ONE DOOR AND ITS MESSAGE CLAIMED ALL OF THEM.</b> The
/// first version walked <c>ModuleSurface</c> alone and failed with <i>"routed by
/// nothing"</i> — and five of the capabilities it named are reachable through
/// this application's gRPC door: <c>AssignRoom</c>, <c>CaptureRegistration</c>,
/// <c>GetRegistration</c>, <c>LogRequest</c>, <c>AddNote</c>. The message
/// travelled further than the check did, a removal was ruled on it, and removing
/// them would have made <c>AssignRoom</c> answer <c>403</c> to a PMS connector:
/// an over-grant argument producing an under-grant.
/// </para>
/// <para>
/// <b>So it reads both doors, and names which.</b> The gRPC handlers authorize
/// nothing themselves — the <i>services</i> they call do, through
/// <c>RequireAsync</c> — so the second door is read where its authorization
/// actually lives rather than where its methods are declared. A guard's failure
/// message is a claim about the world and may claim only what the guard
/// measured.
/// </para>
/// <para>
/// <b>Why the obligation is one method and not the right ones.</b> Whether a
/// capability's <i>methods</i> are complete is a question only the screens can
/// answer, and asserting a list of them here would be the written-list defect
/// one level down. A capability with no route at all is different in kind: it
/// cannot be right under any reading, and it shows its operator a developer's
/// sentence.
/// </para>
/// </remarks>
public sealed class ManifestRoutingTests
{
    /// <summary>Repository-relative, from the test assembly's own location.</summary>
    private static string PackageRoot
    {
        get
        {
            var here = new DirectoryInfo(AppContext.BaseDirectory);

            while (here is not null && !File.Exists(Path.Combine(here.FullName, "manifest.yaml")))
            {
                here = here.Parent;
            }

            return here?.FullName
                ?? throw new InvalidOperationException(
                    "no manifest.yaml above the test assembly — the package layout moved");
        }
    }

    /// <summary>The capabilities the manifest asks an administrator to approve.</summary>
    private static IReadOnlyList<string> Declared()
    {
        var manifest = File.ReadAllText(Path.Combine(PackageRoot, "manifest.yaml"));
        var permissions = Regex.Match(
            manifest,
            @"^permissions:\s*$(?<body>(?:\n(?:\s+.*)?)*)",
            RegexOptions.Multiline);

        Assert.True(permissions.Success, "manifest.yaml has no permissions block");

        return Regex.Matches(permissions.Groups["body"].Value, @"^\s*-\s*id:\s*(?<id>[\w.]+)",
                RegexOptions.Multiline)
            .Select(m => m.Groups["id"].Value)
            .ToList();
    }

    /// <summary>The capabilities either door can reach.</summary>
    /// <remarks>
    /// Two sources, because this application has two doors. The module surface
    /// routes what a screen calls; the application services carry the
    /// <c>RequireAsync</c> the gRPC surface authorizes through. A capability
    /// named in either is reachable by somebody.
    /// </remarks>
    private static IReadOnlyList<string> Reachable()
    {
        var sources = new List<string>
        {
            File.ReadAllText(
                Path.Combine(PackageRoot, "backend", "src", "Module", "ModuleSurface.cs")),
        };

        sources.AddRange(Directory.EnumerateFiles(
                Path.Combine(PackageRoot, "backend", "src", "Application"),
                "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith("Permissions.cs", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        var source = string.Join(Environment.NewLine, sources);

        var names = Regex.Matches(source, @"Permissions\.(?<name>\w+)")
            .Select(m => m.Groups["name"].Value)
            .Distinct()
            .ToList();

        var constants = File.ReadAllText(
            Path.Combine(PackageRoot, "backend", "src", "Application", "Abstractions",
                "Permissions.cs"));

        // The constant's VALUE, so a rename of either side cannot make this pass
        // by comparing two names that no longer mean the same capability.
        return names
            .Select(name => Regex.Match(
                constants, $@"const string {name}\s*=\s*""(?<id>[\w.]+)"""))
            .Where(m => m.Success)
            .Select(m => m.Groups["id"].Value)
            .ToList();
    }

    [Fact]
    public void The_manifest_declares_capabilities_so_the_walk_cannot_be_vacuous()
    {
        Assert.NotEmpty(Declared());
        Assert.NotEmpty(Reachable());
    }

    [Fact]
    public void Every_declared_capability_is_reachable()
    {
        var unreachable = Declared().Except(Reachable()).ToList();

        Assert.True(
            unreachable.Count == 0,
            "declared by the manifest and reached by neither door — no route in "
            + "ModuleSurface and no RequireAsync in any application service: "
            + string.Join(", ", unreachable)
            + ". A property approves these and nothing can exercise them.");
    }

    [Fact]
    public void Every_reachable_capability_is_declared()
    {
        // The other direction, and it is not symmetry for its own sake: a route
        // for a capability the manifest never asked for is a surface the property
        // did not approve, which the Kernel refuses at the door — so the route is
        // dead code that reads as a working feature.
        var undeclared = Reachable().Except(Declared()).ToList();

        Assert.True(
            undeclared.Count == 0,
            "routed but never declared: " + string.Join(", ", undeclared));
    }
}
