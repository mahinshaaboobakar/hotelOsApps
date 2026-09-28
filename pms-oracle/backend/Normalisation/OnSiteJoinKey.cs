using System.Globalization;
using PmsOracle.Integrations.OnSite;
using PmsOracle.Vocabularies;

namespace PmsOracle.Normalisation;

/// <summary>
/// What the two halves of an on-site check-in are paired on.
/// </summary>
/// <param name="Surname">Family name, as the agent sent it.</param>
/// <param name="FirstName">Given name, as the agent sent it.</param>
/// <param name="ArrivalDate">The arrival date, as a date rather than a string.</param>
/// <remarks>
/// <para>
/// <b>This is entity resolution by name, and it is not a design choice.</b> The
/// on-site agent sends a check-in as two messages, one carrying contact details
/// and one carrying the room (R6), and it supplies no correlation identifier
/// for them — the reservation id is absent from at least one half. Three fields
/// is what there is.
/// </para>
/// <para>
/// The risk is real and worth stating where the code is: two guests with the
/// same name arriving the same day at the same property would join wrongly, and
/// a wrong join merges two stays. It is narrowed by the property and the
/// arrival date, and it is bounded by the pending-join window rather than left
/// open indefinitely — but it is not eliminated, and no arrangement of these
/// three fields eliminates it.
/// </para>
/// <para>
/// <b>The comparison is exact, and that is not the same decision the
/// vocabularies made.</b> Names are trimmed and never case-folded, so
/// <c>"Menon"</c> and <c>"MENON"</c> are two keys. Next door,
/// <c>StringComparer.Ordinal</c> is <i>load-bearing</i> — <c>"Checked In"</c>
/// and <c>"CHECKED IN"</c> are two halves with different meanings (R6), and
/// folding case there would destroy the very join this key serves; those
/// vocabularies absorb the source's case-instability by declaring every
/// observed spelling instead.
/// </para>
/// <para>
/// <b>Here nothing decides it.</b> The study establishes case-instability for
/// STATUSES and says nothing about names, no test covers it, and the two halves
/// travel different code paths in the agent — which is exactly where a spelling
/// could differ. What can be said is the direction: an unmatched key expires as
/// <c>join_window_expired</c>, which is visible, while a wrong join merges two
/// stays. It fails safe. <b>Whether that is why it was written this way is not
/// recorded, and a later reader adding <c>OrdinalIgnoreCase</c> "for
/// robustness" would be changing join behaviour with nothing to catch it.</b>
/// </para>
/// <para>
/// The reference did the same correlation with a Mongo query written inline at
/// the call site, over its own private copy of the data. Naming it as a type
/// does not make it safer; it makes it visible, testable, and something a
/// future connector version can replace the moment the agent offers a better
/// key.
/// </para>
/// </remarks>
public readonly record struct OnSiteJoinKey(
    string Surname,
    string FirstName,
    DateOnly ArrivalDate)
{
    /// <summary>
    /// Build a join key, if the message carries all three parts.
    /// </summary>
    /// <param name="surname">The <c>Surname</c> field.</param>
    /// <param name="firstName">The <c>FirstName</c> field.</param>
    /// <param name="arrivalDate">The parsed arrival date.</param>
    /// <returns>The key, or <c>null</c> when a part is missing.</returns>
    /// <remarks>
    /// A key with a blank name would match every other message with a blank
    /// name — which is to say it would join unrelated guests. Refusing to build
    /// one is what keeps that from being expressible.
    /// </remarks>
    public static OnSiteJoinKey? For(string? surname, string? firstName, DateOnly? arrivalDate)
    {
        if (string.IsNullOrWhiteSpace(surname)
            || string.IsNullOrWhiteSpace(firstName)
            || arrivalDate is null)
        {
            return null;
        }

        return new OnSiteJoinKey(surname.Trim(), firstName.Trim(), arrivalDate.Value);
    }

    /// <summary>Whether one on-site message is half of a check-in, and which half.</summary>
    /// <param name="push">The message as it arrived.</param>
    /// <returns>Its key and part, or <c>null</c> when it is whole.</returns>
    /// <remarks>
    /// <para>
    /// <b>One home, two callers.</b> This decision was
    /// <c>OracleOnSiteAdapter.JoinFor</c> alone, and the Connector Protocol's
    /// <c>join</c> needs the same answer from a path that does not construct
    /// that adapter — which now requires settings and an exponent. A second
    /// copy would be two answers to *is this a part*, and the two halves of a
    /// check-in would stop pairing the day they disagreed.
    /// </para>
    /// <para>
    /// <b>Every <c>null</c> here means whole, never rejected.</b> A blank
    /// status, an unrecognised one, or a name the key cannot be built from all
    /// return whole, because deciding a message is half of something is a
    /// claim, and the normaliser is what refuses a message with the field
    /// named. Guessing a part here would decide before anything read what the
    /// message says.
    /// </para>
    /// </remarks>
    public static (string Key, string Part)? Candidate(OnSitePush push)
    {
        ArgumentNullException.ThrowIfNull(push);

        if (string.IsNullOrWhiteSpace(push.Status)
            || !OnSiteStayStatus.Read(push.Status).TryGet(out var status)
            || status.Part == OnSiteMessagePart.Whole)
        {
            return null;
        }

        // Invariant — NUM-Q4, ADR 0174's boundary. The agent posts what OPERA
        // holds, and the reference reads Oracle's dates with explicit machine
        // patterns throughout (`providers/oracle/cloud/services/impl/
        // OracleCloudReservationServiceImpl.java:171` — `yyyy-MM-dd HH:mm:ss.S`).
        // A culture-sensitive parse would make the same bytes mean two different
        // days on two servers, so this key would stop matching for a hotel whose
        // server was set up differently — silently, and only for some dates.
        var arrival = DateOnly.TryParse(
            push.ArrivalDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : (DateOnly?)null;

        return For(push.Surname, push.FirstName, arrival) is { } key
            ? ($"{key.Surname}|{key.FirstName}|{key.ArrivalDate:yyyy-MM-dd}", status.Part.ToString())
            : null;
    }
}
