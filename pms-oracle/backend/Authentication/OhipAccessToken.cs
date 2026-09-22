using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Authentication;

/// <summary>
/// Asking OHIP for an access token, and keeping what it answered.
/// </summary>
/// <remarks>
/// <para>
/// <b>This replaces <c>OhipTokenAttempt</c>, which asked the same question and
/// threw the answer away.</b> That type posted the password grant and read only
/// the status code, because a connection test wants a verdict and nothing else
/// existed to hold a token for. A drain needs the token itself, and two callers
/// making the same request for different halves of one reply is how the two
/// stop agreeing. The verdict is still produced here —
/// <see cref="OhipTokenAcquisition.Finding"/> —
/// so the test path lost nothing.
/// </para>
/// <para>
/// <b>The grant and the response shape are the reference's</b>, transcribed
/// under OHIP's own spellings:
/// <c>providers/oracle/cloud/dto/jpa/OracleCloudAuthToken.java:20-25</c> —
/// <c>access_token</c> and <c>expires_in</c>, with anything else on the
/// response ignored, as the reference ignores it. The vendor's names stop at
/// this type; <see cref="TokenLifetime"/> and everything above it are ours.
/// </para>
/// <para>
/// <b>An absent <c>expires_in</c> is an absent lifetime, never a default.</b>
/// OHIP stating no expiry and OHIP stating one are different facts, and a
/// default here would invent a refresh schedule nobody was told about —
/// <see cref="TokenLifetime.FromExpiresIn"/> answers <c>null</c> and the caller
/// decides what to do with a server that did not say.
/// </para>
/// </remarks>
public static class OhipAccessToken
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Ask OHIP for a token.</summary>
    /// <param name="http">The client to dial with.</param>
    /// <param name="credentials">Everything the password grant needs.</param>
    /// <param name="issuedAt">When the answer arrived, for the lifetime.</param>
    /// <param name="cancellationToken">The caller's.</param>
    /// <returns>What OHIP said, and the token when it granted one.</returns>
    public static async Task<OhipTokenAcquisition> AcquireAsync(
        HttpClient http,
        OhipCredentials credentials,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(credentials);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, OhipPasswordGrant.TokenEndpoint(credentials))
        {
            Content = new FormUrlEncodedContent(OhipPasswordGrant.Form(credentials)),
        };

        request.Headers.TryAddWithoutValidation(
            "Authorization", OhipPasswordGrant.BasicAuthorization(credentials));

        foreach (var (name, value) in OhipRequestHeaders.ForEveryCall(credentials))
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return new OhipTokenAcquisition(Read(response.StatusCode), null, null);
            }

            var granted = JsonSerializer.Deserialize<TokenResponse>(
                await response.Content.ReadAsStringAsync(cancellationToken), Json);

            if (string.IsNullOrWhiteSpace(granted?.AccessToken))
            {
                // A 200 with no token in it. Not a credential problem and not a
                // network one: the endpoint answered and did not grant, and
                // calling that REACHED would report a working connection that
                // cannot make a single request.
                return new OhipTokenAcquisition(
                    ConnectionTest.Unreachable(
                        "OHIP accepted the request and returned no access token."),
                    null,
                    null);
            }

            return new OhipTokenAcquisition(
                ConnectionTest.Reached("OHIP granted a token for these credentials."),
                granted.AccessToken,
                TokenLifetime.FromExpiresIn(issuedAt, granted.ExpiresIn));
        }
        catch (HttpRequestException)
        {
            // Nothing answered: DNS, a closed port, a certificate, a firewall.
            // Not a credential problem, and saying so is the difference between
            // an operator calling their network team and their PMS vendor.
            return new OhipTokenAcquisition(
                ConnectionTest.Unreachable(
                    "Nothing answered at that endpoint. Check the OHIP host and that this "
                    + "property can reach it."),
                null,
                null);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A timeout arrives here rather than as HttpRequestException. Same
            // conclusion, different sentence: something is there and is not
            // answering in time.
            return new OhipTokenAcquisition(
                ConnectionTest.Unreachable("The OHIP host did not answer in time."), null, null);
        }
        catch (JsonException)
        {
            return new OhipTokenAcquisition(
                ConnectionTest.Unreachable(
                    "OHIP answered the token request with a body this connector cannot read."),
                null,
                null);
        }
    }

    /// <summary>What a status other than 200 means for the person who pressed Test.</summary>
    /// <remarks>
    /// Carried over unchanged from <c>OhipTokenAttempt</c>, which is removed in
    /// the same commit: a wrong credential and an unreachable host send an
    /// operator to different people, and the status is the only thing that
    /// separates them here.
    /// </remarks>
    private static ConnectionTest Read(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new(
            ConnectionTestOutcome.Refused,
            "OHIP rejected these credentials. Check the client id, the client secret, the "
            + "PMS username and its password.",
            []),

        HttpStatusCode.NotFound => ConnectionTest.Unreachable(
            "That OHIP host answered, and not with a token endpoint. Check the host address."),

        _ => ConnectionTest.Unreachable(
            $"OHIP answered the token request with {(int)status}."),
    };

    /// <summary>OHIP's token reply, in OHIP's own spellings.</summary>
    /// <param name="AccessToken">`access_token`.</param>
    /// <param name="ExpiresIn">
    /// `expires_in`, in seconds. Zero when the field is absent, which
    /// <see cref="TokenLifetime.FromExpiresIn"/> reads as no stated lifetime
    /// rather than as an immediate expiry.
    /// </param>
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

/// <summary>What asking OHIP for a token produced.</summary>
/// <param name="Finding">
/// The verdict a connection test reports — REACHED, REFUSED or UNREACHABLE.
/// Always present: every path through the request produces one.
/// </param>
/// <param name="AccessToken">The token, when one was granted; otherwise <c>null</c>.</param>
/// <param name="Lifetime">
/// What OHIP said about how long it lasts. <c>null</c> means it said nothing,
/// which is not the same as saying zero.
/// </param>
public sealed record OhipTokenAcquisition(
    ConnectionTest Finding, string? AccessToken, TokenLifetime? Lifetime)
{
    /// <summary>Whether OHIP granted a token this connector can call with.</summary>
    public bool Granted => AccessToken is not null;
}
