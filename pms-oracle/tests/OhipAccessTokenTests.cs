using System.Net;
using System.Text;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Authentication;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// What OHIP answered a password grant with, and what this connector keeps of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The reply shape is the reference's</b> —
/// <c>providers/oracle/cloud/dto/jpa/OracleCloudAuthToken.java:20-25</c>:
/// <c>access_token</c> and <c>expires_in</c>, under OHIP's spellings, with
/// everything else on the response ignored.
/// </para>
/// <para>
/// <b>These are the half <c>OhipTokenAttempt</c> could not have.</b> It read a
/// status code and discarded the body, so nothing could assert what a grant
/// actually returns — and a drain needs exactly that.
/// </para>
/// </remarks>
public class OhipAccessTokenTests
{
    private static readonly DateTimeOffset Issued = new(2026, 9, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_granted_token_is_kept_with_the_lifetime_ohip_stated()
    {
        var acquired = await AcquireAsync(
            HttpStatusCode.OK, """{"access_token":"ohip-token","expires_in":3600}""");

        Assert.True(acquired.Granted);
        Assert.Equal("ohip-token", acquired.AccessToken);
        Assert.Equal(ConnectionTestOutcome.Reached, acquired.Finding.Outcome);

        // The lifetime is OHIP's statement, not a policy of ours: an hour here
        // because the source said an hour.
        Assert.Equal(Issued + TimeSpan.FromSeconds(3600), acquired.Lifetime?.ExpiresAt);
    }

    [Fact]
    public async Task A_grant_that_states_no_lifetime_has_none_rather_than_a_default()
    {
        var acquired = await AcquireAsync(HttpStatusCode.OK, """{"access_token":"ohip-token"}""");

        // Granted, and nothing is known about when it stops working. A default
        // would invent a refresh schedule nobody was told about, and a zero
        // would read as already expired.
        Assert.True(acquired.Granted);
        Assert.Null(acquired.Lifetime);
    }

    [Fact]
    public async Task A_two_hundred_with_no_token_is_not_reached()
    {
        var acquired = await AcquireAsync(HttpStatusCode.OK, """{"token_type":"Bearer"}""");

        // The endpoint answered and granted nothing, so the connection cannot
        // make a single call. Reporting REACHED would tell an operator their
        // configuration works — which is what the status code alone said, and
        // the reason reading the body is not decoration.
        Assert.False(acquired.Granted);
        Assert.Equal(ConnectionTestOutcome.Unreachable, acquired.Finding.Outcome);
        Assert.Contains("no access token", acquired.Finding.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_body_this_connector_cannot_read_is_reported_as_such()
    {
        var acquired = await AcquireAsync(HttpStatusCode.OK, "<html>maintenance</html>");

        // A proxy's holding page answering 200 is the shape that produces this,
        // and it is neither a refusal nor a token.
        Assert.False(acquired.Granted);
        Assert.Equal(ConnectionTestOutcome.Unreachable, acquired.Finding.Outcome);
    }

    [Fact]
    public async Task A_rejected_grant_carries_no_token_and_blames_the_credentials()
    {
        var acquired = await AcquireAsync(HttpStatusCode.Unauthorized, "{}");

        Assert.False(acquired.Granted);
        Assert.Equal(ConnectionTestOutcome.Refused, acquired.Finding.Outcome);
    }

    private static async Task<OhipTokenAcquisition> AcquireAsync(HttpStatusCode status, string body)
    {
        using var http = new HttpClient(new Answered(status, body))
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        Assert.True(OhipCredentials.Read(Settings(), Secrets()).TryGet(out var credentials));

        return await OhipAccessToken.AcquireAsync(http, credentials, Issued, CancellationToken.None);
    }

    private static Dictionary<string, string> Settings() => new(StringComparer.Ordinal)
    {
        [OhipCredentials.EndpointSetting] = "https://ohip.example",
        [OhipCredentials.HotelCodeSetting] = "KOCHI01",
        [OhipCredentials.ExternalSystemCodeSetting] = "HOTELOS",
        [OhipCredentials.ClientIdSetting] = "client",
        [OhipCredentials.PmsUsernameSetting] = "supervisor",
    };

    private static Dictionary<string, string> Secrets() => new(StringComparer.Ordinal)
    {
        [OhipCredentials.ApplicationKeySecret] = "key",
        [OhipCredentials.PmsPasswordSecret] = "password",
        [OhipCredentials.ClientSecretSecret] = "secret",
    };

    private sealed class Answered(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
