using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Vocabularies;

/// <summary>
/// The housekeeping condition of a room, in both Oracle spellings.
/// </summary>
/// <remarks>
/// <para>
/// The on-site flavours send two-letter codes and OHIP sends words, for the
/// same axis. Both are here because they are one vocabulary read from two
/// wires — and keeping them together is what makes it visible that
/// <c>"DI"</c> and <c>"Dirty"</c> are the same condition rather than two.
/// </para>
/// <para>
/// <b>OHIP's empty string is a value, not an absence.</b> A room whose
/// housekeeping status is <c>""</c> is a <b>pick-up</b> — a light tidy, a real
/// state the floor works from — and mapping blank to "unknown" loses it (R5,
/// as amended). It is the reason the contract carries
/// <c>ROOM_CONDITION_PICK_UP</c> beside <c>ROOM_CONDITION_UNSPECIFIED</c>:
/// unspecified means the source did not send this axis at all.
/// </para>
/// <para>
/// <b>OHIP also puts occupancy words in this vocabulary</b> — its housekeeping
/// status can read <c>Vacant</c> or <c>Occupied</c>, which belong to the
/// front-office axis. They are declared here so a message carrying one is not
/// rejected, and they are read as <i>occupancy</i> rather than condition, which
/// is what <see cref="ReadCloudOccupancy"/> exists for. A source mixing two
/// axes into one field does not make them one axis.
/// </para>
/// </remarks>
public static class RoomConditionCodes
{
    private static readonly Dictionary<string, RoomCondition> OnSite = new(StringComparer.Ordinal)
    {
        ["DI"] = RoomCondition.Dirty,
        ["CL"] = RoomCondition.Clean,
        ["IP"] = RoomCondition.Inspected,
        ["OO"] = RoomCondition.OutOfOrder,
        ["OS"] = RoomCondition.OutOfService,
    };

    private static readonly Dictionary<string, RoomCondition> Cloud = new(StringComparer.Ordinal)
    {
        ["Dirty"] = RoomCondition.Dirty,
        ["Clean"] = RoomCondition.Clean,
        ["Inspected"] = RoomCondition.Inspected,
        ["OutOfOrder"] = RoomCondition.OutOfOrder,
        ["OutOfService"] = RoomCondition.OutOfService,

        // Not a missing value. See the remarks.
        [""] = RoomCondition.PickUp,
    };

    private static readonly Dictionary<string, Occupancy> CloudOccupancy = new(StringComparer.Ordinal)
    {
        ["Vacant"] = Occupancy.Vacant,
        ["Occupied"] = Occupancy.Occupied,
    };

    /// <summary>Every on-site code this connector declares.</summary>
    public static IReadOnlyCollection<string> DeclaredOnSite => OnSite.Keys;

    /// <summary>Every OHIP condition word this connector declares, including the empty one.</summary>
    public static IReadOnlyCollection<string> DeclaredCloud => Cloud.Keys;

    /// <summary>Read an on-site <c>RoomStatus</c> code.</summary>
    /// <param name="sourceValue">The code exactly as the agent sent it.</param>
    /// <returns>The condition, or an unrecognised reading carrying the value.</returns>
    public static Reading<RoomCondition> ReadOnSite(string sourceValue) =>
        OnSite.TryGetValue(sourceValue, out var meaning)
            ? Reading<RoomCondition>.Of(meaning)
            : Reading<RoomCondition>.Unrecognised(sourceValue);

    /// <summary>Read an OHIP housekeeping word as a condition.</summary>
    /// <param name="sourceValue">The word exactly as OHIP sent it — <c>""</c> included.</param>
    /// <returns>The condition, or an unrecognised reading carrying the value.</returns>
    /// <remarks>
    /// Returns unrecognised for <c>Vacant</c> and <c>Occupied</c>: they are real
    /// OHIP values and they are not conditions, so the caller reads them through
    /// <see cref="ReadCloudOccupancy"/> instead of receiving a wrong answer here.
    /// </remarks>
    public static Reading<RoomCondition> ReadCloud(string sourceValue) =>
        Cloud.TryGetValue(sourceValue, out var meaning)
            ? Reading<RoomCondition>.Of(meaning)
            : Reading<RoomCondition>.Unrecognised(sourceValue);

    /// <summary>
    /// Read an OHIP housekeeping word that is really an occupancy.
    /// </summary>
    /// <param name="sourceValue">The word exactly as OHIP sent it.</param>
    /// <returns>The occupancy, or <c>null</c> when the word is not one.</returns>
    public static Occupancy? ReadCloudOccupancy(string sourceValue) =>
        CloudOccupancy.TryGetValue(sourceValue, out var meaning) ? meaning : null;
}
