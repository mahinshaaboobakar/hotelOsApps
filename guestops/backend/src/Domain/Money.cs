namespace HotelOS.GuestOps.Domain;

/// <summary>Whether an amount includes tax.</summary>
/// <remarks>
/// <c>Unknown</c> is a value, not a gap to be filled in. A source sometimes
/// does not say, and R19's whole lesson is that guessing is what made the
/// reference's stored revenue unrecoverable — Oracle's <c>amountBeforeTax</c>
/// and Apaleo's <c>totalGrossAmount</c> went into one column with nothing
/// recording which.
/// </remarks>
public enum TaxBasis
{
    /// <summary>The source did not say.</summary>
    Unknown = 0,

    /// <summary>The amount excludes tax.</summary>
    Net = 1,

    /// <summary>The amount includes tax.</summary>
    Gross = 2,
}

/// <summary>
/// An amount of money, with everything needed to know what it means — R19.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three things or it is not an amount</b>: the value, its currency, and
/// whether tax is included. The system this replaces carried a <c>float</c>
/// beside a <c>currency</c> that was always null, and one vendor truncated its
/// amounts to <c>int</c> on the way through, discarding the minor units it had
/// been given.
/// </para>
/// <para>
/// <b>Minor units, as an integer.</b> 1840000 is 18,400.00 in a two-decimal
/// currency. A binary float cannot hold a price exactly, and a price that is
/// almost right is a reconciliation nobody can close.
/// </para>
/// <para>
/// A <b>record class, not a struct</b>, because EF Core owns it as part of the
/// row it belongs to and an owned type is a reference type. The record keeps
/// the value semantics that matter — two amounts of the same minor units,
/// currency and basis are the same amount.
/// </para>
/// </remarks>
/// <param name="MinorUnits">The value, in the currency's smallest unit.</param>
/// <param name="Currency">ISO 4217. An amount without one cannot be summed or reported.</param>
/// <param name="Basis">Whether tax is included.</param>
public sealed record Money(long MinorUnits, string Currency, TaxBasis Basis)
{
    /// <summary>Whether this is an amount at all.</summary>
    /// <remarks>
    /// A currency is what makes the number mean something. The basis may be
    /// <see cref="TaxBasis.Unknown"/> — that is the source's silence, faithfully
    /// carried — but a missing currency is a defect in whatever produced it.
    /// </remarks>
    public bool IsStated => !string.IsNullOrWhiteSpace(Currency);
}
