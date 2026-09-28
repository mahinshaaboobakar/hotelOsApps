using System.Globalization;

namespace PmsOracle.Normalisation;

/// <summary>
/// How the on-site agent spells a date, and how it is read.
/// </summary>
/// <remarks>
/// <para>
/// <b>One home, because two decisions must agree about it.</b>
/// <see cref="OnSiteNormaliser"/> asks whether a push can be normalised;
/// <see cref="OnSiteJoinKey.Candidate"/> asks whether it is half of a
/// check-in. Those are different questions and they read the same field, so a
/// date one of them accepts and the other refuses is a message the connector
/// declares a <c>member</c> and then cannot normalise.
/// </para>
/// <para>
/// That is not hypothetical: the two were written apart, the join key parsed
/// loosely and the normaliser exactly, and an arrival date of
/// <c>2026-08-31</c> — an ISO day with no time — was accepted by one and
/// refused by the other. The Hub holds a declared half for thirty minutes, so
/// the refusal would have surfaced as <c>join_window_expired</c>: *a partner
/// never came*, when in truth this half was never readable.
/// <c>a_date_the_normaliser_cannot_read_is_never_half_of_a_check_in</c> is
/// where that fails if the two ever diverge again.
/// </para>
/// <para>
/// <b>Exact, and invariant.</b> The agent posts what OPERA holds, in one
/// shape, and the reference reads Oracle's dates with explicit machine
/// patterns throughout (<c>providers/oracle/cloud/services/impl/
/// OracleCloudReservationServiceImpl.java:171</c>). A culture-sensitive read
/// would make the same bytes mean two different days on two servers — NUM-Q4,
/// ADR 0174's boundary. A loose read is the weaker fault of the two: it
/// accepts shapes the agent never sends and gives them a meaning nobody
/// checked.
/// </para>
/// <para>
/// <b>Not <c>Iso8601.Day</c> (ADR 0238).</b> That reads <c>yyyy-MM-dd</c>,
/// the platform's own spelling. This is the vendor's, and it carries a time —
/// the two are different formats and mapping the vendor's onto the platform's
/// is what the normaliser does after this returns.
/// </para>
/// </remarks>
public static class OnSiteDateReading
{
    /// <summary>The shape the on-site flavours send — R12.</summary>
    public const string Format = "yyyy-MM-dd'T'HH:mm:ss";

    /// <summary>The day the agent sent, or <c>null</c> where it sent nothing readable.</summary>
    /// <param name="value">The field as it arrived.</param>
    /// <returns>The day, or <c>null</c>.</returns>
    public static DateOnly? Read(string? value) =>
        DateTime.TryParseExact(
            value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? DateOnly.FromDateTime(parsed)
            : null;
}
