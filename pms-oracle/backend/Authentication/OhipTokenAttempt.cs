using System.Net;
using System.Net.Http.Headers;

using HotelOS.Connector;

namespace PmsOracle.Authentication;

/// <summary>
/// Asking OHIP for a token, and reporting what happened in an operator's terms.
/// </summary>
/// <remarks>
/// <para>
/// <b>The smallest call that proves the whole credential set.</b> A token
/// request presents the application key, the client pair and the OPERA user's
/// password at once — so a tenancy that issues a token has accepted every one
/// of them, and one that refuses says which layer failed. There is no cheaper
/// call that proves as much, and no more expensive one that proves more.
/// </para>
/// <para>
/// <b>It reads nothing and changes nothing.</b> The queue is emptied by
/// reading, so a test that drained it would discard a hotel's changes to prove
/// it could reach them. A token request is the one OHIP call with no such cost.
/// </para>
/// <para>
/// <b>Refused and unreachable are kept apart</b>, because they send an operator
/// to different people: one re-issues a credential, the other opens a firewall.
/// The status code decides, and the vendor's own body is never forwarded — it
/// is a diagnostic written for an integrator, and ADR 0041's boundary is that
/// only a sentence written for the reader crosses.
/// </para>
/// </remarks>
public static class OhipTokenAttempt
{
    /// <summary>Try the configured credentials against the tenancy.</summary>
    /// <param name="http">The client to dial with.</param>
    /// <param name="credentials">The property's credential set.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>What was found, in words an operator may read.</returns>
    public static async Task<ConnectionTest> TryAsync(
        HttpClient http,
        OhipCredentials credentials,
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

            return Read(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            // Nothing answered: DNS, a closed port, a certificate, a firewall.
            // Not a credential problem, and saying so is the difference between
            // an operator calling their network team and their PMS vendor.
            return ConnectionTest.Unreachable(
                "Nothing answered at that endpoint. Check the OHIP host and that this "
                + "property can reach it.");
        }
        catch (TaskCanceledException)
        {
            // A timeout arrives here rather than as HttpRequestException. Same
            // conclusion, different sentence: something is there and is not
            // answering in time.
            return ConnectionTest.Unreachable(
                "The OHIP host did not answer in time.");
        }
    }

    /// <summary>What a status code means to the person who pressed the button.</summary>
    /// <param name="status">What the tenancy answered.</param>
    /// <returns>The finding.</returns>
    /// <remarks>
    /// <b>A 200 is the only success, and everything else is named rather than
    /// grouped.</b> A 403 with correct credentials means the user exists and
    /// lacks a grant — an OPERA permissions problem, not a typo — and telling
    /// somebody to re-check their password there would send them the wrong way
    /// for an afternoon.
    /// </remarks>
    public static ConnectionTest Read(HttpStatusCode status) => status switch
    {
        HttpStatusCode.OK => ConnectionTest.Reached(
            "OHIP issued a token for these credentials."),

        HttpStatusCode.Unauthorized => ConnectionTest.Refused(
            "OHIP rejected these credentials. Check the client pair and the PMS "
            + "user's password."),

        HttpStatusCode.Forbidden => ConnectionTest.Refused(
            "OHIP accepted the credentials and refused the request. The PMS user "
            + "is likely missing a grant, or the application key is not enabled "
            + "for this hotel."),

        HttpStatusCode.NotFound => ConnectionTest.Unreachable(
            "That endpoint answered, but has no OHIP token service. Check the OHIP "
            + "host."),

        HttpStatusCode.TooManyRequests => ConnectionTest.Unreachable(
            "OHIP is rate-limiting this integration. Try again shortly."),

        // Anything else is the tenancy having a bad day rather than a statement
        // about these credentials, and it is reported as reachable-but-unwell
        // rather than as a refusal somebody would go and change a password over.
        _ => ConnectionTest.Unreachable(
            $"OHIP answered {(int)status}, which is not a token. The credentials were "
            + "neither accepted nor rejected."),
    };
}
