using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Vocabularies;

/// <summary>
/// The on-site front-office occupancy codes — whether anybody is in the room.
/// </summary>
/// <remarks>
/// Two values, and this axis is <b>independent of cleanliness</b> (R1). A room
/// can be vacant and dirty, occupied and clean, or vacant and out of order, and
/// no arrangement of one axis implies the other. Collapsing them is the
/// modelling mistake that cannot be undone downstream, which is why occupancy
/// has its own enum on the contract and its own vocabulary here.
/// </remarks>
public static class FrontOfficeCodes
{
    private static readonly Dictionary<string, Occupancy> Meanings = new(StringComparer.Ordinal)
    {
        ["VAC"] = Occupancy.Vacant,
        ["OCC"] = Occupancy.Occupied,
    };

    /// <summary>Every code this connector declares for the front-office axis.</summary>
    public static IReadOnlyCollection<string> Declared => Meanings.Keys;

    /// <summary>Read one <c>FOStatus</c> value.</summary>
    /// <param name="sourceValue">The code exactly as the agent sent it.</param>
    /// <returns>The occupancy, or an unrecognised reading carrying the value.</returns>
    public static Reading<Occupancy> Read(string sourceValue) =>
        Meanings.TryGetValue(sourceValue, out var meaning)
            ? Reading<Occupancy>.Of(meaning)
            : Reading<Occupancy>.Unrecognised(sourceValue);
}
