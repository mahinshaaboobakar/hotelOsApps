namespace HotelOS.GuestOps.Domain;

/// <summary>What applying an inbound fact to a stay does.</summary>
public enum FactOutcome
{
    /// <summary>The lifecycle moves.</summary>
    Applied,

    /// <summary>
    /// Same state, nothing new — a replay, or a fact already seen.
    /// </summary>
    /// <remarks>
    /// Nothing changes and <b>nothing is published</b>. Replay is idempotent by
    /// construction rather than by a consumer's deduplication alone.
    /// </remarks>
    Idempotent,

    /// <summary>
    /// A fact that cannot move this stay, recorded rather than applied.
    /// </summary>
    /// <remarks>
    /// The guest stays served and the room stays occupied; a person decides.
    /// Cancelling an in-house stay is the worked example (S26) — one source
    /// contradicting itself, which has no second party and is therefore not a
    /// disagreement.
    /// </remarks>
    Contradiction,
}

/// <summary>
/// R7's one rule, written once — the whole answer to out-of-order arrival.
/// </summary>
/// <remarks>
/// <para>
/// The system this platform replaces met this condition and answered it
/// <b>three times without removing any answer</b>: per-flavour
/// <c>forceCheckIn</c> / <c>forceCheckout</c> flags, <c>directCheckIn</c>
/// fallbacks marked <i>"Todo remove after data flow is ok"</i>, and a
/// commented-out replay that would have injected a check-in that never
/// happened. The cost was not complexity — it was that nobody could say which
/// path a given stay had taken.
/// </para>
/// <para>
/// <b>The rule:</b> an inbound fact is applied only if its lifecycle rank is
/// greater than or equal to the rank the stay already holds. A fact of lower
/// rank is not applied: it is recorded as a contradiction. An antecedent that
/// never arrived is recorded as <i>not observed</i> and is never synthesised.
/// </para>
/// <para>
/// <b>This governs inbound facts only.</b> A staff correction may move a stay
/// backwards — the guest checked out in error at 07:00 who is still asleep in
/// the room (S24) — and takes the stay's write permission, is recorded as a
/// correction, and publishes the correcting fact. Inbound facts are monotonic;
/// people are not, and pretending otherwise would make the erroneous check-out
/// permanent.
/// </para>
/// </remarks>
public static class InboundFactRule
{
    /// <summary>What a fact in <paramref name="arriving"/> does to a stay in <paramref name="held"/>.</summary>
    public static FactOutcome Decide(StayLifecycle held, StayLifecycle arriving)
    {
        if (held == arriving)
        {
            return FactOutcome.Idempotent;
        }

        // A terminal arriving over a pre-arrival state is the stay ending:
        // cancelled or not shown up, both of which are real and both of which
        // are exits rather than steps. Over an arrival, they are the
        // contradiction below — a guest in the room did not fail to arrive.
        if (Lifecycle.IsTerminal(arriving))
        {
            return Lifecycle.RankOf(held) is <= 1 ? FactOutcome.Applied : FactOutcome.Contradiction;
        }

        // Nothing moves a stay out of a terminal state. A booking that was
        // cancelled and then checked in at the source is two facts that cannot
        // both be true, and the desk is the only thing that can say which is.
        if (Lifecycle.IsTerminal(held))
        {
            return FactOutcome.Contradiction;
        }

        var heldRank = Lifecycle.RankOf(held);
        var arrivingRank = Lifecycle.RankOf(arriving);

        // Both ranks exist here: terminals are handled above, and every
        // non-terminal state is ranked. The pattern keeps that a compile-time
        // fact rather than two null-forgiving operators.
        // Greater-or-equal applies. Equal-and-different is the lateral move
        // inside pre-confirmation — a waitlist clearing to pending, and back —
        // which is real, and which rank cannot order because neither state
        // precedes the other (GUEST-Q9).
        return (heldRank, arrivingRank) switch
        {
            (int h, int a) when a >= h => FactOutcome.Applied,
            _ => FactOutcome.Contradiction,
        };
    }
}
