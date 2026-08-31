namespace PmsOracle.Authentication;

/// <summary>
/// When an OHIP access token must be refreshed, computed from the lifetime the
/// token itself declared.
/// </summary>
/// <remarks>
/// <para>
/// The reference refreshed on a hardcoded 45-minute threshold while
/// <c>expires_in</c> sat unread on the row beside it — and the sweep that would
/// have applied even that was commented out, in two different providers. A
/// token approaching expiry therefore produced silence, and the first anyone
/// knew was a 401 in the middle of a poll.
/// </para>
/// <para>
/// Here the token's own declared lifetime is the only input. There is no
/// constant to drift from a vendor's configuration change, and no way to
/// construct this type without supplying one.
/// </para>
/// </remarks>
public readonly record struct TokenLifetime
{
    /// <summary>
    /// The fraction of a token's life after which it is refreshed.
    /// </summary>
    /// <remarks>
    /// Early enough that a refresh has room to fail and be retried before
    /// anything is rejected, late enough not to spend a grant per poll. The
    /// remaining 15% is the margin the reference did not have.
    /// </remarks>
    public const double RefreshAtFractionOfLife = 0.85;

    private TokenLifetime(DateTimeOffset issuedAt, TimeSpan lifetime)
    {
        IssuedAt = issuedAt;
        Lifetime = lifetime;
    }

    /// <summary>When the authorisation server issued the token.</summary>
    public DateTimeOffset IssuedAt { get; }

    /// <summary>How long the token is valid, as the token declared.</summary>
    public TimeSpan Lifetime { get; }

    /// <summary>When the token stops being accepted.</summary>
    public DateTimeOffset ExpiresAt => IssuedAt + Lifetime;

    /// <summary>When a refresh should be attempted.</summary>
    public DateTimeOffset RefreshAt =>
        IssuedAt + (Lifetime * RefreshAtFractionOfLife);

    /// <summary>
    /// Read a lifetime from a token response.
    /// </summary>
    /// <param name="issuedAt">When the response arrived.</param>
    /// <param name="expiresInSeconds">The <c>expires_in</c> the token declared.</param>
    /// <returns>The lifetime, or <c>null</c> when the server declared none.</returns>
    /// <remarks>
    /// A missing or non-positive <c>expires_in</c> returns <c>null</c> rather
    /// than substituting a default. A guessed lifetime is the defect this type
    /// exists to remove, and a guessed one that looks plausible is worse than
    /// an absent one: the caller must decide what to do about a server that did
    /// not say, and that decision belongs where it can be seen.
    /// </remarks>
    public static TokenLifetime? FromExpiresIn(DateTimeOffset issuedAt, int expiresInSeconds) =>
        expiresInSeconds > 0
            ? new TokenLifetime(issuedAt, TimeSpan.FromSeconds(expiresInSeconds))
            : null;

    /// <summary>Whether a refresh is due.</summary>
    /// <param name="now">The current time.</param>
    /// <returns><c>true</c> once <see cref="RefreshAt"/> has passed.</returns>
    public bool NeedsRefresh(DateTimeOffset now) => now >= RefreshAt;

    /// <summary>Whether the token can no longer be used.</summary>
    /// <param name="now">The current time.</param>
    /// <returns><c>true</c> once <see cref="ExpiresAt"/> has passed.</returns>
    /// <remarks>
    /// Separate from <see cref="NeedsRefresh"/> on purpose: between the two the
    /// token still works, which is the window a failed refresh has to recover
    /// in. Collapsing them would make one slow refresh an outage.
    /// </remarks>
    public bool HasExpired(DateTimeOffset now) => now >= ExpiresAt;
}
