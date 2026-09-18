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
/// fault as serialising a
/// <see cref="HotelOS.Contracts.Integration.V1.CredentialRequest"/> ahead of its ruling:
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
/// reached from</b>, and it is not reached yet: there is no ruled invocation to
/// make a credential request inside — <c>CONN-Q34</c>, open. The message itself
/// is ruled and landed. ADR 0189:
/// <see cref="HotelOS.Contracts.Integration.V1.CredentialRequest"/> is the
/// Protobuf message in <c>shared/protos/hotelos/integration/v1/credential.proto</c>
/// — <c>credential_name</c> and <c>purpose</c>, opaque to the Runtime, and <b>no
/// id of any kind</b>: the session stamps the frame, so a connector has no say
/// over which invocation its request belongs to. The reply is granted (opaque
/// material, and an <c>expires_at</c> whose absence means no lifetime was
/// <i>stated</i>, not that it never expires) or denied (<c>NOT_DECLARED</c>,
/// <c>NOT_CONFIGURED</c>). <c>OhipPasswordGrant</c> serialises that message once
/// there is an invocation to send it from.
/// </para>
/// <para>
/// <b>What this paragraph used to say, kept so the correction is checkable.</b>
/// At <c>ab2097c</c> it read <i>"how a <c>&lt;see cref="CredentialRequest"/&gt;</c>
/// is encoded as bytes is <c>CONN-Q28</c>, open"</i>, against a C# record in
/// <c>HotelOS.Connector</c>. ADR 0189 ruled <c>CONN-Q28</c> and <c>08d2d737</c>
/// replaced the record with the generated type, so that cref stopped resolving —
/// CS1574, a build failure under <c>TreatWarningsAsErrors</c>, from a commit in
/// another repository. At <c>7eb361b</c> it was then named rather than referenced,
/// on the belief the proto was not yet written; it was, in the same commit. The
/// cref above now points at the generated type, so the next change to it fails the
/// build here instead of reading as current.
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
