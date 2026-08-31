using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Vocabularies;

/// <summary>
/// Which half of a two-part on-site message a status value belongs to.
/// </summary>
/// <remarks>
/// Requirement R6, and the reason this flavour needs a type the cloud one does
/// not. The on-site agent delivers a check-in as <b>two messages that are not
/// the same message differently cased</b>: <c>"Checked In"</c> carries the
/// contact details and the departure date, <c>"CHECKED IN"</c> carries the room
/// number, and neither is a publishable fact alone. The reference discovered
/// this in production and joined them on surname, first name and arrival date.
/// </remarks>
public enum OnSiteMessagePart
{
    /// <summary>The message is a whole fact on its own.</summary>
    Whole,

    /// <summary>Carries contact details and the departure date — <c>"Checked In"</c>.</summary>
    ContactHalf,

    /// <summary>Carries the room number — <c>"CHECKED IN"</c>.</summary>
    RoomHalf,
}

/// <summary>
/// What one on-site status value means, and which part of a message it is.
/// </summary>
/// <param name="Lifecycle">Where the stay has reached.</param>
/// <param name="Part">Whole fact, or one half of a pair awaiting its partner.</param>
public readonly record struct OnSiteStatus(StayLifecycle Lifecycle, OnSiteMessagePart Part);

/// <summary>
/// The on-site OPERA status vocabulary, shared by <c>oracle-onpremise</c> and
/// <c>oracle-web</c>.
/// </summary>
/// <remarks>
/// <para>
/// Ten values, and the casing is <b>significant rather than sloppy</b> — see
/// <see cref="OnSiteMessagePart"/>. Reading a status here therefore yields both
/// the meaning and the part, so a caller cannot obtain a check-in without also
/// learning that it may be half of one. That is the requirement expressed as a
/// type rather than as a warning in a comment.
/// </para>
/// <para>
/// <c>Due In</c>, <c>DUE IN</c> and <c>OT</c> all mean booked; the reference
/// normalised the three to one before doing anything else, which is the one
/// piece of its status handling worth keeping verbatim.
/// </para>
/// </remarks>
public static class OnSiteStayStatus
{
    private static readonly Dictionary<string, OnSiteStatus> Meanings = new(StringComparer.Ordinal)
    {
        ["Due In"] = new(StayLifecycle.Booked, OnSiteMessagePart.Whole),
        ["DUE IN"] = new(StayLifecycle.Booked, OnSiteMessagePart.Whole),
        ["OT"] = new(StayLifecycle.Booked, OnSiteMessagePart.Whole),

        // The pair. Same lifecycle, different halves, joined before publication.
        ["Checked In"] = new(StayLifecycle.CheckedIn, OnSiteMessagePart.ContactHalf),
        ["CHECKED IN"] = new(StayLifecycle.CheckedIn, OnSiteMessagePart.RoomHalf),

        ["CHECKED OUT"] = new(StayLifecycle.CheckedOut, OnSiteMessagePart.Whole),
        ["CANCELLED"] = new(StayLifecycle.Cancelled, OnSiteMessagePart.Whole),
        ["DUE OUT"] = new(StayLifecycle.DueOut, OnSiteMessagePart.Whole),
        ["PENDING"] = new(StayLifecycle.Pending, OnSiteMessagePart.Whole),
        ["WAITLIST"] = new(StayLifecycle.Waitlisted, OnSiteMessagePart.Whole),
    };

    /// <summary>Every source value this connector declares for the on-site flavours.</summary>
    /// <remarks>
    /// This is the list section 4 of the setup sheet shows the hotel's OPERA
    /// operator, and it comes from the same table the parser reads.
    /// </remarks>
    public static IReadOnlyCollection<string> Declared => Meanings.Keys;

    /// <summary>Read one on-site <c>Status</c> value.</summary>
    /// <param name="sourceValue">The value exactly as the agent sent it.</param>
    /// <returns>
    /// The meaning and message part, or an unrecognised reading carrying
    /// <paramref name="sourceValue"/>.
    /// </returns>
    /// <remarks>
    /// Ordinal, and deliberately not case-insensitive: folding the casing here
    /// would erase the distinction between the two halves of a check-in and
    /// turn requirement R6 into a bug that only appears when a guest's contact
    /// details go missing.
    /// </remarks>
    public static Reading<OnSiteStatus> Read(string sourceValue) =>
        Meanings.TryGetValue(sourceValue, out var meaning)
            ? Reading<OnSiteStatus>.Of(meaning)
            : Reading<OnSiteStatus>.Unrecognised(sourceValue);
}
