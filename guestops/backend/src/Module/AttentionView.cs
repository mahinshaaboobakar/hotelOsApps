using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The Attention screen — the things a person has to decide.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not a defect log.</b> Every member is an ordinary condition of running a
/// hotel against a PMS feed: a value the feed disagrees with, a fact it could
/// not place. The screen's own sentence is <i>"nothing here decides itself"</i>,
/// and that is the whole design — these are queued decisions, not errors.
/// </para>
/// <para>
/// Two kinds today, because two kinds exist in the schema. The gold draws a
/// third — an unconfirmed candidate link — and <c>StayLinkCandidate</c> holds
/// it; it is not projected here because the card needs both stays' names and
/// the held fact's payload parsed, which is its own round rather than a line
/// in this one. <b>Absent rather than half-drawn</b>: a card missing the values
/// a person decides on is worse than a card that is not there.
/// </para>
/// </remarks>
public sealed class AttentionView(GuestOpsDbContext db)
{
    /// <summary>One page of the cards, newest first across both sources.</summary>
    /// <remarks>
    /// <para>
    /// <b>Paged, and the merge is why it took thought</b> — <c>64</c> §8 asks
    /// every list screen for a pager, and this list is two lists. It used to
    /// take twenty of each and concatenate them, which put every disagreement
    /// above every held fact regardless of when either happened, and silently
    /// dropped the twenty-first of either.
    /// </para>
    /// <para>
    /// <b>Ordered by when it happened, then paged over the union.</b> To serve
    /// page <c>n</c> of a merge, at most <c>(n + 1) × size</c> rows of each
    /// source can contribute — anything further down either list is behind
    /// <c>(n + 1) × size</c> newer rows and cannot reach this page. So each
    /// source is asked for exactly that many, the two are merged by time, and
    /// the page is taken from the result.
    /// </para>
    /// <para>
    /// <b>The total counts both</b>, from the database rather than from what was
    /// fetched: a total derived from a capped read is a number that stops
    /// growing at the cap, which is the pager claiming a list is shorter than it
    /// is.
    /// </para>
    /// </remarks>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Paging.Window page, CancellationToken cancellationToken)
    {
        var mine = db.Stays.Where(s => s.PropertyId == scope.PropertyId).Select(s => s.Id);

        var disagreeing = db.Disagreements.Where(d => d.ClearedAt == null && mine.Contains(d.StayId));
        var holding = db.HeldFacts.Where(f => f.PropertyId == scope.PropertyId && f.ResolvedAt == null);

        var total = await disagreeing.CountAsync(cancellationToken)
            + await holding.CountAsync(cancellationToken);

        var reach = (page.Page + 1) * page.PageSize;

        var disagreements = await disagreeing
            .OrderByDescending(d => d.RaisedAt).Take(reach).ToListAsync(cancellationToken);

        var held = await holding
            .OrderByDescending(f => f.ReceivedAt).Take(reach).ToListAsync(cancellationToken);

        var cards = Disagreements(disagreements)
            .Select(card => (When: card.When, Card: card.Card))
            .Concat(Held(held).Select(card => (When: card.When, Card: card.Card)))
            .OrderByDescending(entry => entry.When)
            .Skip(page.Page * page.PageSize)
            .Take(page.PageSize)
            .Select(entry => entry.Card)
            .ToArray();

        return new { total, cards };
    }

    /// <summary>A value the feed disagrees with, over a value a person set.</summary>
    /// <remarks>
    /// Both values are shown and neither is applied — the override stands until
    /// somebody clears it. That is the rule the whole PMS mode rests on: one
    /// truth leaves this application, and the disagreement is a mark on that
    /// answer rather than a second answer.
    /// </remarks>
    private static IEnumerable<(DateTimeOffset When, object Card)> Disagreements(
        IReadOnlyList<StayDisagreement> rows)
        => rows.Select(row => (row.RaisedAt, (object)new
        {
            id = row.Id.ToString(),
            kind = row.Aspect switch
            {
                DisagreementAspect.Lifecycle => "The PMS says this stay is somewhere else",
                DisagreementAspect.Assignment => "The PMS says a different room",
                DisagreementAspect.Dates => "The PMS says different dates",
                _ => "The PMS disagrees",
            },
            status = (object?)null,
            rows = new object[]
            {
                new { label = "You recorded", value = row.OurValue, tags = Array.Empty<object>() },
                new { label = "The PMS sends", value = row.PmsValue, tags = Array.Empty<object>() },
            },
            note = "Your entry stands. Nothing is applied until you decide.",
            hint = (string?)null,
            actions = new[] { "Keep ours", "Take the PMS value" },
        }));

    /// <summary>A fact the matcher could not place on a stay.</summary>
    private static IEnumerable<(DateTimeOffset When, object Card)> Held(
        IReadOnlyList<HeldFact> rows)
        => rows.Select(fact => (fact.ReceivedAt, (object)new
        {
            id = fact.Id.ToString(),
            kind = "A fact arrived that names no stay we hold",
            status = (object?)null,
            rows = new object[]
            {
                // "From" showed the integration's id (`ohip`) until 2026-09-19 —
                // its configured name is not reachable here yet, so the function.
                new { label = "From", value = "the PMS", at = (string?)null, tags = Array.Empty<object>() },

                // An instant, drawn by the screen for the property; it was
                // "dd MMM HH:mm" on the server's clock.
                new { label = "Received", value = "", at = (string?)fact.ReceivedAt.ToString("O"), tags = Array.Empty<object>() },
                new { label = "Why it is held", value = HeldReasonWords.Said(fact.Reason), at = (string?)null, tags = Array.Empty<object>() },
            },
            note = (string?)null,

            // The payload is deliberately not rendered. It is the source's own
            // JSON — a person deciding needs the fact in this application's
            // words, and putting a connector's raw body on a hotel's screen is
            // the platform diagnostic ADR 0041 keeps off it.
            hint = "Held rather than guessed at. Nothing has been applied.",
            actions = Array.Empty<string>(),
        }));
}
