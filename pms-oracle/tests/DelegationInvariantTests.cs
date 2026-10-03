using System.Runtime.CompilerServices;

using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The connector delegates retry, queueing and offline storage to the Hub, and
/// this is what proves it rather than asserting it.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>CONN-Q93</c>, ADR 0369 Addendum 2, 2026-10-03.</b> Chapter 18's
/// certification dimensions 4 (<i>Retries</i>) and 8 (<i>Offline mode</i>) are
/// <b>required of this connector</b>, and each carries two obligations:
/// </para>
/// <list type="number">
///   <item>
///     prove, <b>through the Hub boundary</b>, that this connector behaves per
///     its declared contract while the Hub performs retry, backoff and its
///     durable/offline handling. <b>That half needs the installed package and
///     is not what this file does.</b>
///   </item>
///   <item>
///     prove this connector <b>implements no independent retry, queue or
///     offline mechanism</b>. <b>That is a property of the source, it needs no
///     installed package, and it is what this file asserts.</b>
///   </item>
/// </list>
/// <para>
/// The ruling is explicit that the first does not follow from the second:
/// proving the negative invariant <i>"shows correct DELEGATION and does not
/// prove the dimensions"</i>. So a pass here discharges <b>half</b> of two
/// rows, and the ledger says which half.
/// </para>
/// <para>
/// <b>Why a test and not a sentence.</b> ADR 0128 §5 already rules the
/// mechanism — <i>"Cache, durable inbox/offline queue, retry, deduplication and
/// checkpoints are Integration Hub platform facilities"</i> and <i>"A connector
/// never implements a queue."</i> A limit explained in prose is removed by the
/// next person who finds it inconvenient; a limit asserted in a test has to be
/// argued with.
/// </para>
/// <para>
/// <b>The population is DERIVED, never listed.</b> It is every
/// <c>.cs</c> under <c>backend/</c>, walked from this file's own compiled-in
/// path, so a file somebody adds next week is covered the day it is written
/// rather than the day somebody remembers this guard.
/// </para>
/// </remarks>
public sealed class DelegationInvariantTests
{
    /// <summary>
    /// Mechanisms a connector may not bring, and the reason each one is here.
    /// </summary>
    /// <remarks>
    /// Named families rather than one vague rule, because <i>"no retry"</i> is
    /// not checkable and <c>Task.Delay</c> is. <b>A retry needs somewhere to
    /// wait and a queue needs somewhere to put things</b>, so the primitives
    /// are the thing to forbid — an author cannot write a backoff loop without
    /// one of these.
    /// </remarks>
    private static readonly (string Token, string Why)[] Forbidden =
    [
        ("Polly", "a retry policy library — retry is the Hub's, ADR 0128 §5"),
        ("RetryPolicy", "a retry policy — the Hub's, ADR 0128 §5"),
        ("WaitAndRetry", "a backoff policy — the Hub's, ADR 0128 §5"),
        ("Task.Delay", "waiting between attempts is what a retry loop is made of"),
        ("Thread.Sleep", "as Task.Delay, and worse on a serving path"),
        ("PeriodicTimer", "scheduling is Temporal's and the Hub's, never a connector's"),
        ("new Timer", "as PeriodicTimer"),
        ("ConcurrentQueue", "\"A connector never implements a queue\" — ADR 0128 §5"),
        ("BlockingCollection", "a queue by another name"),
        ("Channel.Create", "a queue by another name"),
        ("BackgroundService", "a connector is invoked by the Hub; it does not run itself"),
        ("IHostedService", "as BackgroundService"),
        ("File.WriteAll", "an offline store needs persistence, and this is persistence"),
        ("File.AppendAll", "as File.WriteAll"),
        ("StreamWriter", "as File.WriteAll"),
        ("SqliteConnection", "a local database is an offline queue with a schema"),
        ("DbContext", "the connector owns no database — ADR 0128 §5, and it declares db_connections: 0"),
    ];

