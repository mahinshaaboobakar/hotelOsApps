using HotelOS.Connector;

namespace PmsOracle.Hosting;

/// <summary>
/// What this connector answers to a kind it does not serve: a refusal that
/// says which kind, and that it was refused rather than guessed at.
/// </summary>
/// <remarks>
/// <para>
/// <b>This used to refuse everything, and its reason has been replaced rather
/// than deleted.</b> It read: <i>"pms-oracle recognises no invocation kinds
/// yet… no invocation vocabulary between the Hub and a connector has been
/// ruled, and dispatching on this name would make this package define it."</i>
/// That was correct when written. ADR 0194 ruled the vocabulary and the
/// platform landed <see cref="HotelOS.Connector.ConnectorProtocolKinds"/> on
/// 2026-09-19, so the argument for refusing became an argument for dispatching
/// — and the old sentence would have read as current to anyone meeting it.
/// <see cref="InvocationDispatch"/> now serves the ruled kinds and this
/// answers the rest.
/// </para>
/// <para>
/// <b>A refusal, not a silence.</b> The session turns a failed handler into a
/// <c>Fault</c> frame carrying the exception's message, so the Hub learns the
/// kind it sent and why it went unserved — rather than a reply that looks like
/// an answer, or a hang that looks like a slow OHIP.
/// </para>
/// <para>
/// <b>Unrecognised is not unimplemented.</b> <c>drain</c> is understood and
/// refused elsewhere with what is missing; a kind that reaches here is one no
/// version of this connector knows, which usually means a newer Hub talking to
/// an older package. Saying so plainly is what tells an operator which of the
/// two to upgrade.
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
            $"pms-oracle does not serve the invocation kind '{invocation.Kind}'. It serves " +
            $"'{HotelOS.Connector.ConnectorProtocolKinds.Test}' and understands " +
            $"'{HotelOS.Connector.ConnectorProtocolKinds.Drain}'. A kind this package does " +
            "not know is usually a newer Hub talking to an older connector, so it is refused " +
            "by name rather than guessed at."));
    }
}
