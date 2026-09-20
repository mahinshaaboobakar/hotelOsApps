using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What the stay was sold on, and what the platform cannot tell you — frame 7.
/// </summary>
/// <remarks>
/// <para>
/// <b>The terms are v1 and buildable today</b> (GUEST-Q6). What is real here is
/// the part that would otherwise be lost: a guarantee with its codes, a deposit
/// deadline as an <b>offset from the booking date</b>, a cancellation deadline
/// as an <b>offset from arrival</b> plus a drop time, and an amount with a
/// basis, a night count and a currency (R18). The system this replaces kept two
/// pre-formatted human strings and discarded the structure.
/// </para>
/// <para>
/// <b>The deadlines are computed here, never read from a column</b> (R18). The
/// record holds <i>48 hours before arrival</i>; move the arrival and the
/// deadline moves with it. A stored deadline silently stops matching its
/// reservation, and that is a chargeable error.
/// </para>
/// <para>
/// <b>The folio is not ruled and nothing is built behind it.</b> Every line of
/// it is returned as a refusal naming what it would take — never as a zero. A
/// balance of nothing and a balance nobody can compute look identical on a
/// screen and mean opposite things.
/// </para>
/// </remarks>
public sealed class PaymentView(GuestOpsDbContext db, IBusinessDay businessDay)
{
    /// <summary>The terms, and the folio's five refusals.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        var stay = await db.Stays
            .Where(one => one.Id == stayId && one.PropertyId == scope.PropertyId)
            .Select(one => new { one.Id, Arrival = one.ArrivalAt.At, Departure = one.DepartureAt.At })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        var terms = await db.Terms
            .FirstOrDefaultAsync(one => one.StayId == stayId, cancellationToken);

