using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The business-mix widget — where this property's stays came from.
/// </summary>
/// <remarks>
/// <b>A stay whose source said nothing is left out, not counted as
/// "Direct".</b> <c>StaySource.Channel</c> is nullable because a source may
/// never have named one, and folding those into a channel would invent a
/// commercial fact — the widget would then show a mix nobody sold.
/// </remarks>
public sealed class MixView(GuestOpsDbContext db)
{
    /// <summary>Channels and markets, counted over this property's stays.</summary>
    public async Task<object?> AnswerAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var sources = db.Sources
            .Join(db.Stays.Where(s => s.PropertyId == scope.PropertyId),
                source => source.StayId, stay => stay.Id, (source, _) => source);

        return new
        {
            channels = await CountAsync(sources, source => source.Channel, cancellationToken),
            markets = await CountAsync(sources, source => source.MarketCode, cancellationToken),
        };
    }

    /// <summary>One dimension of the mix, named and counted.</summary>
    /// <remarks>
    /// Grouped in the database rather than in memory: a property's whole stay
    /// history would otherwise be carried here to be counted, and this is a
    /// widget that refreshes on a tick.
    /// </remarks>
    private static async Task<object[]> CountAsync(
        IQueryable<Domain.StaySource> sources,
        System.Linq.Expressions.Expression<Func<Domain.StaySource, string?>> dimension,
        CancellationToken cancellationToken)
    {
        var counted = await sources
            .Select(dimension)
            .Where(value => value != null && value != "")
            .GroupBy(value => value!)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(row => row.Count)
            .Take(6)
            .ToListAsync(cancellationToken);

        return [.. counted.Select(row => (object)new { name = row.Name, count = row.Count })];
    }
}
