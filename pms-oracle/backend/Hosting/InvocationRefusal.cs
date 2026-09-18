using HotelOS.Connector;

namespace PmsOracle.Hosting;

/// <summary>
/// What this connector answers to an invocation today: a refusal that says why.
/// </summary>
/// <remarks>
/// <para>
/// <b>No invocation kind is ruled, so none is dispatched.</b> The session hands
/// this handler a <see cref="ConnectorInvocation"/> carrying a
/// <see cref="ConnectorInvocation.Kind"/> string — and no vocabulary of those
/// strings exists anywhere: not in <c>HotelOS.Connector</c>, not in the Hub, not
/// on the Runtime side. Mapping <c>"test"</c> to <c>TestAsync</c> here would make
/// this package the author of the Hub ↔ connector contract, which is the same
/// fault as serialising a <c>CredentialRequest</c> ahead of its ruling:
/// whatever a third-party package picks first becomes what everyone else has to
/// match.
/// </para>
/// <para>
/// <b>A refusal, not a silence.</b> The session turns a failed handler into a
/// <c>Fault</c> frame carrying the exception's message, so the Hub learns the
/// kind it sent and why it went unserved — rather than a reply that looks like
/// an answer, or a hang that looks like a slow OHIP.
/// </para>
/// <para>
/// <b>This is where <see cref="ConnectorInvocation.RequestAsync"/> will be
/// reached from</b>, and it is not reached yet for two separate reasons: there is
/// no ruled invocation to make a credential request inside (<c>CONN-Q34</c>,
/// open), and the message itself does not exist yet. ADR 0189 ruled the encoding
/// — <c>CredentialRequest</c> is a Protobuf message in <c>shared/protos</c>,
/// opaque to the Runtime, and it carries <b>no id of any kind</b>: the session
/// stamps the frame, so a connector has no say over which invocation its request
/// belongs to. The proto is not written yet. <c>OhipPasswordGrant</c> serialises
/// that message once it lands, and waits on both.
/// </para>
/// <para>
/// <b>Named here, not referenced.</b> Until 2026-09-18 this read
/// <c>&lt;see cref="CredentialRequest"/&gt;</c> against a C# record in
/// <c>HotelOS.Connector</c>, and said the encoding was open. ADR 0189 removed
/// the record (<c>08d2d737</c>) and the cref stopped resolving — which under
/// <c>TreatWarningsAsErrors</c> made this package fail to build, from a commit in
/// another repository that nothing here could see. A code reference to a type
/// whose ruled home is a proto that does not exist yet is a reference to a moving
/// target; the name is what is stable.
/// </para>
/// </remarks>
public static class InvocationRefusal
{
    /// <summary>Refuse one invocation, naming its kind and the reason.</summary>
    /// <param name="invocation">What the Hub asked for.</param>
    /// <param name="cancellationToken">Unused: a refusal does no work to cancel.</param>
    /// <returns>A faulted task, always — the session answers it as a <c>Fault</c>.</returns>
    /// <remarks>
    /// Faulted through <see cref="ValueTask.FromException{TResult}(Exception)"/>
    /// rather than thrown, so the refusal reaches the session as a failed
    /// operation however the session happens to call its handler.
    /// </remarks>
    public static ValueTask<ReadOnlyMemory<byte>> HandleAsync(
        ConnectorInvocation invocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        return ValueTask.FromException<ReadOnlyMemory<byte>>(new NotSupportedException(
            $"pms-oracle recognises no invocation kinds yet, so '{invocation.Kind}' was " +
            "refused rather than guessed at. No invocation vocabulary between the Hub " +
            "and a connector has been ruled, and dispatching on this name would make " +
            "this package define it."));
    }
}
