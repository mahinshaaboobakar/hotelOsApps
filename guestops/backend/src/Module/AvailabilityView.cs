using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What is free to sell — gold frame 14.
/// </summary>
/// <remarks>
/// <para>
/// <b>Computed, never a table anyone feeds</b> (GUEST-Q7). Four inputs and
/// three owners: the rooms are Master Data's, out-of-order is EngineeringOps's
/// heard as an event, and the stays and stop-sell are ours. This projection
/// adds no fifth input and stores nothing.
/// </para>
/// <para>
/// <b>One day's answer, not a range's.</b> The service answers per type per
/// date; frame 14 asks a single question — <i>what can I sell for these
/// dates</i> — so this takes the <b>worst</b> day in the window rather than the
/// first. Taking the first would offer a room that is free on the 3rd and sold
/// on the 5th, which is a booking that cannot be honoured.
/// </para>
/// </remarks>
public sealed class AvailabilityView(
    GuestOpsDbContext db,
    AvailabilityService availability)
{
    /// <summary>The answer for the dates the bundle asked about.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var days = await availability.GetAsync(scope, from, to, [], cancellationToken);
        var names = await NamesAsync(days, cancellationToken);

        var byType = days
            .GroupBy(day => day.RoomTypeId)
            .Select(group => Worst(group.Key, [.. group]))
            .OrderBy(type => names.TryGetValue(type.RoomTypeId, out var name) ? name : "")
            .ToList();

        return new
        {
            query = new
            {
                arrive = from.ToString("d MMM"),
                depart = to.ToString("d MMM"),

                // The party is the desk's own input and nothing here holds one
                // yet. It is stated as the search this answer was computed for,
                // which is one room — never a number of adults nobody entered.
                party = "1 room",
            },

            mode = await ConnectedAsync(scope, cancellationToken)
                ? "PMS-connected — Opera writes the lifecycle"
                : "Standalone — this property is the book",

            types = byType.Select(type => Row(type, names)).ToArray(),
        };
    }

    /// <summary>
    /// The day in the window with the least to sell.
    /// </summary>
    /// <remarks>
    /// Each count is taken from that same worst day rather than each being its
    /// own maximum across the window: a row whose numbers came from four
    /// different days would not add up, and an operator checking the arithmetic
    /// would be right that it is wrong.
    /// </remarks>
    private static TypeAvailability Worst(
        Guid typeId, IReadOnlyList<TypeAvailability> days)
    {
        var worst = days[0];

        foreach (var day in days)
        {
            if (day.Free < worst.Free)
            {
                worst = day;
            }
        }

        return worst;
    }

    /// <summary>One room type's row.</summary>
    private static object Row(
        TypeAvailability type, IReadOnlyDictionary<Guid, string> names)
        => new
        {
            roomType = names.TryGetValue(type.RoomTypeId, out var name) ? name : null,

            // **No rate, and it is absent rather than zero** — GUEST-Q7 rules
            // pricing out of this round, and nothing in this schema holds a
            // published rate per type. A price on this screen would be a number
            // no system produced, in the one column a guest is quoted from.
            rate = (string?)null,

            total = type.TotalRooms,
            sold = type.HeldByStays,

            outOfOrder = type.OutOfOrder,
            outOfOrderBy = type.OutOfOrder > 0 ? "EngineeringOps" : null,

            stopSold = type.StopSold,

            // The reason is on the stop-sell record and this projection reads
            // the counts rather than the rows, so it says that they are held
            // without claiming to know why. The alternative — inventing a
            // reason — is what the attribution exists to prevent.
            stopSoldWhy = type.StopSold > 0 ? "held back" : null,

            free = type.Free,
        };

    /// <summary>The room type names, read from Master Data and never copied.</summary>
    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyList<TypeAvailability> days, CancellationToken cancellationToken)
    {
        var ids = days.Select(day => day.RoomTypeId).Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await db.Set<MasterDataRoomTypeName>()
            .Where(type => ids.Contains(type.Id))
            .ToDictionaryAsync(type => type.Id, type => type.Name, cancellationToken);
    }

    /// <summary>Whether a PMS writes this property's lifecycle.</summary>
    /// <remarks>
    /// A feed mark exists only once a fact has arrived through the Hub, so this
    /// is a fact about the property rather than a setting somebody ticked.
    /// </remarks>
    private async Task<bool> ConnectedAsync(
        RequestScope scope, CancellationToken cancellationToken)
        => await db.FeedMarks.AnyAsync(
            mark => mark.PropertyId == scope.PropertyId, cancellationToken);
}
