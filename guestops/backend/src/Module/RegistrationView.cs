using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The registration card, as the card screen draws it — gold frame 15.
/// </summary>
/// <remarks>
/// <para>
/// <b>The card was drawn from a fixture and its <c>Save</c> did nothing.</b>
/// <c>RegistrationService</c> has captured cards since it was written and no
/// screen could reach it — the ledger has carried that as a defect, and this
/// is the read half of closing it.
/// </para>
/// <para>
/// <b>Served under <c>registration.capture</c> and not under
/// <c>reservation.read</c>.</b> This answer carries a passport number, a visa
/// number and a home address whole, because the desk holding the document has
/// to be able to correct what was typed. A property that grants a screen the
/// day's arrivals has not thereby granted it every guest's papers, and the
/// split is what keeps those two decisions separate.
/// </para>
/// <para>
/// <b>Which fields are REQUIRED comes from the property, never from here.</b>
/// <c>RegistrationFields</c> holds the design's proposal and no required-ness
/// at all; the flag on each box is <c>GuestOpsSettings</c>'s answer for this
/// guest, by way of <see cref="RegistrationRule.RequiredFor"/>. That is frame
/// 15's caption, and it is why this file has no list of mandatory fields to
/// find.
/// </para>
/// <para>
/// <b>Nothing here invents a value.</b> A property with no settings row has not
/// been configured, which <c>SettingsService</c> distinguishes from one that
/// requires nothing; a card that has never been saved has no number, so the
/// series shows what the property <i>would</i> mint rather than a number that
/// has been taken.
/// </para>
/// </remarks>
public sealed class RegistrationView(
    GuestOpsDbContext db, SettingsService settings, IBusinessDay businessDay)
{
    /// <summary>The card for a stay, with the property's own required set applied.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay whose card this is.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The card, as frame 15 draws it.</returns>
    /// <exception cref="NotFoundException">No such stay at this property.</exception>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        var stay = await db.Stays
            .Where(s => s.PropertyId == scope.PropertyId && s.Id == stayId)
            .FirstOrDefaultAsync(cancellationToken)
            // Scoped by property before id, so another property's stay is not
            // found rather than forbidden — ADR 0054's boundary.
            ?? throw new NotFoundException("stay", stayId);

        var configuration = await settings.LoadAsync(scope.PropertyId, cancellationToken);

        // A stay whose card has never been opened has none, and that is the
        // ordinary case rather than an error: the card is created on the first
        // save, which is when its number is minted.
        var card = await db.Registrations
            .FirstOrDefaultAsync(r => r.StayId == stayId, cancellationToken)
            ?? new Registration { StayId = stayId };

        var guest = await db.Party
            .Where(p => p.StayId == stay.Id)
            .OrderByDescending(p => p.IsPrimary == true)
            .Select(p => p.Guest!.NameAsGiven)
            .FirstOrDefaultAsync(cancellationToken);

        var room = stay.CurrentRoomId is not { } assigned
            ? null
            : await db.Set<Infrastructure.ReadModels.MasterDataRoom>()
                .Where(r => r.Id == assigned)
                .Select(r => r.RoomNumber)
                .FirstOrDefaultAsync(cancellationToken);

        var due = await db.Reporting
            .Where(r => r.StayId == stay.Id && r.State == ReportingState.Needed)
            .Select(r => r.RequiredBy)
            .FirstOrDefaultAsync(cancellationToken);

        // The property's own zone, because a day is the property's — a date
        // component read off a stored instant is UTC's after a round trip.
        var zone = await businessDay.ZoneAsync(scope, cancellationToken);

        var required = RegistrationRule.RequiredFor(configuration, card.Nationality).ToHashSet();
        var visitor = RegistrationRule.IsVisitor(card.Nationality, configuration.HomeCountry);

        return new
        {
            stayId = stay.Id.ToString(),

            // "Not yet named" is a state. A stay a feed sent before the guest
            // was known has no name, and inventing one would put a room in the
            // hands of somebody who does not exist.
            who = string.IsNullOrWhiteSpace(guest) ? "Not yet named" : guest,

            // The room, where there is one. A card can be filled in before a
            // room is assigned, and saying which room would be a claim.
            room,

            // **Carried because the card's primary action checks the guest in**,
            // and a lifecycle write takes the version it was read at. Without
            // it the card would either guess or read the stay a second time,
            // and both make the concurrency check a formality.
            version = stay.Version,

            arriving = stay.Lifecycle == StayLifecycle.Booked,

            // Values, not a rendering — the card's header reads `31 Aug → 4
            // Sep`, and which of month and day comes first is the reader's
            // locale (ADR 0174, and the owner's decision B on the join). Null
            // where nothing is recorded: a stay a feed sent without a departure
            // has none, and a date invented for the header would be a claim.
            arrive = stay.ArrivalAt.DateIn(zone)?.ToString("yyyy-MM-dd"),
            depart = stay.DepartureAt.DateIn(zone)?.ToString("yyyy-MM-dd"),

            // So the screen can say why the block is there in its own words.
            // The country is the property's setting and never a literal.
            homeCountry = configuration.HomeCountry,

            series = Series(configuration, card),

            rows = Lines(RegistrationFields.Main, card, configuration, required),

            foreign = visitor
                ? new
                {
                    title = "Guest from outside",

                    // **It names why it is here.** A block of extra questions
                    // with no stated reason reads to a receptionist as the
                    // software being difficult; naming the two countries reads
                    // as a rule they can explain to the person in front of them.
                    because = $"shown because {card.Nationality} is not "
                            + $"{configuration.HomeCountry}, this property's home country",
                    rows = Lines(RegistrationFields.Foreign, card, configuration, required),
                }
                : null,

            closing = Lines(RegistrationFields.Closing, card, configuration, required),

            // What the property's own configuration says is still wanted. The
            // card is saved whatever this holds — S19b — so it is a prompt and
            // never a gate.
            missing = RegistrationRule.Missing(configuration, card),

            // **Stated, never enforced.** Null where this property files
            // nothing, or where this guest is outside its reporting scope.
            obligation = due is not { } when
                ? null
                : new { at = when.ToString("O"), hours = configuration.ReportingDueHours },

            signedAt = card.SignedAt?.ToString("O"),
        };
    }

    /// <summary>The card's number, or the number the property would mint for it.</summary>
    /// <remarks>
    /// <b>Minting is the save's, and this must not do it.</b>
    /// <c>SettingsService.MintCardNumber</c> advances the series; a read that
    /// called it would burn a number every time somebody opened a card and
    /// closed it, and a gap in the series is a question a property gets asked.
    /// </remarks>
    private static object Series(GuestOpsSettings configuration, Registration card)
        => card.CardNumber is { } taken
            ? new { number = taken, taken = true }
            : new
            {
                number = $"{configuration.CardNumberPrefix}{configuration.NextCardNumber}",
                taken = false,
            };

    /// <summary>One block's lines, each carrying one or two boxes.</summary>
    private static object[] Lines(
        IReadOnlyList<IReadOnlyList<CardBox>> block,
        Registration card,
        GuestOpsSettings configuration,
        IReadOnlySet<string> required)
        => [.. block.Select(line => line.Count == 1
            ? new { kind = "one", field = Box(line[0], card, configuration, required) }
            : (object)new
            {
                kind = "pair",
                fields = line.Select(box => Box(box, card, configuration, required)).ToArray(),
            })];

    /// <summary>One box: what it is, what it holds, and whether this property wants it.</summary>
    private static object Box(
        CardBox box,
        Registration card,
        GuestOpsSettings configuration,
        IReadOnlySet<string> required)
    {
        var value = RegistrationRule.ValueOf(card, box.Name);

        return new
        {
            name = box.Name,
            label = box.Label,
            kind = box.Kind,
            value,

            // **Masked at rest, whole underneath** — the drawing shows
            // `P•••••4412` and the desk holding the passport has to be able to
            // retype it. The screen reveals the whole value when somebody types
            // into the box, which is the only moment it is needed.
            masked = box.Secret ? Mask(value) : null,

            tall = box.Tall,
            placeholder = box.Placeholder,

            // The property's list, in the property's words, and only where the
            // box is a chooser. An empty list is a property that accepts no
            // document, which is a configuration to fix rather than a default
            // to supply.
            choices = box.Kind == "choice" ? configuration.AcceptedIdTypes : null,

            required = required.Contains(box.Name),
        };
    }

    /// <summary>
    /// The last four characters, and dots for the rest.
    /// </summary>
    /// <remarks>
    /// <b>Enough to check a page against a record, and not the number.</b> A
    /// value too short to mask is shown as dots alone rather than as itself —
    /// masking that gives up on short inputs is the mask that fails open.
    /// </remarks>
    private static string? Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return value.Length <= 4
            ? new string('•', value.Length)
            : new string('•', value.Length - 4) + value[^4..];
    }
}