        return new
        {
            terms = Rows(terms, stay.Arrival, stay.Departure, await businessDay.ZoneAsync(scope, cancellationToken)),
            // Null: it explained how the deadlines are computed and stored — a
            // developer's note, removed under the owner's ruling of 2026-09-19.
            note = (string?)null,
            folio = Folio,
            folioNote = FolioNote,
        };
    }

    /// <summary>The terms, as the design's rows.</summary>
    private static object[] Rows(
        CommercialTerms? terms, DateTimeOffset? arrival, DateTimeOffset? departure, TimeZoneInfo? zone)
    {
        // **No terms is a real state, said as one.** A stay a source sent with
        // no commercial detail is ordinary, and a card of empty rows would read
        // as a loading failure.
        if (terms is null)
        {
            return
            [
                Row("The terms", "nothing was recorded about what this stay was sold on", []),
            ];
        }

        var rows = new List<object>();
        var nights = Nights(arrival, departure);

        if (terms.Amount is { IsStated: true } rate)
        {
            rows.Add(Big("Rate", Money(rate), " per night", Basis(rate.Basis)));

            if (nights is { } count)
            {
                // Multiplied here rather than stored: a total is a consequence
                // of the rate and the nights, and a stored one stops matching
                // the moment a departure moves. The nights travel as a count
                // the screen words and formats — the label was an English word
                // ("Four nights") until 2026-09-19.
                rows.Add(new
                {
                    label = "For the stay",
                    value = "",
                    strong = Money(rate with { MinorUnits = rate.MinorUnits * count }),
                    count = Count(count, "night", "nights"),
                    big = true,
                    tags = new[] { Lock(Basis(rate.Basis)) },
                });
            }
        }

        var plan = Plan(terms);

        if (plan is not null)
        {
            rows.Add(Row("Rate plan", plan, []));
        }

        var guarantee = terms.GuaranteeDescription ?? terms.GuaranteeCode;

        if (!string.IsNullOrWhiteSpace(guarantee))
        {
            var marks = new List<object>();

            // Two independent facts about one guarantee, and the design draws
            // both: whether it holds a room, and whether it is currently held.
            if (terms.ReservesInventory)
            {
                marks.Add(Pill("holds inventory"));
            }

            if (terms.OnHold)
            {
                marks.Add(Pill("on hold"));
            }

            rows.Add(Row("Guarantee", guarantee, [.. marks]));
        }

        if (terms.DepositOffsetDaysFromBooking is { } depositDays)
        {
            rows.Add(new
            {
                label = "Deposit policy",
                value = "due ",
                count = Count(depositDays, "day after booking", "days after booking"),
                // Carried a "COMPUTED FROM OFFSET" tag — how the value is
                // derived, for the developer.
                tags = Array.Empty<object>(),
            });
        }

        // **Computed at the moment it is shown, from the stored offset** (R18).
        // The record holds *48 hours before arrival*; move the arrival and this
        // moves with it. A stored deadline silently stops matching.
        if (terms.CancellationDeadline(Date(arrival, zone), zone) is { } deadline)
        {
            rows.Add(new
            {
                label = "Cancellation",
                value = "",
                // A deadline exists only where the offset does (CancellationDeadline).
                count = Count(terms.CancelOffsetDaysFromArrival!.Value, "day before arrival", "days before arrival"),

                // The deadline as an instant, drawn by the screen in the
                // property's zone — "ddd d MMM HH:mm" on the server until
                // 2026-09-19.
                at = deadline.ToString("O"),
                tags = Passed(deadline) ? new[] { PillWarn("deadline passed") } : [],
            });
        }

        if (terms.PenaltyAmount is { IsStated: true } penalty)
        {
            rows.Add(new
            {
                label = "Penalty if cancelled",
                value = "",
                strong = Money(penalty),

                // The nights it is worth, as a count — a lock reading "1 NIGHT"
                // until 2026-09-19.
                count = terms.PenaltyNights is { } penaltyNights ? Count(penaltyNights, "night", "nights") : null,
                tags = new[] { Lock(Basis(penalty.Basis)) },
            });
        }

        return [.. rows];
    }

    /// <summary>The rate plan as the source named it — its name, else its code.</summary>
    /// <remarks>
    /// Both, joined, where both exist: <c>BAR-FLEX · Best Available, flexible</c>
    /// is what the design draws, and the code alone is what a revenue manager
    /// searches on while the name is what a receptionist reads.
    /// </remarks>
    private static string? Plan(CommercialTerms terms)
    {
        var parts = new[] { terms.RateCode, terms.RateName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? null : string.Join(" · ", parts);
    }

    /// <summary>Whether a deadline is behind us, on the same clock it was built on.</summary>
    private static bool Passed(DateTimeOffset deadline)
        => deadline < DateTimeOffset.UtcNow;

    /// <summary>The arrival's day at the property — UTC's until 2026-09-19 (ADR 0174).</summary>
    private static DateOnly? Date(DateTimeOffset? at, TimeZoneInfo? zone)
        => new StayTime(at, TimeBasis.Observed).DateIn(zone);

    /// <summary>How many nights the stay is, where both ends are known.</summary>
    private static int? Nights(DateTimeOffset? arrival, DateTimeOffset? departure)
    {
        if (arrival is not { } from || departure is not { } to)
        {
            return null;
        }

        var count = (to.Date - from.Date).Days;
        return count > 0 ? count : null;
    }

    /// <summary>`₹ 8 400.00 INR` — value and currency, never one without the other.</summary>
    /// <remarks>
    /// The minor-unit exponent is assumed to be two, which is wrong for the
    /// Kuwaiti dinar and for the yen. The exponent belongs with the currency and
    /// this schema does not carry one; it is named here rather than hidden
    /// inside a division.
    /// </remarks>
    /// <summary>An amount, as text — the one value this view still formats.</summary>
    /// <remarks>
    /// <b>A known ADR 0175 migration site. NUM-Q2 is ruled, so this is owed
    /// work and no longer a blocked line.</b> ADR 0175 rules that money is not
    /// an exception: the service sends amount and currency and the screen
    /// formats them. Its <c>NUM-Q2</c> amendment (2026-09-20) settles the wire
    /// — <i>"<c>amount</c> is a decimal string and <c>currency</c> is an ISO
    /// 4217 alphabetic code"</i>, and <i>"no <c>N2</c>-formatted text appears
    /// on a service contract"</i>. This line is that text, so it is wrong
    /// under a ruling that exists rather than waiting for one.
    /// <para>
    /// It migrates when the SDK's money style lands — the parts here,
    /// composition there — and not before: a contract that sends the parts to
    /// a reader that cannot render them makes the screen worse while looking
    /// like progress. The page-64 audit records U1 and U2 as owed against ADR
    /// 0175 + NUM-Q2. <b>The exponent above is part of that work</b>: NUM-Q2
    /// rejects minor units as the canonical wire representation, and the
    /// division by 100 here is the same assumption this schema cannot state.
    /// </para>
    /// </remarks>
    private static string Money(Money amount)
        => $"{amount.Currency} {amount.MinorUnits / 100m:N2}";

    private static string Basis(TaxBasis basis)
        => basis switch
        {
            TaxBasis.Gross => "GROSS — TAX INCLUDED",
            TaxBasis.Net => "NET — TAX EXCLUDED",
            _ => "BASIS NOT STATED",
        };

    /// <summary>A count with its words, for the screen to format and choose between.</summary>
    /// <remarks>
    /// The number travels as a number — the screen formats it in the property's
    /// locale (page 64 §12). The two English forms travel with it because the
    /// module's copy is English; which one applies is the screen's to decide.
    /// </remarks>
    private static object Count(int n, string one, string other) => new { n, one, other };

    private static object Row(string label, string value, object[] tags)
        => new { label, value, tags };

    private static object Big(string label, string strong, string tail, string basis)
        => new { label, value = "", strong, tail, big = true, tags = new[] { Lock(basis) } };

    private static object Lock(string text)
        => new { kind = "lock", tone = "neutral", text };

    private static object Pill(string text)
        => new { kind = "pill", tone = "neutral", text };

    private static object PillWarn(string text)
        => new { kind = "pill", tone = "warn", text };

    /// <summary>Five lines, each naming what it would take.</summary>
    private static readonly object[] Folio =
    [
        new { label = "Deposit received", because = "NOT AVAILABLE" },
        new { label = "Room & tax posted", because = "NOT AVAILABLE" },
        new { label = "Extras", because = "NOT AVAILABLE" },
        new { label = "Balance due", because = "NOT AVAILABLE" },
        new { label = "Settle · invoice", because = "NOT AVAILABLE" },
    ];

    // It explained two different gaps, the connector contract and a register
    // ruling — for the developer. What the desk needs is the one sentence.
    private const string FolioNote =
        "GuestOps cannot show the folio or take a payment yet.";
}
