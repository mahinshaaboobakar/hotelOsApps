namespace HotelOS.GuestOps.Domain;

/// <summary>
/// How <see cref="ReportingScope"/> is spelled on a wire — §2.8.
/// </summary>
/// <remarks>
/// <para>
/// <b>Here because a second door was about to repeat it.</b> The gRPC surface
/// held the parse and the render privately, which was correct while it was the
/// only caller; the module door's write would have been a second copy, and
/// <i>"a copy is not a copy for long — two status mappings drift and one service
/// answers one thing where the other answers another"</i>. Following
/// <c>jobs/Domain/JobVocabulary.cs</c>, which ADR 0356's own owners section
/// names as the precedent for this split.
/// </para>
/// <para>
/// <b>Parse and render are one pair deliberately.</b> A vocabulary with two
/// homes for its two directions drifts in the half nobody reads: a value the
/// writer accepts and the reader cannot spell is a row no screen can draw.
/// </para>
/// </remarks>
public static class ReportingScopes
{
    /// <summary>Only guests from outside the property's home country.</summary>
    public const string FromOutside = "from_outside";

    /// <summary>Every guest, wherever they are from.</summary>
    public const string EveryGuest = "every_guest";

    /// <summary>Both spellings, for a message that has to list them.</summary>
    public static readonly IReadOnlyList<string> All = [FromOutside, EveryGuest];

    /// <summary>The scope a caller named.</summary>
    /// <param name="value">The wire spelling.</param>
    /// <returns>The scope.</returns>
    /// <remarks>
    /// <b>An unrecognised value is refused rather than defaulted.</b> Defaulting
    /// to from-outside would quietly narrow a property's obligation, and the
    /// property would not find out until an inspection.
    /// </remarks>
    /// <exception cref="HotelOS.Platform.InvalidRequestException">
    /// Any spelling this application does not have.
    /// </exception>
    public static ReportingScope Parse(string value) => value switch
    {
        FromOutside => ReportingScope.FromOutside,
        EveryGuest => ReportingScope.EveryGuest,
        _ => throw new HotelOS.Platform.InvalidRequestException(
            $"reporting_applies_to must be {FromOutside} or {EveryGuest}"),
    };

    /// <summary>How a scope is spelled on the wire.</summary>
    /// <param name="scope">The stored scope.</param>
    /// <returns>Its wire spelling.</returns>
    public static string Wire(ReportingScope scope)
        => scope == ReportingScope.EveryGuest ? EveryGuest : FromOutside;
}
