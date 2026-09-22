using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Authentication;

namespace PmsOracle.Hosting;

/// <summary>
/// Serving <c>test</c>: does this configuration actually reach OHIP? — ADR 0194.
/// </summary>
/// <remarks>
/// <para>
/// <b>The settings arrive with the invocation; the credentials do not.</b>
/// <see cref="TestInvocation"/> carries the endpoint, the hotel code and the
/// rest — what an administrator typed — and the secrets are requested by name
/// inside this invocation (<see cref="InvocationCredentials"/>). So a test
/// runs against exactly what the property configured, with the Hub deciding
/// each credential separately.
/// </para>
/// <para>
/// <b>An incomplete configuration is answered without calling OHIP.</b> There
/// is nothing to learn from asking a token endpoint for a grant that has no
/// password: the answer would be a refusal that reads like a wrong credential.
/// <c>CONFIGURATION_INCOMPLETE</c> names the missing fields instead, which is
/// what the form needs, and a credential the Hub denied appears in that list
/// exactly like a setting nobody filled in — because to the person at the
/// screen it is the same problem.
/// </para>
/// <para>
/// <b>The outcome is the connector's own finding, relayed unreworded.</b>
/// <see cref="OhipAccessToken"/> answers REACHED, REFUSED or UNREACHABLE from
/// what OHIP did, and this maps that record onto the wire message field for
/// field. It does not decide anything: a Hub that could not get an answer at
/// all reports that itself (<c>CONN-Q43</c>), and this end never speaks for a
/// silence it did not hear.
/// </para>
/// </remarks>
public static class ConnectionTestInvocation
{
    /// <summary>Why the Hub is asked for the credentials, for its own record.</summary>
    private const string Purpose = "test the OHIP connection";

    /// <summary>Run one test and answer it.</summary>
    /// <param name="invocation">The <c>test</c> invocation, carrying the settings.</param>
    /// <param name="http">The client the token attempt dials OHIP with.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>A <see cref="TestResult"/>, serialised.</returns>
    public static async ValueTask<ReadOnlyMemory<byte>> ServeAsync(
        ConnectorInvocation invocation, HttpClient http, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var asked = TestInvocation.Parser.ParseFrom(invocation.Payload.Span);

        var secrets = await InvocationCredentials.GrantedAsync(
            invocation, OhipCredentials.SecretNames, Purpose, cancellationToken);

        var reading = OhipCredentials.Read(asked.Settings, secrets);

        if (!reading.TryGet(out var credentials))
        {
            return Answer(ConnectionTest.Incomplete(reading.Missing));
        }

        // The acquisition answers the verdict and the token together; a test
        // reads the verdict and drops the token, which is the one place it is
        // right to hold nothing.
        var acquired = await OhipAccessToken.AcquireAsync(
            http, credentials, DateTimeOffset.UtcNow, cancellationToken);

        return Answer(acquired.Finding);
    }

    /// <summary>The connector's finding, on the wire.</summary>
    /// <remarks>
    /// Field for field: <see cref="ConnectionTest"/> and
    /// <see cref="TestResult"/> carry the same three, which is why this is a
    /// transcription rather than a translation. A value this build does not
    /// know is not reinterpreted here — the Hub reads an unknown outcome as no
    /// finding, and deciding it twice would be two places to keep agreeing.
    /// </remarks>
    private static ReadOnlyMemory<byte> Answer(ConnectionTest found)
    {
        var result = new TestResult { Outcome = found.Outcome, Detail = found.Detail };

        result.Missing.AddRange(found.Missing);

        return result.ToByteArray();
    }
}
