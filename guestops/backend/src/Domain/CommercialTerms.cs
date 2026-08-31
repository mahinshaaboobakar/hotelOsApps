namespace HotelOS.GuestOps.Domain;

/// <summary>
/// What the stay was sold on — GUEST-Q6's half of the line.
/// </summary>
/// <remarks>
/// <para>
/// v1 carries the <b>terms</b>; the <b>folio</b> — posting, payments,
/// settlement, invoicing, the night-audit posting — is Finance's later round.
/// The accepted consequence is recorded rather than left implicit: a standalone
/// property cannot settle a guest in v1, and the first deployments are
/// PMS-connected.
/// </para>
/// <para>
/// <b>Offsets, never resolved deadlines</b> — R18. An offset from arrival
/// survives the arrival date changing and a resolved timestamp does not, so a
/// deadline is computed when it is displayed. A cancellation deadline that
/// silently stops matching its reservation is a chargeable error — which is
/// what the system this replaces produced, by keeping two pre-formatted human
/// strings and discarding the structure behind them.
/// </para>
/// </remarks>
public class CommercialTerms
{
    public Guid StayId { get; set; }

    public string? RateCode { get; set; }

    public string? RateName { get; set; }

    public Money? Amount { get; set; }

    public string? GuaranteeCode { get; set; }

    public string? GuaranteeDescription { get; set; }

    public bool OnHold { get; set; }

    /// <summary>Whether this booking holds a room — R18's flag.</summary>
    /// <remarks>
    /// Precisely <i>"does this booking hold inventory"</i>, asked of the system
    /// that knows. <see cref="Lifecycle.HoldsInventory"/> answers by state for a
    /// stay with no stated terms; where the source has said, this is the better
    /// answer and availability uses it.
    /// </remarks>
    public bool ReservesInventory { get; set; }

    public bool IsDefault { get; set; }

    /// <summary>Deposit due this many days after the booking was made.</summary>
    public int? DepositOffsetDaysFromBooking { get; set; }

    /// <summary>Cancellation free until this many days before arrival.</summary>
    public int? CancelOffsetDaysFromArrival { get; set; }

    /// <summary>The time of day the cancellation window closes.</summary>
    public TimeOnly? CancelDropTime { get; set; }

    public Money? PenaltyAmount { get; set; }

    /// <summary>Some sources state the penalty in nights rather than money.</summary>
    /// <remarks>
    /// Carried alongside rather than converted: a penalty stated in nights
    /// cannot be turned into an amount without a rate the fact does not have,
    /// and inventing one would be a chargeable guess.
    /// </remarks>
    public int? PenaltyNights { get; set; }

    public string? PenaltyBasis { get; set; }

    public RoomStay? Stay { get; set; }

    /// <summary>
    /// When cancelling stops being free, computed from the offset — never stored.
    /// </summary>
    /// <remarks>
    /// R18's whole point. Move the arrival and the deadline moves with it; a
    /// stored date would silently stop matching the reservation it belongs to.
    /// </remarks>
    /// <param name="arrival">The stay's arrival date.</param>
    public DateTimeOffset? CancellationDeadline(DateOnly? arrival)
    {
        if (arrival is not { } date || CancelOffsetDaysFromArrival is not { } days)
        {
            return null;
        }

        var deadline = date.AddDays(-days);
        var dropTime = CancelDropTime ?? new TimeOnly(0, 0);

        // No zone applied here: the caller renders it in the property's, which
        // is the only clock a hotel's deadlines mean anything in. Returning an
        // instant built in the server's zone would be R16's failure wearing a
        // different field.
        return new DateTimeOffset(deadline.ToDateTime(dropTime), TimeSpan.Zero);
    }
}

/// <summary>
/// The kept set, and what is retained beyond it — GUEST-Q7.
/// </summary>
/// <remarks>
/// <para>
/// Named explicitly, because <b>a field is kept by decision, never by accident
/// of the payload</b>. These are what the PMS sends on a reservation and every
/// hotel reports on; a fact not recorded when it arrives is unrecoverable, which
/// is the walk-in flag's argument applied to the rest of the row.
/// </para>
/// </remarks>
public class StaySource
{
    public Guid StayId { get; set; }

    /// <summary>Direct · OTA · corporate · walk-in, or the source's own code.</summary>
    public string? Channel { get; set; }

    /// <summary>As sent — a reference, not a profile.</summary>
    public string? TravelAgent { get; set; }

    public string? MarketCode { get; set; }

    /// <summary>EP · CP · MAP · AP, or the source's own code.</summary>
    public string? MealPlan { get; set; }

    public int Adults { get; set; }

    public int Children { get; set; }

    public RoomStay? Stay { get; set; }

    public ICollection<StaySourceDetail> Detail { get; set; } = [];
}

/// <summary>A significant field the source sent and this model does not name.</summary>
/// <remarks>
/// <para>
/// <b>The R25 lesson turned around.</b> The reference dropped a reservation with
/// no phone and fabricated an email when a downstream field demanded one — two
/// ways of losing the truth. Retention is the third option: what is not yet
/// modelled is kept as it arrived, so the day it earns a column the history is
/// there and nobody reconstructs it.
/// </para>
/// <para>
/// <b>Two limits, so this does not become a dumping ground.</b> It is
/// <b>never read to drive behaviour</b> — a field that decides something gets
/// modelled first — and it is <b>not a second copy of the raw payload</b>, which
/// is the Hub's inbox and stays there (ADR 0128 §5).
/// </para>
/// </remarks>
public class StaySourceDetail
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }

    /// <summary>The source's own name for it, unaltered.</summary>
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    /// <summary>Which integration sent it.</summary>
    /// <remarks>
    /// Two flavours of one PMS can use one name for different things, so the
    /// key alone does not identify the meaning.
    /// </remarks>
    public string IntegrationId { get; set; } = string.Empty;

    public StaySource? Source { get; set; }
}
