using System.Text;

namespace PmsOracle.Authentication;

/// <summary>
/// The token request OHIP accepts: a password grant, authenticated as the OAuth
/// client and identified by the tenancy's application key.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three credentials in one call, and they prove different things.</b> The
/// application key identifies the tenancy, the client pair proves the
/// integration, and the username and password authenticate the OPERA user whose
/// permissions the poll then runs under. A shape that carried fewer would still
/// look like OAuth and would be refused by the tenancy.
/// </para>
/// <para>
/// <b>Built here, sent nowhere.</b> This produces the form fields and the
/// authorization header; the transport that posts them is
/// <c>IOhipQueue</c>'s and remains unimplemented. Keeping the construction
/// separate from the sending is what lets it be tested exhaustively without a
/// network, and it is the half <c>CONN-Q12</c> asked for backend-first.
/// </para>
/// </remarks>
public static class OhipPasswordGrant
{
    /// <summary>The grant type this connector uses.</summary>
    /// <remarks>
    /// A password grant, because OHIP scopes what a poll may read to an OPERA
    /// user rather than to the client. A client-credentials grant would
    /// authenticate the integration and leave the tenancy unable to say which
    /// user's permissions applied.
    /// </remarks>
    public const string GrantType = "password";

    /// <summary>Where a tenancy issues tokens, relative to its host.</summary>
    public const string TokenPath = "/oauth/v1/tokens";

    /// <summary>The absolute token endpoint for one property's tenancy.</summary>
    /// <param name="credentials">The property's credential set.</param>
    /// <returns>The URL a token request is posted to.</returns>
    public static string TokenEndpoint(OhipCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return credentials.Endpoint + TokenPath;
    }

    /// <summary>
    /// The form-encoded body of a token request.
    /// </summary>
    /// <param name="credentials">The property's credential set.</param>
    /// <returns>The fields, in a fixed order.</returns>
    /// <remarks>
    /// A fixed order so a test can assert the whole body rather than probing it
    /// field by field — and so two runs produce the same bytes, which matters
    /// the day a request has to be compared against a vendor's capture.
    /// </remarks>
    public static IReadOnlyList<KeyValuePair<string, string>> Form(
        OhipCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        return
        [
            new("grant_type", GrantType),
            new("username", credentials.PmsUsername),
            new("password", credentials.PmsPassword),
        ];
    }

    /// <summary>
    /// The <c>Authorization</c> header value: the client pair, Basic-encoded.
    /// </summary>
    /// <param name="credentials">The property's credential set.</param>
    /// <returns>A <c>Basic </c>-prefixed header value.</returns>
    /// <remarks>
    /// The client pair authenticates the request itself and so travels in the
    /// header, while the user's credentials travel in the body — OHIP's shape,
    /// and the reason the client secret is not simply another form field.
    /// </remarks>
    public static string BasicAuthorization(OhipCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var pair = $"{credentials.ClientId}:{credentials.ClientSecret}";
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));
    }
}
