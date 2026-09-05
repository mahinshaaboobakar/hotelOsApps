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
    /// <summary>The cards, newest first.</summary>
    public async Task<object?> AnswerAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var mine = db.Stays.Where(s => s.PropertyId == scope.PropertyId).Select(s => s.Id);

        var disagreements = await db.Disagreements
            .Where(d => d.ClearedAt == null && mine.Contains(d.StayId))
            .OrderByDescending(d => d.RaisedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var held = await db.HeldFacts
            .Where(f => f.PropertyId == scope.PropertyId && f.ResolvedAt == null)
            .OrderByDescending(f => f.ReceivedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new[] { Disagreements(disagreements), Held(held) }
            .SelectMany(cards => cards)
            .ToArray();
    }

    /// <summary>A value the feed disagrees with, over a value a person set.</summary>
    /// <remarks>
    /// Both values are shown and neither is applied — the override stands until
    /// somebody clears it. That is the rule the whole PMS mode rests on: one
    /// truth leaves this application, and the disagreement is a mark on that
    /// answer rather than a second answer.
    /// </remarks>
    private static IEnumerable<object> Disagreements(IReadOnlyList<StayDisagreement> rows)
        => rows.Select(row => new
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
        });

    /// <summary>A fact the matcher could not place on a stay.</summary>
    private static IEnumerable<object> Held(IReadOnlyList<HeldFact> rows)
        => rows.Select(fact => new
        {
            id = fact.Id.ToString(),
            kind = "A fact arrived that names no stay we hold",
            status = (object?)null,
            rows = new object[]
            {
                new { label = "From", value = fact.IntegrationId, tags = Array.Empty<object>() },
                new { label = "Received", value = fact.ReceivedAt.ToString("dd MMM HH:mm"), tags = Array.Empty<object>() },
                new { label = "Why it is held", value = fact.Reason.ToString(), tags = Array.Empty<object>() },
            },
            note = (string?)null,

            // The payload is deliberately not rendered. It is the source's own
            // JSON — a person deciding needs the fact in this application's
            // words, and putting a connector's raw body on a hotel's screen is
            // the platform diagnostic ADR 0041 keeps off it.
            hint = "Held rather than guessed at. Nothing has been applied.",
            actions = Array.Empty<string>(),
        });
}
