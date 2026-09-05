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

        rows.Add(Row("Afterwards", Afterwards(cancellable), null, []));

        return new
        {
            subject = Subject(record, cancellable.Count),
            consequence = Consequence(cancellable.Count),

            // Stated, so no reader has to count it off a mixed list of rows.
            stays = cancellable.Count,
            rows = rows.ToArray(),
            notTold = record.Reference is null ? null : NotTold,
            reasons = Reasons(),
        };
    }

    /// <summary>`BK-4506 · Fatima Sheikh · two stays, 3 – 7 September`.</summary>
    private static string Subject(BookingRecord record, int count)
    {
        var parts = new List<string>();

        if (record.Reference is { } reference)
        {
            parts.Add(reference);
        }

        if (record.Guest is { } guest)
        {
            parts.Add(guest);
        }

        var span = record.Arrival is { } from && record.Departure is { } to
            ? $"{Spell(count)} {(count == 1 ? "stay" : "stays")}, {from.Day} – {to:d MMMM}"
            : $"{Spell(count)} {(count == 1 ? "stay" : "stays")}";

        parts.Add(span);
        return string.Join(" · ", parts);
    }

    /// <summary>The sentence naming what the button does.</summary>
    private static string Consequence(int count)
        => count == 1
            ? "This cancels one stay. A booking is a group and every operation "
                + "happens to a stay — so this records one cancellation, and it "
                + "can be reinstated afterwards."
            : $"This cancels {Spell(count)} stays, one at a time. A booking is a "
                + "group and every operation happens to a stay — so this records "
                + $"{Spell(count)} cancellations, and any of them can be "
                + "reinstated separately afterwards.";

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

        var dates = stay.Arrival is { } from && stay.Departure is { } to
            ? $"{from.Day} – {to:d MMM} · "
            : string.Empty;

        if (!terms.TryGetValue(stay.Id, out var agreed) || agreed.PenaltyAmount is null)
        {
            return Row(label, $"{dates}no penalty agreed", null, []);
        }

        var money = agreed.PenaltyAmount;

        // An amount with no currency is not an amount. `Money.IsStated` exists
        // for exactly this: a row can carry minor units and no currency, and
        // rendering `12000.00` with nothing beside it is a number a guest could
        // be charged in the wrong denomination.
        if (!money.IsStated)
        {
            return Row(label, $"{dates}penalty recorded without a currency", null, []);
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

        return Row(
            label,
            dates,
            $"penalty {money.Currency} {Major(money.MinorUnits):N2}",
            [.. tags]);
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
    private static object Row(string label, string value, string? strong, object[] tags)
        => strong is null
            ? new { label, value, tags }
            : (object)new { label, value, strong, tags };

    /// <summary>What happens to the rooms.</summary>
    private static string Afterwards(IReadOnlyList<BookingStayRow> stays)
    {
        if (stays.Count == 0)
        {
            return "nothing returns to inventory — no stay on this booking can be cancelled";
        }

        var from = stays.Min(stay => stay.Arrival);
        var to = stays.Max(stay => stay.Departure);

        var rooms = stays.Count == 1 ? "the room returns" : "both rooms return";
        var many = stays.Count > 2 ? $"all {stays.Count} rooms return" : rooms;

        return from is { } start && to is { } end
            ? $"{many} to inventory for {start.Day} – {end:d MMMM}"
            : $"{many} to inventory";
    }

    /// <summary>
    /// The sentence that must not be omitted — CONN-Q5, ADR 0128 §4.
    /// </summary>
    /// <remarks>
    /// Nothing GuestOps records reaches the PMS in v1. A cancellation screen
    /// that stayed silent about that would let a receptionist believe the room
    /// had been released in Opera, and the room would be sold twice.
    /// </remarks>
    private const string NotTold =
        "Opera will not be told. This records the cancellation in HotelOS only "
        + "— it does not reach the PMS, and Opera will keep showing this booking "
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

    /// <summary>Small counts, spelled the way the design spells them.</summary>
    private static string Spell(int count)
        => count switch
        {
            1 => "one",
            2 => "two",
            3 => "three",
            4 => "four",
            _ => count.ToString(),
        };
}
