namespace HotelOS.GuestOps.Domain;

/// <summary>
/// When this property last heard anything at all from an integration.
/// </summary>
/// <remarks>
/// <para>
/// <b>Recorded at arrival, before any decision.</b> Every inbound fact stamps
/// this — one that creates a stay, one that settles silently against a standing
/// override, and one that is held. The mark answers *"is the feed alive"*, and
/// that question has nothing to do with whether the last fact was useful.
/// </para>
/// <para>
/// <b>Why it exists at all.</b> <see cref="HeldFact.ReceivedAt"/> was the only
/// arrival timestamp in this domain, and a held fact is a fact that
/// <i>failed</i> — so a property whose feed is perfectly healthy has no held
/// facts and therefore no timestamp, and a widget reading that reports "never"
/// exactly when the wire is fine. The signal was inverted: it looked worst when
/// things were best. Ruled 2026-09-03; this is the mark that is not.
/// </para>
/// <para>
/// <b>Per integration, not per property.</b> A property with a PMS and a
/// channel manager has two feeds, and one going quiet is invisible in a single
/// combined stamp — which is the same inversion one level up.
/// </para>
/// </remarks>
public class InboundFeedMark
{
    public Guid PropertyId { get; set; }

    /// <summary>The connector that sent it — ADR 0020's closed set.</summary>
    public string IntegrationId { get; set; } = string.Empty;

    /// <summary>
    /// When the last fact arrived, whatever became of it.
    /// </summary>
    /// <remarks>
    /// Not nullable: a mark exists because a fact arrived, so there is no state
    /// in which this row is present and the time is unknown. A property that
    /// has never been sent anything has <b>no row</b> — which a reader must
    /// distinguish from a feed that has gone quiet, and can, because the two
    /// are absence and an ageing timestamp rather than one value meaning both.
    /// </remarks>
    public DateTimeOffset LastFactAt { get; set; }
}
