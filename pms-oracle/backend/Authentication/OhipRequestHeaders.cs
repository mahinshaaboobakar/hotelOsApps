namespace PmsOracle.Authentication;

/// <summary>
/// The headers OHIP requires on every call, token requests included.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every call, not just the authenticated ones.</b> The application key
/// identifies the tenancy before any token exists, so it rides the token
/// request too; the hotel id and external system code say which property and
/// which integration a request speaks for. A call missing one is refused by the
/// tenancy with a message about the header rather than about the credential,
/// which is a long way from the cause.
/// </para>
/// <para>
/// <b><c>x-hotelid</c> comes from the hotel code, and they are the same
/// value under two names</b> — the property as OPERA registered it. It is a
/// setting rather than something derived from the platform's property id
/// because only the PMS knows what it calls this hotel; ADR 0052's rule that
/// the establishing party owns the value.
/// </para>
/// </remarks>
public static class OhipRequestHeaders
{
    /// <summary>The tenancy's application key header.</summary>
    public const string ApplicationKey = "x-app-key";

    /// <summary>The property header, carrying the hotel code.</summary>
    public const string HotelId = "x-hotelid";

    /// <summary>The integration's registered code at the tenancy.</summary>
    public const string ExternalSystemCode = "x-externalsystem";

    /// <summary>
    /// What every request to this property's tenancy carries.
    /// </summary>
    /// <param name="credentials">The property's credential set.</param>
    /// <returns>The headers, in a fixed order.</returns>
    /// <remarks>
    /// The bearer token is deliberately absent: it has a lifetime and these do
    /// not, and a helper that returned both would invite a caller to cache the
    /// pair and keep sending an expired token — the reference system's defect
    /// that <see cref="TokenLifetime"/> exists to prevent.
    /// </remarks>
    public static IReadOnlyList<KeyValuePair<string, string>> ForEveryCall(
        OhipCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        return
        [
            new(ApplicationKey, credentials.ApplicationKey),
            new(HotelId, credentials.HotelCode),
            new(ExternalSystemCode, credentials.ExternalSystemCode),
        ];
    }
}
