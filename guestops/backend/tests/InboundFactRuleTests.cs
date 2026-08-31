using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// R7's one rule — the whole answer to out-of-order arrival.
/// </summary>
/// <remarks>
/// The system this platform replaces answered this condition three times and
/// removed none of them, and the cost was not complexity: it was that nobody
/// could say which path a given stay had taken. These are the cases that rule
/// covers, asserted so a fourth mechanism cannot quietly appear.
/// </remarks>
public class InboundFactRuleTests
{
    /// <summary>A later fact applies. That is the ordinary day.</summary>
    [Theory]
    [InlineData(StayLifecycle.Booked, StayLifecycle.InHouse)]
    [InlineData(StayLifecycle.InHouse, StayLifecycle.Departed)]
    [InlineData(StayLifecycle.Waitlisted, StayLifecycle.Booked)]
    [InlineData(StayLifecycle.Pending, StayLifecycle.InHouse)]
    public void A_later_fact_applies(StayLifecycle held, StayLifecycle arriving)
        => Assert.Equal(FactOutcome.Applied, InboundFactRule.Decide(held, arriving));

    /// <summary>The same fact twice changes nothing and publishes nothing.</summary>
    /// <remarks>
    /// What makes replay idempotent <b>by construction</b> rather than by a
    /// consumer's diligence — the Hub's backlog re-runs through this rule, and
    /// a fact already applied must not announce itself again.
    /// </remarks>
    [Theory]
    [InlineData(StayLifecycle.Booked)]
    [InlineData(StayLifecycle.InHouse)]
    [InlineData(StayLifecycle.Cancelled)]
    public void The_same_fact_twice_is_idempotent(StayLifecycle state)
        => Assert.Equal(FactOutcome.Idempotent, InboundFactRule.Decide(state, state));

    /// <summary>An earlier fact is recorded, never applied.</summary>
    /// <remarks>
    /// The guest does not un-leave. A check-in arriving after a check-out fills
    /// what is missing and moves nothing.
    /// </remarks>
    [Fact]
    public void A_late_antecedent_does_not_move_the_stay_back()
        => Assert.Equal(
            FactOutcome.Contradiction,
            InboundFactRule.Decide(StayLifecycle.Departed, StayLifecycle.InHouse));

    /// <summary>Cancelling an in-house stay is a contradiction — S26.</summary>
    /// <remarks>
    /// <para>
    /// One source contradicting itself, which has no second party and is
    /// therefore <b>not</b> a disagreement: GUEST-Q3's precedence rule is
    /// override-versus-PMS, and stretching it here would be applying a rule with
    /// only one side.
    /// </para>
    /// <para>
    /// The guest stays served and the room stays occupied; a person decides.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(StayLifecycle.InHouse)]
    [InlineData(StayLifecycle.Departed)]
    public void Cancelling_an_arrived_stay_is_a_contradiction(StayLifecycle held)
        => Assert.Equal(
            FactOutcome.Contradiction,
            InboundFactRule.Decide(held, StayLifecycle.Cancelled));

    /// <summary>But cancelling a stay that never arrived is ordinary.</summary>
    [Theory]
    [InlineData(StayLifecycle.Waitlisted, StayLifecycle.Cancelled)]
    [InlineData(StayLifecycle.Pending, StayLifecycle.Cancelled)]
    [InlineData(StayLifecycle.Booked, StayLifecycle.Cancelled)]
    [InlineData(StayLifecycle.Booked, StayLifecycle.NoShow)]
    public void A_terminal_applies_before_arrival(StayLifecycle held, StayLifecycle arriving)
        => Assert.Equal(FactOutcome.Applied, InboundFactRule.Decide(held, arriving));

    /// <summary>Nothing moves a stay out of a terminal state.</summary>
    /// <remarks>
    /// A booking cancelled and then checked in at the source is two facts that
    /// cannot both be true, and the desk is the only thing that can say which
    /// is — through a correction, which is not an inbound fact.
    /// </remarks>
    [Theory]
    [InlineData(StayLifecycle.Cancelled, StayLifecycle.InHouse)]
    [InlineData(StayLifecycle.NoShow, StayLifecycle.InHouse)]
    [InlineData(StayLifecycle.Cancelled, StayLifecycle.Booked)]
    public void A_terminal_stay_is_not_reopened_by_a_fact(
        StayLifecycle held, StayLifecycle arriving)
        => Assert.Equal(
            FactOutcome.Contradiction, InboundFactRule.Decide(held, arriving));

    /// <summary>
    /// Equal rank, different state — the lateral move GUEST-Q9 made real.
    /// </summary>
    /// <remarks>
    /// A waitlist clearing to pending, and back. Rank cannot order two states
    /// where neither precedes the other, and refusing the move would leave a
    /// desk unable to record something a PMS does every day.
    /// </remarks>
    [Theory]
    [InlineData(StayLifecycle.Waitlisted, StayLifecycle.Pending)]
    [InlineData(StayLifecycle.Pending, StayLifecycle.Waitlisted)]
    public void Pre_confirmation_states_move_between_themselves(
        StayLifecycle held, StayLifecycle arriving)
        => Assert.Equal(FactOutcome.Applied, InboundFactRule.Decide(held, arriving));

    /// <summary>Waitlisted holds no room; everything before departure does.</summary>
    /// <remarks>
    /// <b>The catch this exists to hold still.</b> A waitlist is a queue
    /// position because the hotel is full — counting one against inventory would
    /// make a full hotel look oversold, and hide the room a cancellation gives
    /// back, which is the exact moment the waitlist is for.
    /// </remarks>
    [Theory]
    [InlineData(StayLifecycle.Waitlisted, false)]
    [InlineData(StayLifecycle.Pending, true)]
    [InlineData(StayLifecycle.Booked, true)]
    [InlineData(StayLifecycle.InHouse, true)]
    [InlineData(StayLifecycle.Departed, false)]
    [InlineData(StayLifecycle.Cancelled, false)]
    [InlineData(StayLifecycle.NoShow, false)]
    public void Which_states_hold_a_room(StayLifecycle state, bool holds)
        => Assert.Equal(holds, Lifecycle.HoldsInventory(state));

    /// <summary>Only the progression is ranked; the exits are not.</summary>
    /// <remarks>
    /// Asking whether a cancellation is "later than" a check-in is asking the
    /// wrong question, and the rule answers it as a contradiction rather than a
    /// comparison. A rank on a terminal would make that comparison possible and
    /// therefore eventually made.
    /// </remarks>
    [Fact]
    public void Terminals_carry_no_rank()
    {
        Assert.Null(Lifecycle.RankOf(StayLifecycle.Cancelled));
        Assert.Null(Lifecycle.RankOf(StayLifecycle.NoShow));

        Assert.Equal(0, Lifecycle.RankOf(StayLifecycle.Waitlisted));
        Assert.Equal(0, Lifecycle.RankOf(StayLifecycle.Pending));
        Assert.Equal(1, Lifecycle.RankOf(StayLifecycle.Booked));
        Assert.Equal(2, Lifecycle.RankOf(StayLifecycle.InHouse));
        Assert.Equal(3, Lifecycle.RankOf(StayLifecycle.Departed));
    }
}
