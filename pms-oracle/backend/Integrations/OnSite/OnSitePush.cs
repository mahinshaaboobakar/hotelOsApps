namespace PmsOracle.Integrations.OnSite;

/// <summary>
/// One message from an on-site OPERA agent, exactly as it arrives on the wire.
/// </summary>
/// <remarks>
/// <para>
/// Shared by <c>oracle-onpremise</c> and <c>oracle-web</c>: the study found the
/// two flavours send the same flat PascalCase shape, and one wire model for two
/// integrations is honest where the wire is genuinely the same. What differs
/// between them is the endpoint and the credential, which are configuration,
/// not payload.
/// </para>
/// <para>
/// <b>Every field is a string, including the money and the dates, because that
/// is what arrives.</b> Parsing belongs in normalisation where a failure can be
/// carried as a rejection naming the value; a type that refused to hold what
/// the agent actually sent would move the failure to deserialisation, where the
/// only thing left to report is that a message could not be read at all. The
/// reference stored <c>Amount</c> as a string and called
/// <c>Float.parseFloat</c> at the point of use, four call sites apart — the
/// storage was right and the parsing had no home.
/// </para>
/// <para>
/// <b>One message describes one room.</b> <see cref="NoOfRooms"/> may say three
/// while this payload carries one, which is requirement R9 and the reason the
/// normalised fact is anchored to the room-stay with the booking group possibly
/// incomplete.
/// </para>
/// </remarks>
public sealed record OnSitePush
{
    /// <summary>The PMS's reservation number. Absent on some check-in halves.</summary>
    public string? ReservationId { get; init; }

    /// <summary>The status value — read through the on-site vocabulary, never folded for case.</summary>
    /// <remarks>
    /// Its casing decides which half of a two-part check-in this is, so it is
    /// carried verbatim and compared ordinally.
    /// </remarks>
    public string? Status { get; init; }

    /// <summary>Family name, and part of the join key when a check-in arrives in halves.</summary>
    public string? Surname { get; init; }

    /// <summary>Given name, and part of the join key.</summary>
    public string? FirstName { get; init; }

    /// <summary>Arrival date, and the third part of the join key.</summary>
    public string? ArrivalDate { get; init; }

    /// <summary>Departure date.</summary>
    public string? DepartureDate { get; init; }

    /// <summary>The room number, when this message carries one.</summary>
    public string? RoomNo { get; init; }

    /// <summary>The PMS's room-type code, mapped through the property's room types.</summary>
    public string? RoomType { get; init; }

    /// <summary>How many rooms the booking has — often more than this message describes.</summary>
    public string? NoOfRooms { get; init; }

    /// <summary>Adults on this room-stay.</summary>
    public string? PaxAdults { get; init; }

    /// <summary>Children on this room-stay.</summary>
    public string? PaxKids { get; init; }

    /// <summary>Primary telephone, when the contact half of a check-in carries it.</summary>
    public string? Phone1 { get; init; }

    /// <summary>Secondary telephone.</summary>
    public string? Phone2 { get; init; }

    /// <summary>Email address.</summary>
    public string? Email { get; init; }

    /// <summary>The stay's total, unparsed — see the note on strings above.</summary>
    public string? Amount { get; init; }

    /// <summary>Meal plan code.</summary>
    public string? MealPlan { get; init; }

    /// <summary>Market segment code.</summary>
    public string? MarketCode { get; init; }

    /// <summary>Booking source.</summary>
    public string? Source { get; init; }

    /// <summary>Travel agent, where the booking came through one.</summary>
    public string? TravelAgent { get; init; }

    /// <summary>The PMS's own guest identifier.</summary>
    public string? UniquePersonId { get; init; }

    /// <summary>
    /// The property this message claims to be about.
    /// </summary>
    /// <remarks>
    /// <b>Claims, and is not believed.</b> The ingress identifies the
    /// configured integration from the URL it was posted to, and this value is
    /// checked against it. The reference took the property from this field and
    /// trusted it, on an endpoint with no authentication at all — so a body was
    /// enough to write into any property.
    /// </remarks>
    public string? PropertyCode { get; init; }
}
