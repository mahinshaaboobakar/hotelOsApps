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
/// <b>Three answers, and they are three different facts.</b> A kind this
/// connector serves is served; <c>drain</c> is understood and refused with what
/// is missing (<see cref="QueueDrainInvocation"/>); anything else is
/// unrecognised (<see cref="InvocationRefusal"/>). Collapsing the last two
/// would tell an operator that this connector does not know what <c>drain</c>
/// means.
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
    /// <summary>Serve one invocation.</summary>
    /// <param name="invocation">What the Hub asked for.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>The reply payload; a faulted task becomes a <c>Fault</c> frame.</returns>
    public ValueTask<ReadOnlyMemory<byte>> HandleAsync(
        ConnectorInvocation invocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        return invocation.Kind switch
        {
            ConnectorProtocolKinds.Test =>
                ConnectionTestInvocation.ServeAsync(invocation, http, cancellationToken),

            ConnectorProtocolKinds.Drain =>
                QueueDrainInvocation.ServeAsync(invocation, cancellationToken),

            // `credential.response` is the Hub answering a request this
            // connector made, and the session matches it to the invocation that
            // asked. It never arrives here as a request, and a handler that
            // accepted one would be answering its own question.
            _ => InvocationRefusal.HandleAsync(invocation, cancellationToken),
        };
    }
}
