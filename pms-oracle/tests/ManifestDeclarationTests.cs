using HotelOS.Contracts.Integration.V1;
using PmsOracle.Adapters;
using PmsOracle.Authentication;
using PmsOracle.Capabilities;
using PmsOracle.Hosting;
using PmsOracle.Normalisation;
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
    /// The manifest is the authority for these three, and the code agrees with
    /// it — ADR 0248 (d) for <c>delivery</c>, and the id closed set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The direction was settled before the vocabulary question arose.</b>
    /// <c>delivery</c> is a manifest field refused at admission when absent
    /// (<c>DeliveryUndeclared</c>), and the Hub <i>"may only materialize
    /// registrations for ids present in the verified signed manifest"</i>. So
    /// this asserts the code against the manifest, never the reverse — a guard
    /// written the other way round would let a renamed declaration drag the
    /// signed file after it.
    /// </para>
    /// <para>
    /// <b>This was held back for a reason that did not apply to it.</b> It
    /// waited on the payload-kind vocabulary question, and it asserts no
    /// payload kinds — ids, <c>accepts_push</c> and <c>delivery</c> only, all
    /// manifest-owned by prior ruling. The neighbouring question was open; this
    /// one never was.
    /// </para>
    /// <para>
    /// <b>What it replaces is a sentence.</b> The manifest said the block was
    /// <i>"derived from `PmsOracleCapabilities.cs`, not typed"</i> and that
    /// <i>"a script parses them and emits the block, with assertions that fail
    /// rather than guess if that file moves."</i> No such script existed in
    /// either repository, and the only guard here covered `required_secrets`
    /// against a different type — so the ids, `accepts_push` and `delivery`
    /// were hand-kept copies with nothing comparing them. This is the
    /// assertion that sentence promised.
    /// </para>
    /// </remarks>
    [Fact]
    public void the_manifest_and_the_code_declare_the_same_three_integrations()
    {
        IntegrationCapability[] declared =
            [PmsOracleCapabilities.Cloud, PmsOracleCapabilities.OnPremise, PmsOracleCapabilities.Web];

        foreach (var capability in declared)
        {
            var id = capability.IntegrationId;
            var push = capability.Delivery is ChangeDelivery.Push;

            Assert.Equal(push, AcceptsPushOf(id));

            // `polling` and `push` are the manifest's spellings of what
            // `ChangeDelivery` calls `PolledQueue` and `Push` — the wire says
            // INTEGRATION_DELIVERY_POLLING and a manifest is read by an
            // administrator. Two vocabularies for one fact is the thing ADR
            // 0264 forbids, so the mapping is asserted rather than assumed.
            Assert.Equal(push ? "push" : "polling", DeliveryOf(id));
        }
    }

    /// <summary>
    /// The payload kinds the code uses are the ones the manifest declares —
    /// ADR 0272 §1, narrowed by ADR 0274.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This direction, and only this one.</b> A payload kind is the
    /// connector's, declared in the signed declaration, and code <i>"may
    /// consume the identifiers and must not establish a competing
    /// authoritative list"</i>. So the constants are checked against the
    /// manifest; a guard written the other way round would let a renamed
    /// constant drag the signed file after it.
    /// </para>
    /// <para>
    /// <b>Not every vocabulary, and that is the narrowing.</b> ADR 0274 counts
    /// five with three owners: the platform's capability kinds, these payload
    /// kinds, the Hub's <c>capabilities</c>, and <c>StatusVocabulary</c> and
    /// <c>IdentifierKinds</c>, which are the external source's and
    /// observational. The last two are deliberately not asserted here — a list
    /// recording what a source has been seen to emit cannot live in a signed
    /// artefact without freezing it or invalidating the signature.
    /// </para>
    /// </remarks>
    [Fact]
    public void the_manifest_and_the_code_declare_the_same_payload_kinds()
    {
        // Compared as sets, ordered the same way on both sides: the manifest's
        // order is the author's and carries no meaning, so asserting it would
        // pin a fact nobody decided.
        string[] cloud =
        [
            OracleCloudAdapter.NotificationPayload,
            OracleCloudAdapter.ReservationPayload,
            OracleCloudAdapter.HousekeepingPayload,
            OracleCloudAdapter.GuaranteePayload,
        ];

        Assert.Equal(
            cloud.Order(StringComparer.Ordinal),
            ListOf("oracle-cloud", "payload_kinds").Order(StringComparer.Ordinal));

        string[] onSite = [OracleOnSiteAdapter.StayPayload, OracleOnSiteAdapter.RoomStatusPayload];

        foreach (var pushed in new[] { "oracle-onpremise", "oracle-web" })
        {
            Assert.Equal(
                onSite.Order(StringComparer.Ordinal),
                ListOf(pushed, "payload_kinds").Order(StringComparer.Ordinal));
        }
    }

    /// <summary>
    /// Every prerequisite the manifest declares is refused on the
    /// <c>unresolved</c> arm — ADR 0321, ADR 0327, <c>CONN-Q84</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Both directions, and the second is the one that catches drift.</b>
    /// Declared-and-not-refused would let a connector rediscover a prerequisite
    /// the Hub was required to gate — <i>"a generic escape hatch"</i>, which
    /// <c>NormalizationUnresolved</c>'s own comment forbids by name.
    /// Refused-and-not-declared is the opposite failure: the guard would swallow
    /// a genuine platform fact, and <c>prerequisite</c> is OPEN, so the next one
    /// ruled must reach the wire rather than meet a stale refusal here.
    /// </para>
    /// <para>
    /// <b>The declaration is the manifest's, not the code's.</b> The set in
    /// <c>NormalizeReply</c> is a copy — the compiler cannot read a signed YAML
    /// file — so this is the only thing that compares them, exactly as
    /// <see cref="the_manifest_declares_exactly_the_secrets_the_credentials_read"/>
    /// is for secrets.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_prerequisite_the_manifest_declares_is_refused_on_the_unresolved_arm()
    {
        string[] integrations = [DiallingIntegration, .. PushIntegrations];

        var declared = integrations
            .SelectMany(id => ListOf(id, "prerequisites"))
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(declared);

        foreach (var prerequisite in declared)
        {
            var refusal = Assert.Throws<NotSupportedException>(() => NormalizeReply.From(
                new NormalisationOutcome.Unresolved(prerequisite, "Amount")));

            Assert.Contains("ADR 0316", refusal.Message, StringComparison.Ordinal);
        }

        // The other direction: a platform fact the manifest does NOT declare
        // travels, so the guard cannot quietly grow into the allow-list this
        // arm was ruled not to be.
        var platformFact = NormalizeReply.From(
            new NormalisationOutcome.Unresolved("minor_unit_digits", "Amount"));

        Assert.Equal(NormalizeResult.OutcomeOneofCase.Unresolved, platformFact.OutcomeCase);
        Assert.DoesNotContain("minor_unit_digits", declared, StringComparer.Ordinal);
    }

    /// <summary>One inline-list key inside one integration's block.</summary>
    /// <param name="integrationId">Which integration to read.</param>
    /// <param name="key">The key, without its colon.</param>
    /// <returns>Its declared members, in declaration order.</returns>
    /// <remarks>
    /// <b>One reader for both lists.</b> This was <c>PayloadKindsOf</c> with the
    /// key written into it; a second inline list in the same block would have
    /// arrived as a second copy of the bracket parsing, which is the shape where
    /// two readers of one file drift in the half nobody reads.
    /// </remarks>
    private static IReadOnlyList<string> ListOf(string integrationId, string key)
    {
        var value = ScalarOf(integrationId, key);

        Assert.True(
            value.StartsWith('[') && value.EndsWith(']'),
            $"`{integrationId}` declares {key} in a form this guard cannot read: {value}");

        return [.. value[1..^1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
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

    /// <summary>Whether the manifest says this integration is posted to.</summary>
    /// <param name="integrationId">Which integration to read.</param>
    /// <returns>Its declared <c>accepts_push</c>.</returns>
    private static bool AcceptsPushOf(string integrationId) =>
        ScalarOf(integrationId, "accepts_push") == "true";

    /// <summary>How the manifest says this integration's changes arrive.</summary>
    /// <param name="integrationId">Which integration to read.</param>
    /// <returns>Its declared <c>delivery</c>, verbatim.</returns>
    private static string DeliveryOf(string integrationId) =>
        ScalarOf(integrationId, "delivery");

    /// <summary>One scalar key inside one integration's block.</summary>
    /// <param name="integrationId">Which integration to read.</param>
    /// <param name="key">The key, without its colon.</param>
    /// <returns>The value, trimmed.</returns>
    /// <remarks>
    /// <b>An absent key throws rather than answering.</b> Returning a default
    /// would make every assertion above pass the day somebody deleted the line
    /// — which is the shape this file exists to refuse, and the shape that let
    /// a claimed derivation go years without one.
    /// </remarks>
    private static string ScalarOf(string integrationId, string key)
    {
        var lines = File.ReadAllLines(ManifestPath());

        var start = Array.FindIndex(
            lines, line => line.TrimStart().StartsWith($"- id: {integrationId}", StringComparison.Ordinal));

        Assert.True(start >= 0, $"the manifest declares no integration `{integrationId}`");

        for (var i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimStart();

            if (line.StartsWith("- id:", StringComparison.Ordinal)
                || (lines[i].Length > 0 && !char.IsWhiteSpace(lines[i][0])))
            {
                break;
            }

            if (line.StartsWith($"{key}:", StringComparison.Ordinal))
            {
                return line[(key.Length + 1)..].Trim();
            }
        }

        Assert.Fail($"`{integrationId}` declares no `{key}` — ADR 0264 makes the manifest the authority, "
            + "so a missing declaration is a refusal and not a default");
        return string.Empty;
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
