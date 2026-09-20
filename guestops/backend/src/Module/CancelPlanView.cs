using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What cancelling this booking will actually do — gold frame 8's dialog.
/// </summary>
/// <remarks>
/// <para>
/// <b>A plan, computed before anything is written.</b> The confirmation names
/// the object, the consequence and the limit (ADR 0106 §3), and every one of
/// those is a fact the server has to supply — a desk cannot be asked to confirm
/// a penalty the screen invented.
/// </para>
/// <para>
/// <b>Cancelling a booking is n cancellations of stays</b>, said out loud
/// (GUEST-Q2, S23). The dialog counts them because that is what the model does
/// and because either stay can be reinstated on its own afterwards.
/// </para>
/// <para>
/// <b>The penalty is computed from the stored offset at the moment it is
/// shown</b> (R18) and <b>recorded, never charged</b> (GUEST-Q6). Charging is
/// Finance's.
/// </para>
/// </remarks>
public sealed class CancelPlanView(
    GuestOpsDbContext db,
    BookingReadService bookings)
{
    /// <summary>The plan for one booking.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid bookingId, CancellationToken cancellationToken)
    {
        var record = await bookings.GetAsync(scope, bookingId, cancellationToken);

        var cancellable = record.Stays
            .Where(stay => stay.Status is StayLifecycle.Booked or StayLifecycle.Waitlisted)
            .ToList();

        var terms = await TermsAsync(cancellable, cancellationToken);
        var types = await TypesAsync(record, cancellationToken);

        var rows = new List<object>();

        foreach (var stay in cancellable)
        {
            rows.Add(Penalty(stay, terms, types));
        }

        // Why there is a penalty, where the terms say. Omitted rather than
        // guessed when they do not: a cancellation penalty a person is about to
        // agree to must state the rule it came from, and inventing a plausible
        // one — *within 48 h of arrival* — would put a term nobody agreed in
        // front of a guest.
        if (Why(cancellable, terms) is { } why)
        {
            rows.Add(Row("Why a penalty", why, null, []));
        }

        rows.Add(Afterwards(cancellable));

        return new
        {
            subject = Subject(record),

            // Stated, so no reader has to count it off a mixed list of rows.
            stays = cancellable.Count,
            rows = rows.ToArray(),
            notTold = record.Reference is null ? null : NotTold,
            reasons = Reasons(),
        };
    }

    /// <summary>The parts of `BK-4506 · Fatima Sheikh · two stays, 3 – 7 September`.</summary>
    /// <remarks>
    /// <para>
    /// <b>ADR 0175, and this line is the clearest case in the application.</b>
    /// It joined a reference, a name, a spelled count, a singular-or-plural noun
    /// and a compressed date range into one string. Only the first two are
    /// facts; the rest is English, and the range is not even a format —
    /// <c>3 – 7 September</c> puts the month at the second end **because
    /// `en-GB` puts the month after the day**, and a locale that writes the
    /// month first cannot be served by moving it.
    /// </para>
    /// <para>
    /// The count is <i>cancellable</i> stays and not every stay on the booking,
    /// which is why it travels beside the subject rather than being recounted
    /// from the rows: a booking with three stays where one has already departed
    /// says <i>two stays</i>, and a screen counting rows would say three.
    /// </para>
    /// </remarks>
    private static object Subject(BookingRecord record)
        => new
        {
            reference = record.Reference,
            guest = record.Guest,
            arrive = Iso(record.Arrival),
            depart = Iso(record.Departure),
        };

    /// <summary>A day as ISO-8601, or null — never a rendering.</summary>
    private static string? Iso(DateOnly? day) => day?.ToString("yyyy-MM-dd");

    // `Consequence` was the sentence naming what the button does — "This
    // cancels two stays, one at a time." It is the screen's now (ADR 0175),
    // composed from the `stays` count this view already sends, because the
    // spelled number, the plural and the clause order are all English.
    //
    // Its own history is worth keeping: it went on to explain that a booking is
    // a group and every operation happens to a stay — the model, for the
    // developer — and promised the stays "can be reinstated afterwards", which
    // nothing in GuestOps can do. Both removed on 2026-09-19 (the owner's
    // ruling on developer notes, and a sentence may not promise an outcome the
    // code does not deliver). Neither may come back in the screen's wording.

    /// <summary>One stay's penalty, as the dialog states it.</summary>
    /// <remarks>
    /// <b>An amount carries three things or it is not an amount</b> — value,
    /// currency, and whether tax is included. A stay whose terms carry no
    /// penalty says so in words rather than showing a zero, because zero is a
    /// penalty of nothing and *no terms* is nobody having agreed one.
    /// </remarks>
    private static object Penalty(
        BookingStayRow stay,
        IReadOnlyDictionary<Guid, CommercialTerms> terms,
        IReadOnlyDictionary<string, string> types)
    {
        var label = stay.RoomTypeId is { } id && types.TryGetValue(id, out var name)
            ? name
            : "This stay";

        // The two days, not the range: the row's value used to open with
        // `3 – 7 Sep · ` and the screen now draws that span in the property's
        // locale. Both ends or neither — a penalty row naming one date would
        // read as the day the penalty falls due, which is a different fact.
        var arrive = stay.Departure is null ? null : Iso(stay.Arrival);
        var depart = stay.Arrival is null ? null : Iso(stay.Departure);

        if (!terms.TryGetValue(stay.Id, out var agreed) || agreed.PenaltyAmount is null)
        {
            return Row(label, "no penalty agreed", null, [], arrive, depart);
        }

        var money = agreed.PenaltyAmount;

        // An amount with no currency is not an amount. `Money.IsStated` exists
        // for exactly this: a row can carry minor units and no currency, and
        // rendering `12000.00` with nothing beside it is a number a guest could
        // be charged in the wrong denomination.
        if (!money.IsStated)
        {
            return Row(label, "penalty recorded without a currency", null, [], arrive, depart);
        }

        var tags = new List<object>
        {
            new { kind = "lock", tone = "neutral", text = Basis(money.Basis) },
        };

        // The nights the penalty is worth, where the terms say. It is the
        // second half of the design's `GROSS · 1 NIGHT` and it is omitted
        // rather than guessed when the terms did not state one.
        if (agreed.PenaltyNights is { } nights)
        {
            tags.Add(new
            {
                kind = "lock",
                tone = "neutral",
                text = nights == 1 ? "1 NIGHT" : $"{nights} NIGHTS",
            });
        }

        // **THE KNOWN ADR 0175 MIGRATION SITE. NUM-Q2 IS RULED; THIS IS OWED WORK.**
        // ADR 0175 names this line: money is not an exception — the service sends
        // amount and currency, and the screen formats them. Its NUM-Q2 amendment
        // (2026-09-20) settles the wire: the amount is a DECIMAL STRING and the
        // currency an ISO 4217 alphabetic code, and "no N2-formatted text appears
        // on a service contract". This line is N2 text in the server's culture, so
        // it is wrong under a ruling that exists rather than waiting for one.
        // It migrates with PaymentView's Money() when GG's SDK money style lands;
        // the page-64 audit records U1/U2 as owed against ADR 0175 + NUM-Q2.
        return Row(
            label,
            string.Empty,
            $"penalty {money.Currency} {Major(money.MinorUnits):N2}",
            [.. tags],
            arrive,
            depart);
    }

    /// <summary>
    /// Minor units to the major unit a person reads.
    /// </summary>
    /// <remarks>
    /// <b>A hundred is assumed and that assumption is wrong for some
    /// currencies</b> — the Kuwaiti dinar has three decimal places and the yen
    /// has none. The exponent belongs with the currency and this schema does
    /// not carry one, which is a real gap in the money model rather than
    /// something this projection can settle. It is named here so the next
    /// person to meet it finds it stated rather than inferred from a division.
    /// </remarks>
    private static decimal Major(long minorUnits) => minorUnits / 100m;

    /// <summary>Whether the amount includes tax, in the design's words.</summary>
    private static string Basis(TaxBasis basis)
        => basis switch
        {
            TaxBasis.Gross => "GROSS",
            TaxBasis.Net => "NET",

            // The third state is *nobody said*, and it is drawn as that rather
            // than defaulted to either — a penalty shown as gross that is
            // actually net is wrong by the tax rate.
            _ => "BASIS NOT STATED",
        };

    /// <summary>
    /// The rule the penalties came from, where the terms name one.
    /// </summary>
    /// <remarks>
    /// One sentence for the whole booking rather than one per stay: the stays
    /// of a booking are sold together and share their terms, and a per-row
    /// repetition of the same rule would read as several different ones. Where
    /// they genuinely differ, the bases are listed.
    /// </remarks>
    private static string? Why(
        IReadOnlyList<BookingStayRow> stays,
        IReadOnlyDictionary<Guid, CommercialTerms> terms)
    {
        var bases = stays
            .Select(stay => terms.TryGetValue(stay.Id, out var agreed) ? agreed.PenaltyBasis : null)
            .Where(basis => !string.IsNullOrWhiteSpace(basis))
            .Distinct()
            .ToList();

        return bases.Count == 0 ? null : string.Join("; ", bases);
    }

    /// <summary>A label–value row of the dialog.</summary>
    /// <summary>One row of the plan — and the days it is about, where it has them.</summary>
    /// <remarks>
    /// <c>arrive</c> and <c>depart</c> are omitted rather than sent as nulls,
    /// so a row that is not about a span carries no field for one. The screen
    /// draws the span before the value, which is where the server used to put
    /// it as text.
    /// </remarks>
    private static object Row(
        string label,
        string value,
        string? strong,
        object[] tags,
        string? arrive = null,
        string? depart = null)
        => (strong, arrive) switch
        {
            (null, null) => (object)new { label, value, tags },
            (null, not null) => new { label, value, tags, arrive, depart },
            (not null, null) => new { label, value, strong, tags },
            _ => new { label, value, strong, tags, arrive, depart },
        };

    /// <summary>What happens to the rooms — the count and the span, not the sentence.</summary>
    /// <remarks>
    /// <para>
    /// <b>ADR 0175.</b> This wrote <i>both rooms return to inventory for
    /// 3 – 7 September</i>: a spelled quantity (<i>the room</i> · <i>both
    /// rooms</i> · <i>all 4 rooms</i>), an English clause order, and a
    /// compressed range whose month sits at the second end because that is what
    /// `en-GB` does. The screen says it now, from <c>rooms</c> and the two days.
    /// </para>
    /// <para>
    /// <b>The rooms are the plan's own <c>stays</c>, already on the payload</b>
    /// — one stay is one room here — so this row carries no second count that
    /// could disagree with it. The empty case needs no token either: a plan
    /// with no cancellable stay is <c>stays: 0</c>, and the screen says
    /// <i>nothing returns to inventory</i> from that rather than from a word
    /// this view invents for it.
    /// </para>
    /// </remarks>
    private static object Afterwards(IReadOnlyList<BookingStayRow> stays)
        => Row(
            "Afterwards",
            string.Empty,
            null,
            [],
            stays.Count == 0 ? null : Iso(stays.Min(stay => stay.Arrival)),
            stays.Count == 0 ? null : Iso(stays.Max(stay => stay.Departure)));

    /// <summary>
    /// The sentence that must not be omitted — CONN-Q5, ADR 0128 §4.
    /// </summary>
    /// <remarks>
    /// Nothing GuestOps records reaches the PMS in v1. A cancellation screen
    /// that stayed silent about that would let a receptionist believe the room
    /// had been released in Opera, and the room would be sold twice.
    /// </remarks>
    private const string NotTold =
        // It named Opera until 2026-09-19, whatever PMS the property runs. The
        // configured name is the Integration Hub's and not reachable from here
        // yet (SourceNameTests' remarks), so it says the function instead.
        "The PMS will not be told. This records the cancellation in HotelOS only "
        + "— it does not reach the PMS, and the PMS will keep showing this booking "
        + "as live until somebody cancels it there too.";

    /// <summary>
    /// The reasons the property configured — <b>and nothing configures them</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Frame 8 draws a reason chooser showing <i>Guest cancelled — flight
    /// changed</i>. <c>GuestOpsSettings</c> carries registration, reporting and
    /// numbering and <b>no cancellation reasons</b>, and frame 16 — the settings
    /// screen — does not configure any either. So no owner exists for this list.
    /// </para>
    /// <para>
    /// It returns empty, and the screen draws the field with nothing in it.
    /// The alternative was a hardcoded list here, which would put a reporting
    /// vocabulary nobody chose into a projection, in the one field a
    /// cancellation is later audited by. <b>Reported as a gap rather than
    /// filled</b>.
    /// </para>
    /// </remarks>
    private static string[] Reasons() => [];

    /// <summary>The agreed terms for the stays being cancelled.</summary>
    private async Task<IReadOnlyDictionary<Guid, CommercialTerms>> TermsAsync(
        IReadOnlyList<BookingStayRow> stays, CancellationToken cancellationToken)
    {
        var ids = stays.Select(stay => stay.Id).ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, CommercialTerms>();
        }

        var terms = await db.Terms
            .Where(one => ids.Contains(one.StayId))
            .ToListAsync(cancellationToken);

        return terms.ToDictionary(one => one.StayId);
    }

    /// <summary>The room type names for this booking's stays.</summary>
    private async Task<IReadOnlyDictionary<string, string>> TypesAsync(
        BookingRecord record, CancellationToken cancellationToken)
    {
        var ids = record.Stays
            .Select(stay => stay.RoomTypeId)
            .Where(id => id is not null)
            .Select(id => Guid.Parse(id!))
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<string, string>();
        }

        var types = await db.Set<MasterDataRoomTypeName>()
            .Where(type => ids.Contains(type.Id))
            .ToListAsync(cancellationToken);

        return types.ToDictionary(type => type.Id.ToString(), type => type.Name);
    }

    // `Spell` — small counts written as English words, the way the design
    // spells them — went to the screen with the sentences that used it
    // (ADR 0175, 2026-09-20). It is not a formatting helper to be reinstated
    // here: which numbers a language spells, and how, is the reader's.
}
