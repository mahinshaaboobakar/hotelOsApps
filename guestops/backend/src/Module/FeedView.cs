using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The from-the-PMS widget — what the feed has sent, and what it could not place.
/// </summary>
/// <remarks>
/// <b>Absence and an ageing time are different facts and stay different
/// values.</b> The widget contract says so outright: a property nobody has ever
/// sent a fact to has not "gone quiet". So <c>lastFactAt</c> is null only when
/// there is no feed mark at all, and a mark that is hours old is returned as
/// the time it is — the widget decides what to say about the gap, not this.
/// </remarks>
public sealed class FeedView(GuestOpsDbContext db)
{
    /// <summary>The feed's counters, and the facts it is holding.</summary>
    public async Task<object?> AnswerAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var mark = await db.FeedMarks
            .Where(m => m.PropertyId == scope.PropertyId)
            .OrderByDescending(m => m.LastFactAt)
            .FirstOrDefaultAsync(cancellationToken);

        var held = await db.HeldFacts
            .Where(f => f.PropertyId == scope.PropertyId && f.ResolvedAt == null)
            .OrderByDescending(f => f.ReceivedAt)
            .Take(6)
            .ToListAsync(cancellationToken);

        return new
        {
            // Stays this feed created today. Counted from the stays rather than
            // from a running tally, because a tally is a second number that can
            // disagree with the list a person opens next.
            newToday = await db.Stays.CountAsync(
                s => s.PropertyId == scope.PropertyId
                    && s.Origin == Domain.RecordOrigin.Pms,
                cancellationToken),

            held = await db.HeldFacts.CountAsync(
                f => f.PropertyId == scope.PropertyId && f.ResolvedAt == null,
                cancellationToken),

            lastFactAt = mark?.LastFactAt.ToString("HH:mm"),

            facts = held.Select(fact => new
            {
                reason = fact.Reason.ToString(),
                source = fact.IntegrationId,
                at = fact.ReceivedAt.ToString("HH:mm"),

                // The held fact names no stay — that is why it is held. The
                // widget's `stay` is the tap-through target, and an id invented
                // here would open a page for a stay that does not exist.
                stay = (string?)null,
            }).ToArray(),
        };
    }
}