    /// <summary>
    /// The one loop in the backend, exempted by name because it is not a retry.
    /// </summary>
    /// <remarks>
    /// <c>OhipBusinessEventQueue</c>'s drain is a <b>paging</b> loop whose
    /// terminator is OHIP's own answer — a 204, a short page, a throw on any
    /// other status, and the invocation's cancellation token. <b>It retries
    /// nothing</b>: a transport failure ends it rather than re-attempting.
    /// <para>
    /// Asserted rather than commented, so that renaming the file cannot quietly
    /// drop the one exemption out of the population and leave this guard
    /// passing over a loop nobody looked at.
    /// </para>
    /// </remarks>
    private const string PagingDrain = "OhipBusinessEventQueue.cs";

    [Fact]
    public void The_backend_brings_no_retry_no_queue_and_no_offline_store()
    {
        var files = BackendSources();

        // A guard's own denominator needs a guard. A walk that quietly stopped
        // finding things would pass every assertion below on the shrinking set
        // it still had.
        Assert.True(
            files.Count >= 40,
            $"only {files.Count} backend sources found — this guard's population "
            + "collapsed, and a pass would mean nothing");

        // And the positive control: a token this backend certainly contains. A
        // zero from the sweep below is evidence about the connector only once
        // the same instrument has found something it should.
        Assert.Contains(files, f => Code(f.Text).Contains("HttpClient", StringComparison.Ordinal));

        var found = new List<string>();

        foreach (var (path, text) in files)
        {
            var code = Code(text);

            foreach (var (token, why) in Forbidden)
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    found.Add($"{Path.GetFileName(path)} brings `{token}` — {why}");
                }
            }
        }

        Assert.Empty(found);
    }

    [Fact]
    public void The_only_loop_in_the_backend_is_the_paging_drain()
    {
        var looping = BackendSources()
            .Where(f => Code(f.Text).Contains("while (", StringComparison.Ordinal)
                        || Code(f.Text).Contains("for (", StringComparison.Ordinal)
                        || Code(f.Text).Contains("do\n", StringComparison.Ordinal))
            .Select(f => Path.GetFileName(f.Path))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // The exemption is asserted, not described: if the drain is renamed this
        // fails rather than silently covering a different file.
        Assert.Equal([PagingDrain], looping);
    }

    /// <summary>Every backend source, derived from this file's own location.</summary>
    /// <remarks>
    /// Refuses rather than returning an empty set, in `ManifestDeclarationTests`'
    /// own form: passing with nothing read would assert the connector is clean
    /// when nothing looked at it.
    /// </remarks>
    private static List<(string Path, string Text)> BackendSources()
    {
        var backend = Path.Combine(Path.GetDirectoryName(TestFile())!, "..", "backend");

        if (!Directory.Exists(backend))
        {
            throw new DirectoryNotFoundException(
                $"no backend/ beside {TestFile()} — this guard cannot run, and passing "
                + "without it would assert the connector delegates when nothing read it");
        }

        return [.. Directory
            .EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (path, File.ReadAllText(path)))];
    }

    /// <summary>The source with whole-line comments removed.</summary>
    /// <remarks>
    /// <b>Measured, not precautionary.</b> A sweep for <c>for (</c> over this
    /// backend returns four files and only one holds a loop: the other three
    /// match doc comments reading <i>"answered 202 for (ADR 0272 §3)"</i>.
    /// Prose about a mechanism carries the mechanism's tokens, so an instrument
    /// reading text meets the explanation before the instance.
    /// <para>
    /// <b>Only WHOLE-LINE comments are dropped</b>, which is the role
    /// distinction rather than a blanket exemption: a line that is entirely
    /// commentary is a <i>record</i> and must survive, while a trailing comment
    /// on a code line is <b>not</b> exempted — so a forbidden token in one
    /// fails this guard and gets reworded. That false positive is the price of
    /// a guard a comment cannot defeat.
    /// </para>
    /// <para>
    /// No string-literal parsing, deliberately: a stripper that tracked quotes
    /// could eat code after a <c>//</c> inside a URL, and a guard that
    /// sometimes removes code is worse than one that sometimes reads a comment.
    /// </para>
    /// </remarks>
    private static string Code(string text) =>
        string.Join(
            '\n',
            text.Split('\n')
                .Where(line =>
                {
                    var trimmed = line.TrimStart();

                    return !trimmed.StartsWith("//", StringComparison.Ordinal)
                           && !trimmed.StartsWith("/*", StringComparison.Ordinal)
                           && !trimmed.StartsWith('*');
                }));

    private static string TestFile([CallerFilePath] string path = "") => path;
}
