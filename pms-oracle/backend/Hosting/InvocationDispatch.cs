using HotelOS.Connector;

namespace PmsOracle.Hosting;

/// <summary>
/// Which invocation kind goes where — the handler the session calls.
/// </summary>
/// <remarks>
/// <para>
/// <b>The vocabulary is the platform's, read from
/// <see cref="ConnectorProtocolKinds"/>.</b> The strings are never written out
/// here: the one place both ends read them from is the SDK, and a package that
/// spelled <c>"test"</c> for itself would be a third spelling that agrees until
/// it does not.
/// </para>
/// <para>
/// <b>This file replaced a blanket refusal, and the reason is worth keeping.</b>
/// Until now every invocation was faulted with <i>"pms-oracle recognises no
/// invocation kinds yet… no invocation vocabulary between the Hub and a
/// connector has been ruled"</i>. That was true when written and stopped being
/// true on 2026-09-19, when ADR 0194 and the platform's <c>8b3a6189</c> landed
/// <see cref="ConnectorProtocolKinds"/> — in this package's own SDK reference —
/// and the Hub's dispatcher began sending <c>test</c> and <c>drain</c>. The
/// refusal went on explaining itself clearly to an operator about a world that
/// had moved, which is worse than an unexplained one: nobody looks behind a
/// reason that reads well.
/// </para>
/// <para>
/// <b>Two kinds are served and the rest are refused by name.</b> <c>test</c>
/// and <c>drain</c> both reach OHIP; anything else is unrecognised
/// (<see cref="InvocationRefusal"/>), which is a different fact from an
/// operation that failed. This paragraph read <i>"drain is understood and
/// refused with what is missing"</i> until the queue transport landed — kept as
/// the record, because the refusal it describes is what a reader would
/// otherwise still expect to find.
/// </para>
/// <para>
/// <b>Nothing here is exercised until a Connector Runtime exists.</b> This
/// process serves only a channel the Supervisor provides, and that Supervisor
/// is not built (<c>CONN-Q32a</c>). So what these tests prove is that the
/// dispatch answers each kind correctly when it is driven; that the Runtime
/// ever drives it is a different proof, on a tier no test here can reach.
/// </para>
/// </remarks>
/// <param name="http">The client the OHIP token attempt dials with.</param>
public sealed class InvocationDispatch(HttpClient http)
{
    /// <summary>The invocation kinds this connector answers.</summary>
    /// <remarks>
    /// <b>One source, walked both ways by a test.</b> This used to be typed
    /// out in the refusal's message, which then told an operator the connector
    /// served <c>test</c> and <c>drain</c> for days after <c>join</c> was
    /// added — a hand-kept list in the one sentence a confused operator reads.
    /// The test asserts every kind here is dispatched AND that every protocol
    /// kind absent from here is refused, so the list cannot drift from the
    /// switch in either direction.
    /// </remarks>
    public static readonly IReadOnlyList<string> ServedKinds =
    [
        ConnectorProtocolKinds.Test,
        ConnectorProtocolKinds.Drain,
        ConnectorProtocolKinds.Join,
        ConnectorProtocolKinds.DedupeKey,
    ];

    /// <summary>Serve one invocation.</summary>
    /// <param name="invocation">What the Hub asked for.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>The reply payload; a faulted task becomes a <c>Fault</c> frame.</returns>
    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
        ConnectorInvocation invocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        // ONE record per invocation, at the one chokepoint every kind passes
        // through. Instrumenting the four handlers instead would mean a call at
        // each of their return points, and a handler somebody adds later would
        // log nothing until they remembered to.
        //
        // **Four of Chapter 13's seven fields, and the other three ABSENT
        // rather than defaulted.** `ConnectorInvocation` carries
        // `CorrelationId`, `Kind` and `Payload` — so `integration`,
        // `rate_limit` and `authentication` cannot be established here, and
        // `rate_limit: false` would tell an operator the source did not
        // throttle an operation that may never have dialled it. They are the
        // handlers' to surface, and that is named rather than left looking
        // covered.
        //
        // **UNEXERCISED BY CONSTRUCTION, not by omission.** `pms-oracle` is
        // installed and does not run: nothing can tell the Connector Runtime
        // Supervisor to start a connector, so no session opens and this code
        // has never executed outside its own tests. The lines are correct and
        // waiting for a runtime, which is a different claim from working.
        var started = System.Diagnostics.Stopwatch.StartNew();
        var observed = new ConnectorObservations();

        try
        {
            var reply = await DispatchAsync(invocation, observed, cancellationToken);

            ConnectorLogging.Operation(
                integration: null,
                operation: invocation.Kind,
                resource: Resource(invocation, observed),
                duration: started.Elapsed,
                result: "served",
                rateLimited: observed.RateLimited,
                authentication: observed.Authentication);

            return reply;
        }
        catch (Exception failed)
        {
            // The type and not the message: a fault's text can carry a
            // property's own data, and this line goes to a file an operator
            // reads. The Fault frame still carries the detail to the Hub.
            ConnectorLogging.Operation(
                integration: null,
                operation: invocation.Kind,
                resource: Resource(invocation, observed),
                duration: started.Elapsed,
                result: $"faulted: {failed.GetType().Name}",
                rateLimited: observed.RateLimited,
                authentication: observed.Authentication);

            throw;
        }
    }

    /// <summary>What the operation acted on, or the correlation id.</summary>
    /// <remarks>
    /// `join` and `dedupe_key` act on bytes the Hub already holds, and this
    /// connector knows no name for them — so they fall back to the correlation
    /// id, which at least ties the line to the Hub's own record of the
    /// invocation. That is a weaker resource than a hotel code and it is named
    /// here rather than left looking like an oversight.
    /// </remarks>
    private static string Resource(
        ConnectorInvocation invocation, ConnectorObservations observed) =>
        observed.Resource ?? invocation.CorrelationId.ToString();

    private ValueTask<ReadOnlyMemory<byte>> DispatchAsync(
        ConnectorInvocation invocation,
        ConnectorObservations observed,
        CancellationToken cancellationToken)
    {
        return invocation.Kind switch
        {
            ConnectorProtocolKinds.Test =>
                ConnectionTestInvocation.ServeAsync(invocation, http, observed, cancellationToken),

            ConnectorProtocolKinds.Drain =>
                QueueDrainInvocation.ServeAsync(invocation, http, observed, cancellationToken),

            // `join` needs neither credentials nor the network: it reads one
            // payload and says whether it is half of a check-in.
            ConnectorProtocolKinds.Join =>
                PartJoinInvocation.ServeAsync(invocation, cancellationToken),

            // `dedupe_key` needs neither credentials nor the network either: it
            // keys bytes the Hub already holds. For a `delivery: push`
            // integration it is the ONLY way a payload is ever keyed, because
            // there is no drain to carry one and the Hub never computes one
            // (ADR 0255 §2) — which is why both on-site integrations declaring
            // it and nothing serving it made every pushed payload unkeyable.
            ConnectorProtocolKinds.DedupeKey =>
                PayloadKeyInvocation.ServeAsync(invocation, cancellationToken),

            // `credential.response` is the Hub answering a request this
            // connector made, and the session matches it to the invocation that
            // asked. It never arrives here as a request, and a handler that
            // accepted one would be answering its own question.
            _ => InvocationRefusal.HandleAsync(invocation, cancellationToken),
        };
    }
}
