using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using PmsOracle.Authentication;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The package's two halves name the same things.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the guard for the one duplication this design accepts.</b> ADR
/// 0128 §7 makes the Hub's <c>settings</c> map opaque to it — a Hub that knew
/// these names would need a schema per connector, the "second programming
/// language" <c>CONN-Q9</c> refused. So the vocabulary lives twice, once per
/// language, and nothing but this test can notice when the two disagree.
/// </para>
/// <para>
/// <b>Derived from the source rather than restated.</b> The names are read out
/// of <c>ui/configuration.ts</c> as it actually ships, so a setting added to
/// the form and forgotten in the backend fails here on the day it is written.
/// A hand-copied list would be a third place to keep in step, and the one
/// nobody would remember.
/// </para>
/// <para>
/// The drift this exists to catch is not hypothetical: before <c>CONN-Q12</c>
/// both halves implemented a simplified OAuth consistently, and the gap read as
/// a form gap until somebody checked the backend.
/// </para>
/// </remarks>
public sealed class ConfigurationVocabularyTests
{
    /// <summary>Names declared in one exported array of the UI vocabulary.</summary>
    private static IReadOnlyList<string> DeclaredIn(string arrayName)
    {
        var source = File.ReadAllText(Path.Combine(
            Path.GetDirectoryName(TestFile())!, "..", "ui", "configuration.ts"));

        var block = Regex.Match(
            source,
            $@"export const {arrayName}: readonly \w+\[\] = \[(.*?)^\];",
            RegexOptions.Singleline | RegexOptions.Multiline);

        Assert.True(block.Success, $"{arrayName} is not declared as the UI vocabulary expects");

        return Regex.Matches(block.Groups[1].Value, @"name:\s*""([^""]+)""")
            .Select(match => match.Groups[1].Value)
            .ToArray();
    }

    private static string TestFile([CallerFilePath] string path = "") => path;

    [Fact]
    public void Every_credential_the_backend_reads_is_offered_by_the_form()
    {
        var offered = DeclaredIn("SETTINGS");

        // Not equality: the form also carries polling, which is configuration
        // rather than a credential and has no place in a credential set.
        foreach (var name in OhipCredentials.SettingNames)
        {
            Assert.Contains(name, offered);
        }
    }

    [Fact]
    public void The_secret_names_match_exactly_because_they_are_vault_paths()
    {
        // Exact, and in order. A secret's name is a path segment in the Token
        // Vault, so a form submitting `applicationKey` where the backend reads
        // `application-key` would write a credential nobody ever reads back —
        // and the failure would arrive as a 401 at poll time.
        Assert.Equal(OhipCredentials.SecretNames, DeclaredIn("SECRETS"));
    }

    [Fact]
    public void No_credential_is_offered_as_both_a_setting_and_a_secret()
    {
        // The vault split is the whole of frame 3's masked-versus-legible
        // drawing. A name on both sides would be stored twice with two
        // meanings, and `client-id` was on the wrong side of it until
        // `CONN-Q12`.
        Assert.Empty(DeclaredIn("SETTINGS").Intersect(DeclaredIn("SECRETS")));
    }

    [Fact]
    public void The_guard_can_still_find_what_it_reads()
    {
        // If the UI file is restructured so the regex matches nothing, every
        // assertion above passes vacuously. This is what makes them fail
        // instead — the derivation is checked, not just used.
        Assert.NotEmpty(DeclaredIn("SETTINGS"));
        Assert.NotEmpty(DeclaredIn("SECRETS"));
    }
}
