using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What a row calls a stay: its guest's name and its room's number.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two queries for the whole set, never one per row</b> — a lookup per row is
/// the N+1 a list notices first.
/// </para>
/// <para>
/// <b>No Master Data query when no stay holds a room.</b> An empty id list still
/// sends a statement, and the answer to it is already known; skipping it is
/// also what lets a test database without the <c>masterdata</c> schema exercise
/// a view whose stays have no rooms yet.
/// </para>
/// <para>
/// <b>Written for the widget's read first.</b> <c>TodayView</c> and
/// <c>WatchlistView</c> each still carry their own copy of these two lookups,
/// and they differ — Today prefers the party member marked primary, Watchlist
/// takes the first. Queued to converge on this one; noted here so the three are
/// not mistaken for a decision.
/// </para>
/// </remarks>
public sealed class StayLabels(GuestOpsDbContext db)
{
    /// <summary>The design's own words for a party nobody has named yet.</summary>
    public const string Unnamed = "Not yet named";

    /// <summary>Each stay's guest — the one marked primary, else the first the source sent.</summary>
    /// <param name="stays">The stays.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A name per stay that has one.</returns>
    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyList<RoomStay> stays, CancellationToken cancellationToken)
    {
        var ids = stays.Select(s => s.Id).Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, string>();

        var party = await db.Party
            .Where(member => ids.Contains(member.StayId))
            .Join(db.Guests, member => member.GuestId, guest => guest.Id,
                (member, guest) => new
                {
                    member.StayId,
                    Primary = member.IsPrimary == true,
                    guest.NameAsGiven,
                })
            .ToListAsync(cancellationToken);

        return party
            .Where(member => !string.IsNullOrWhiteSpace(member.NameAsGiven))
            .GroupBy(member => member.StayId)
            .ToDictionary(
                group => group.Key,
                group => (group.FirstOrDefault(member => member.Primary) ?? group.First()).NameAsGiven);
    }

    /// <summary>Each stay's room number, where it holds a room.</summary>
    /// <param name="scope">The caller — rooms are read at its property only.</param>
    /// <param name="stays">The stays.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A room number per room id.</returns>
    public async Task<IReadOnlyDictionary<Guid, string>> RoomsAsync(
        RequestScope scope, IReadOnlyList<RoomStay> stays, CancellationToken cancellationToken)
    {
        var ids = stays.Where(s => s.CurrentRoomId is not null)
            .Select(s => s.CurrentRoomId!.Value).Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, string>();

        return await db.Set<MasterDataRoom>()
            .Where(r => r.PropertyId == scope.PropertyId && ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.RoomNumber, cancellationToken);
    }
}
