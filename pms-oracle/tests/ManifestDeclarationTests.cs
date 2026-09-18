using PmsOracle.Authentication;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The manifest declares which secrets this connector needs; the code decides
/// which ones it reads. After ADR 0176 those are two lists, and this is the
/// only thing that compares them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why there are two at all.</b> <c>RequiredSecrets</c> used to be a
/// property on <c>ITestableConnection</c>, so the manifest's answer was
/// <i>derived</i> from <see cref="OhipCredentials.SecretNames"/> and could not
/// disagree with it. ADR 0176 moved the authority to the signed manifest,
/// because a connector naming its own secrets is the party being authorized
/// describing its own authorization — the running process is third-party code
/// and the manifest is signed.
/// </para>
/// <para>
/// That is the right boundary and it costs this: the declaration and the reader
/// are now hand-kept copies. The compiler cannot see the manifest, the Hub
/// cannot see the code, and a secret added to
/// <see cref="OhipCredentials"/> without the manifest line would fail at a
/// property as a 401 on the next poll with nobody watching.
/// </para>
/// <para>
/// So this is a guard on the agreement rather than a second copy of the list —
/// spelling the three names out here would be a tautology that passes by
/// agreeing with itself.
/// </para>
/// </remarks>
public class ManifestDeclarationTests
{
    /// <summary>The integration that dials OHIP, and the only one with secrets.</summary>
    private const string DiallingIntegration = "oracle-cloud";

    /// <summary>The two that are posted to, and read no secret at all.</summary>
    private static readonly string[] PushIntegrations =
        ["oracle-onpremise", "oracle-web"];

    [Fact]
    public void the_manifest_declares_exactly_the_secrets_the_credentials_read()
    {
        var declared = RequiredSecretsOf(DiallingIntegration);

        Assert.Equal(
            OhipCredentials.SecretNames.OrderBy(name => name, StringComparer.Ordinal),
            declared.OrderBy(name => name, StringComparer.Ordinal));
    }

    /// <summary>
    /// <b>The over-grant direction, which nothing else would catch.</b>
    /// </summary>
    /// <remarks>
    /// <c>OracleOnSiteAdapter</c> takes settings and no secrets — it is posted
    /// to, and authenticates its caller with the ingress shared secret, which
    /// <c>ingress_authentication:</c> declares on its own axis. Declaring OHIP's
    /// password-grant secrets here would have the Hub read three credentials
    /// for an integration that never dials out, and the Token Vault prefix is
    /// shared: the deleted property's own remark warned about the reverse
    /// direction, that an ingress shared secret "has no business reaching an
    /// outbound dial".
    /// <para>
    /// If a push integration one day does dial, this test fails and whoever
    /// changed it meets this reason — which is the point of asserting a
    /// deliberate limit rather than writing it in prose.
    /// </para>
    /// </remarks>
    [Fact]
    public void an_integration_that_is_posted_to_declares_no_secrets()
    {
        foreach (var integration in PushIntegrations)
        {
            Assert.Empty(RequiredSecretsOf(integration));
        }
    }

    /// <summary>
    /// The declared secret names for one integration, read from the manifest.
    /// </summary>
    /// <param name="integrationId">Which integration to read.</param>
    /// <returns>Its declared names; empty when it declares none.</returns>
    /// <remarks>
    /// <b>Absent and empty are different, and only one of them is an answer.</b>
    /// A missing manifest, a missing integration block or a malformed sequence
    /// all throw rather than returning nothing — otherwise the first test would
    /// pass the day somebody deleted the key, which is the failure this file
    /// exists to prevent.
    /// </remarks>
    private static IReadOnlyList<string> RequiredSecretsOf(string integrationId)
    {
        var lines = File.ReadAllLines(ManifestPath());

        var start = Array.FindIndex(
            lines, line => line.TrimStart().StartsWith($"- id: {integrationId}", StringComparison.Ordinal));

        Assert.True(start >= 0, $"the manifest declares no integration `{integrationId}`");

        for (var i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimStart();

            // The next integration, or the end of the block.
            if (line.StartsWith("- id:", StringComparison.Ordinal) || (lines[i].Length > 0 && !char.IsWhiteSpace(lines[i][0])))
            {
                return [];
            }

            if (!line.StartsWith("required_secrets:", StringComparison.Ordinal))
            {
                continue;
            }

            var value = line["required_secrets:".Length..].Trim();

            Assert.True(
                value.StartsWith('[') && value.EndsWith(']'),
                $"`{integrationId}` declares required_secrets in a form this guard cannot read: {value}");

            return [.. value[1..^1]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        return [];
    }

    /// <summary>The package manifest, found by walking up from the test assembly.</summary>
    /// <returns>Its full path.</returns>
    /// <remarks>
    /// Walked rather than a counted `..\..\..\..`, which is a claim about the
    /// build output's depth and goes stale the day a target framework or a
    /// configuration folder changes.
    /// </remarks>
    private static string ManifestPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "manifest.yaml");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"no manifest.yaml above {AppContext.BaseDirectory} — this guard cannot run, " +
            "and passing without it would assert the declaration matches when nothing read it");
    }
}
