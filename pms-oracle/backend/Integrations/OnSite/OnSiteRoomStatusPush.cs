namespace PmsOracle.Integrations.OnSite;

/// <summary>
/// A room-status message from the on-site OPERA agent, as it arrives.
/// </summary>
/// <remarks>
/// <para>
/// Sent by the web flavour whenever a room's state changes. Four of its fields
/// are the four independent axes of R1 — occupancy, condition, and the stays
/// touching the room — and the fifth, <see cref="NextBlocked"/>, is the one no
/// other surveyed vendor supplies and without which a strip-the-linen decision
/// cannot be made.
/// </para>
/// <para>
/// Every field is a string because that is what arrives. The date format here
/// is <c>dd-MM-yy</c> — a <b>third</b> format in one integration, after the
/// reservation push's <c>yyyy-MM-dd'T'HH:mm:ss</c> and OHIP's
/// <c>yyyy-MM-dd HH:mm:ss.S</c>. R15: the format belongs to the field, not to
/// the vendor.
/// </para>
/// </remarks>
public sealed record OnSiteRoomStatusPush
{
    /// <summary>The room number, as the PMS knows it.</summary>
    public string? RoomNo { get; init; }

    /// <summary>
    /// The stays touching this room, comma-separated in one string.
    /// </summary>
    /// <remarks>
    /// <b>A list, delivered as a string</b> — R2. The reference split it and
    /// took element zero, discarding the rest, which is how a room with a
    /// departure and an arrival on the same day became a room with one status.
    /// </remarks>
    public string? ReservationStatus { get; init; }

    /// <summary>The housekeeping condition — <c>DI</c>, <c>CL</c>, <c>IP</c>, <c>OO</c>, <c>OS</c>.</summary>
    public string? RoomStatus { get; init; }

    /// <summary>The front-office occupancy — <c>VAC</c> or <c>OCC</c>.</summary>
    public string? FOStatus { get; init; }

    /// <summary>
    /// When the room is next sold, <c>dd-MM-yy</c>.
    /// </summary>
    /// <remarks>
    /// R3. Two rooms identical on all four axes — occupied, dirty, due out —
    /// differ only here: one is sold again tonight and is made up for the next
    /// guest, the other is not and has its linen stripped. It exists in no
    /// other surveyed PMS and can be derived from no current status.
    /// </remarks>
    public string? NextBlocked { get; init; }

    /// <summary>The property this message claims — checked, never believed.</summary>
    public string? PropertyCode { get; init; }
}
