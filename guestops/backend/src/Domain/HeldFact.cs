namespace HotelOS.GuestOps.Domain;

/// <summary>Why a fact is being held rather than applied.</summary>
public enum HeldReason
{
    /// <summary>
    /// It may be a stay this property already created — GUEST-Q5.
    /// </summary>
    /// <remarks>
    /// Same room, overlapping dates, and no external reference we know. A person
    /// decides whether it is the same stay; until then it applies to nothing.
    /// </remarks>
    CandidateLink = 1,
}

/// <summary>
/// A normalised fact received and deliberately not applied.
/// </summary>
/// <remarks>
/// <para>
/// <b>Held, never published.</b> The alternative — create the PMS's stay,
/// announce it, then merge — tells every consumer about a stay we intend to
/// withdraw, and there is no honest event for that withdrawal. Holding keeps
/// GUEST-Q5's two outcomes clean: confirming applies this fact to the local
/// stay, rejecting creates the second stay and applies it there.
/// </para>
/// <para>
/// <b>It is not a copy of the raw payload.</b> That is the Hub's inbox and
/// stays there (ADR 0128 §5). This is the normalised fact, in the shape this
/// application would have applied — enough to apply later, and nothing more.
/// </para>
/// <para>
/// <b>Later facts for the same reservation queue behind it.</b> A check-in
/// arriving while its booking is undecided is held too, and applies wherever
/// the decision sends them — so a rejected candidate does not leave half a
/// history on the wrong stay.
/// </para>
/// </remarks>
public class HeldFact
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>The connector that produced it — ADR 0020's closed set.</summary>
    public string IntegrationId { get; set; } = string.Empty;

    /// <summary>The fact, as this application would have applied it.</summary>
    /// <remarks>
    /// Serialised rather than modelled into columns: it is inert while held —
    /// nothing queries inside it — and a column set would be a second
    /// definition of the fact that drifts from the contract it came from.
    /// </remarks>
    public string Payload { get; set; } = string.Empty;

    /// <summary>What the fact says the stay has reached.</summary>
    /// <remarks>
    /// Lifted out of the payload because the Attention list shows it: a person
    /// deciding a candidate wants to see *"Opera says checked in"* without the
    /// screen parsing a blob.
    /// </remarks>
    public StayLifecycle Lifecycle { get; set; }

    public HeldReason Reason { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>Null until a decision applies or discards it.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }
}
