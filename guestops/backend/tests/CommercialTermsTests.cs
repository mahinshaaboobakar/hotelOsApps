using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The terms, and the deadline that is computed rather than stored — R18.
/// </summary>
public class CommercialTermsTests
{
    /// <summary>
    /// Move the arrival and the deadline moves with it.
    /// </summary>
    /// <remarks>
    /// <b>The whole reason the offset is stored and the date is not.</b> The
    /// system this replaces kept two pre-formatted human strings and discarded
    /// the structure — so a reservation whose arrival changed carried a
    /// cancellation deadline that no longer matched it, which is a chargeable
    /// error nobody can see.
    /// </remarks>
    [Fact]
    public void The_deadline_follows_the_arrival()
    {
        var terms = new CommercialTerms
        {
            CancelOffsetDaysFromArrival = 2,
            CancelDropTime = new TimeOnly(18, 0),
        };

        var first = terms.CancellationDeadline(new DateOnly(2026, 9, 3));
        var moved = terms.CancellationDeadline(new DateOnly(2026, 9, 10));

        Assert.Equal(new DateTime(2026, 9, 1, 18, 0, 0), first!.Value.DateTime);
        Assert.Equal(new DateTime(2026, 9, 8, 18, 0, 0), moved!.Value.DateTime);
    }

    /// <summary>No offset, no deadline — never a guessed one.</summary>
    [Fact]
    public void An_unstated_offset_produces_no_deadline()
    {
        var terms = new CommercialTerms { CancelDropTime = new TimeOnly(18, 0) };

        Assert.Null(terms.CancellationDeadline(new DateOnly(2026, 9, 3)));
    }

    /// <summary>And no arrival, no deadline.</summary>
    /// <remarks>
    /// A stay whose dates are not yet known has nothing to count back from, and
    /// counting from today would produce a deadline the guest never agreed to.
    /// </remarks>
    [Fact]
    public void An_unknown_arrival_produces_no_deadline()
    {
        var terms = new CommercialTerms { CancelOffsetDaysFromArrival = 2 };

        Assert.Null(terms.CancellationDeadline(null));
    }

    /// <summary>Where a source gives whole days, the window closes at midnight.</summary>
    [Fact]
    public void A_missing_drop_time_closes_the_window_at_midnight()
    {
        var terms = new CommercialTerms { CancelOffsetDaysFromArrival = 1 };

        var deadline = terms.CancellationDeadline(new DateOnly(2026, 9, 3));

        Assert.Equal(new DateTime(2026, 9, 2, 0, 0, 0), deadline!.Value.DateTime);
    }

    /// <summary>An amount is three things, and a currency is what makes it one.</summary>
    /// <remarks>
    /// R19. The reference carried a <c>float</c> beside a <c>currency</c> that
    /// was always null; the basis may legitimately be unknown — that is the
    /// source's silence, carried — but a missing currency is a defect in
    /// whatever produced the row.
    /// </remarks>
    [Theory]
    [InlineData("INR", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void An_amount_without_a_currency_is_not_an_amount(string currency, bool stated)
        => Assert.Equal(stated, new Money(840000, currency, TaxBasis.Unknown).IsStated);

    /// <summary>Unknown tax basis is a value, not a gap.</summary>
    /// <remarks>
    /// Oracle sends before-tax and Apaleo sends gross; the reference wrote both
    /// into one column with nothing recording which, so its stored revenue means
    /// a different thing per connector. Carrying <c>Unknown</c> keeps the
    /// silence visible instead of resolving it by guess.
    /// </remarks>
    [Fact]
    public void An_unknown_basis_is_carried_rather_than_guessed()
    {
        var money = new Money(840000, "INR", TaxBasis.Unknown);

        Assert.True(money.IsStated);
        Assert.Equal(TaxBasis.Unknown, money.Basis);
    }
}
