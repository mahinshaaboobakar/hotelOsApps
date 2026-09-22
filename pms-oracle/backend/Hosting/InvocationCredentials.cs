using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Hosting;

/// <summary>
/// Asking the Hub for this integration's credentials, from inside the
/// invocation that needs them — ADR 0186, ADR 0189.
/// </summary>
/// <remarks>
/// <para>
/// <b>The connector holds no secret and is handed none.</b> It asks by name,
/// inside an invocation, and the Hub authorizes each request against the signed
/// manifest's <c>required_secrets</c> before answering. A connector that was
/// given its credentials with the work would hold them whether or not it used
/// them, and nothing could refuse one request while allowing another.
/// </para>
/// <para>
/// <b>A denial is an answer, not a failure.</b> <c>NOT_CONFIGURED</c> means an
/// administrator has not set this credential for this property — the remedy is
/// a form, and the test that follows says exactly which name is missing.
/// <c>NOT_DECLARED</c> means this package never asked for it in its manifest,
/// which is a packaging defect and reads as one. Neither is turned into an
/// exception here: the caller decides what an absent credential means for the
/// operation it is doing.
/// </para>
/// <para>
/// <b>No id travels.</b> <see cref="CredentialRequest"/> carries a name and a
/// purpose and nothing else; the session stamps the frame with the invocation
/// it belongs to, so a connector has no say over which operation its request is
/// attributed to (ADR 0208's trusted-reader rule, at this end of the protocol).
/// </para>
/// </remarks>
public static class InvocationCredentials
{
    /// <summary>Ask for every credential a name list names.</summary>
    /// <param name="invocation">The invocation asking; the request belongs to it.</param>
    /// <param name="names">The credential names, from <c>OhipCredentials.SecretNames</c>.</param>
    /// <param name="purpose">Why, in one phrase, for the Hub's audit of the request.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>
    /// The material granted, by name. A denied credential is simply absent — the
    /// caller reads what it needs and reports what it did not get, rather than
    /// receiving an empty string that would read as a configured blank.
    /// </returns>
    public static async Task<IReadOnlyDictionary<string, string>> GrantedAsync(
        ConnectorInvocation invocation,
        IReadOnlyList<string> names,
        string purpose,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(names);

        var granted = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            var request = new CredentialRequest { CredentialName = name, Purpose = purpose };

            var answer = CredentialResponse.Parser.ParseFrom(
                (await invocation.RequestAsync(
                    ConnectorProtocolKinds.CredentialRequest,
                    request.ToByteArray(),
                    cancellationToken)).Span);

            if (answer.OutcomeCase is CredentialResponse.OutcomeOneofCase.Granted)
            {
                // Opaque material, decoded as text because every OHIP credential
                // is a string the token request carries in a header or a form
                // field. A future credential that is not text would need its own
                // reading rather than this one widened.
                granted[name] = answer.Granted.Material.ToStringUtf8();
            }
        }

        return granted;
    }
}
