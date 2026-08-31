using PmsOracle.Authentication;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The token's declared lifetime governs the refresh — the constant the
/// reference used instead is the defect these tests hold shut.
/// </summary>
public sealed class TokenLifetimeTests
{
    private static readonly DateTimeOffset Issued =
        new(2026, 8, 31, 14, 2, 0, TimeSpan.Zero);

    [Fact]
    public void the_refresh_is_computed_from_the_declared_lifetime()
    {
        var lifetime = TokenLifetime.FromExpiresIn(Issued, expiresInSeconds: 3600);

        Assert.NotNull(lifetime);
        Assert.Equal(Issued.AddHours(1), lifetime.Value.ExpiresAt);
        // 85% of an hour is 51 minutes.
        Assert.Equal(Issued.AddMinutes(51), lifetime.Value.RefreshAt);
    }

    /// <summary>
    /// A server that declares a different lifetime gets a different refresh.
    /// The reference's 45-minute constant would have been wrong in both
    /// directions here — late for the short token, early for the long one.
    /// </summary>
    [Theory]
    [InlineData(600, 510)]      // 10 minutes → refresh at 8.5
    [InlineData(3600, 3060)]    // an hour    → refresh at 51 minutes
    [InlineData(28800, 24480)]  // 8 hours    → refresh at 6.8 hours
    public void a_shorter_or_longer_token_moves_the_refresh_with_it(
        int expiresInSeconds,
        int expectedRefreshAfterSeconds)
    {
        var lifetime = TokenLifetime.FromExpiresIn(Issued, expiresInSeconds);

        Assert.NotNull(lifetime);
        Assert.Equal(
            Issued.AddSeconds(expectedRefreshAfterSeconds),
            lifetime.Value.RefreshAt);
    }

    /// <summary>
    /// A server that declares nothing usable gets no lifetime — not a default.
    /// A plausible guess is worse than an absence, because nobody looks at it.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void a_missing_or_impossible_expires_in_yields_no_lifetime(int expiresInSeconds)
    {
        Assert.Null(TokenLifetime.FromExpiresIn(Issued, expiresInSeconds));
    }

    /// <summary>
    /// Between the refresh point and expiry the token still works. That window
    /// is what a failed refresh recovers in, and collapsing the two would turn
    /// one slow refresh into an outage.
    /// </summary>
    [Fact]
    public void a_token_due_for_refresh_has_not_yet_expired()
    {
        var lifetime = TokenLifetime.FromExpiresIn(Issued, expiresInSeconds: 3600)!.Value;
        var justAfterRefreshPoint = lifetime.RefreshAt.AddSeconds(1);

        Assert.True(lifetime.NeedsRefresh(justAfterRefreshPoint));
        Assert.False(lifetime.HasExpired(justAfterRefreshPoint));

        // …and there are nine minutes of it left.
        Assert.Equal(TimeSpan.FromMinutes(9), lifetime.ExpiresAt - lifetime.RefreshAt);
    }

    [Fact]
    public void a_fresh_token_needs_nothing()
    {
        var lifetime = TokenLifetime.FromExpiresIn(Issued, expiresInSeconds: 3600)!.Value;

        Assert.False(lifetime.NeedsRefresh(Issued));
        Assert.False(lifetime.HasExpired(Issued));
    }

    [Fact]
    public void an_expired_token_is_both_expired_and_due()
    {
        var lifetime = TokenLifetime.FromExpiresIn(Issued, expiresInSeconds: 3600)!.Value;
        var afterExpiry = lifetime.ExpiresAt.AddSeconds(1);

        Assert.True(lifetime.HasExpired(afterExpiry));
        Assert.True(lifetime.NeedsRefresh(afterExpiry));
    }
}
