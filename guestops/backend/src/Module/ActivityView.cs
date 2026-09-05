using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Everything that happened to a stay, with who said it — gold frame 4.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the screen that answers a complaint.</b> Why does it say 214? Who
/// moved the departure? Was the guest's ID taken? A list showing only this
/// application's own facts would answer none of the questions a duty manager
/// asks at 9 p.m., because half the story belongs to Opera, Room Care and Jobs.
/// </para>
/// <para>
/// <b>It is read from our own event store, and nothing is copied.</b> Every row
/// here is a <c>StoredEvent</c> this service appended in the same transaction as
/// the change it describes — so the list cannot disagree with the stay, and it
/// cannot be missing a change that succeeded.
/// </para>
/// <para>
/// <b>The other applications' rows are not here yet, and their absence is the
/// honest state.</b> The design draws Room Care's and Jobs' own records
/// resolved live through the Context Service and <i>stored nowhere here</i> —
/// which is exactly why they cannot be produced from this table. They arrive
/// when this application can call Context; until then the list is complete
/// about what GuestOps did and says nothing about what anyone else did, rather
/// than presenting itself as the whole story.
/// </para>
/// </remarks>
public sealed class ActivityView(GuestOpsDbContext db)
{
    /// <summary>One stay's history.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        var events = await db.Set<StoredEvent>()
            .Where(stored => stored.PropertyId == scope.PropertyId
                && stored.AggregateType == "stay"
                && stored.AggregateId == stayId)
            .OrderBy(stored => stored.OccurredAt)
            .Select(stored => new
            {
                stored.EventType,
                stored.OccurredAt,
                stored.Source,
                stored.ActorType,
            })
            .ToListAsync(cancellationToken);

        return new
        {
            // Only the filters this projection can honour. `Opera` and `Other
            // apps` are in the design and would return nothing here, and a
            // filter that is always empty teaches an operator that a source has
            // gone quiet when it was never being read.
            filters = new object[]
            {
                new { label = "Everything", on = true },
                new { label = "Ours", on = false },
            },

            entries = events.Select(one => new
            {
                date = one.OccurredAt.ToString("d MMM"),
                time = one.OccurredAt.ToString("HH:mm"),
                who = Who(one.ActorType, one.Source),
                what = What(one.EventType),

                // The event's own name, so a row a person does not recognise
                // can still be matched against a log. It is never a sentence
                // this projection made up about what the event meant.
                detail = one.EventType,
                disagrees = one.EventType.EndsWith(".disagreed", StringComparison.Ordinal),
            }).ToArray(),
        };
    }

    /// <summary>
    /// Who said it, as one of the design's marks.
    /// </summary>
    /// <remarks>
    /// <b>The actor's name is not here, and it is left absent.</b> The design
    /// draws <i>Anitha M.</i>; the event carries a user id, and turning that
    /// into a name means Identity, which this projection does not call. A row
    /// therefore says <i>a person</i> rather than naming one — an initial and a
    /// surname invented from an id would be the worst kind of wrong on the
    /// screen that exists to answer <i>who did this?</i>.
    /// </remarks>
    private static object Who(int actorType, string source)
        => actorType switch
        {
            // A person, unnamed until Identity is reachable.
            1 => new { mark = "override", text = "a person" },

            // Something that arrived through the Hub — the source names it.
            2 => new { mark = "pms", text = string.IsNullOrWhiteSpace(source) ? "a feed" : source },

            _ => new { mark = "other", text = string.IsNullOrWhiteSpace(source) ? "the system" : source },
        };

    /// <summary>
    /// The event, in the design's words.
    /// </summary>
    /// <remarks>
    /// Mapped for the ones the design names and passed through otherwise. An
    /// unmapped event shows its subject rather than being dropped: a history
    /// that silently omits what it does not recognise is the one thing this
    /// screen must not do.
    /// </remarks>
    private static string What(string eventType)
        => eventType switch
        {
            "stay.created" => "Booked",
            "stay.amended" => "Amended",
            "stay.assigned" => "Room assigned",
            "stay.checked_in" => "Checked in",
            "stay.checked_out" => "Checked out",
            "stay.cancelled" => "Cancelled",
            "stay.no_show" => "Recorded as a no-show",
            "stay.disagreed" => "The source reported something different",
            "registration.captured" => "Registration captured",
            "request.logged" => "Request logged",
            _ => eventType,
        };
}
