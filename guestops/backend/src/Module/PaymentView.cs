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
public sealed class PaymentView(GuestOpsDbContext db)
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
            terms = Rows(terms, stay.Arrival, stay.Departure),
            note = Note,
            folio = Folio,
            folioNote = FolioNote,
        };
    }

    /// <summary>The terms, as the design's rows.</summary>
    private static object[] Rows(
        CommercialTerms? terms, DateTimeOffset? arrival, DateTimeOffset? departure)
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
                // the moment a departure moves.
                rows.Add(Big(
                    Spell(count),
                    Money(rate with { MinorUnits = rate.MinorUnits * count }),
                    string.Empty,
                    Basis(rate.Basis)));
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
                strong = depositDays == 1 ? "1 day after booking" : $"{depositDays} days after booking",
                tags = new[] { Lock("COMPUTED FROM OFFSET") },
            });
        }

        // **Computed at the moment it is shown, from the stored offset** (R18).
        // The record holds *48 hours before arrival*; move the arrival and this
        // moves with it. A stored deadline silently stops matching.
        if (terms.CancellationDeadline(Date(arrival)) is { } deadline)
        {
            rows.Add(new
            {
                label = "Cancellation",
                value = $"{terms.CancelOffsetDaysFromArrival} days before arrival → ",
                strong = deadline.ToString("ddd d MMM HH:mm"),
                tags = Passed(deadline) ? new[] { PillWarn("deadline passed") } : [],
            });
        }

        if (terms.PenaltyAmount is { IsStated: true } penalty)
        {
            rows.Add(Row(
                "Penalty if cancelled",
                Money(penalty),
                terms.PenaltyNights is { } count
                    ? [Lock(Basis(penalty.Basis)), Lock(count == 1 ? "1 NIGHT" : $"{count} NIGHTS")]
                    : [Lock(Basis(penalty.Basis))]));
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

    private static DateOnly? Date(DateTimeOffset? at)
        => at is { } value ? DateOnly.FromDateTime(value.Date) : null;

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
    private static string Money(Money amount)
        => $"{amount.Currency} {amount.MinorUnits / 100m:N2}";

    private static string Basis(TaxBasis basis)
        => basis switch
        {
            TaxBasis.Gross => "GROSS — TAX INCLUDED",
            TaxBasis.Net => "NET — TAX EXCLUDED",
            _ => "BASIS NOT STATED",
        };

    private static string Spell(int nights)
        => nights switch
        {
            1 => "One night",
            2 => "Two nights",
            3 => "Three nights",
            4 => "Four nights",
            _ => $"{nights} nights",
        };

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

    private const string Note =
        "The deadlines are computed, never stored. The record holds “48 hours "
        + "before arrival”; move the arrival and the deadline moves with it. A "
        + "stored deadline silently stops matching its reservation, and that is a "
        + "chargeable error.";

    /// <summary>Five lines, each naming what it would take.</summary>
    private static readonly object[] Folio =
    [
        new { label = "Deposit received", because = "NEEDS FINANCE OR A CONNECTOR CAPABILITY" },
        new { label = "Room & tax posted", because = "NOT AVAILABLE" },
        new { label = "Extras", because = "NOT AVAILABLE" },
        new { label = "Balance due", because = "NOT AVAILABLE" },
        new { label = "Settle · invoice", because = "FINANCE, A LATER ROUND" },
    ];

    private const string FolioNote =
        "Two different gaps, and they need two different answers. In a "
        + "PMS-connected property the folio lives in Opera and the desk settles "
        + "there — showing it here needs the connector to carry a balance, a "
        + "capability v1's inbound contract does not include. In a standalone "
        + "property there is no Opera, so settlement happens nowhere in v1 — a "
        + "consequence accepted knowingly with GUEST-Q6, and the reason the first "
        + "deployments are PMS-connected.";
}
