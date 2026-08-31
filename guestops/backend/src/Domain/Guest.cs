namespace HotelOS.GuestOps.Domain;

/// <summary>
/// A person as this property knows them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not a person-graph</b> — G360-Q1. GuestOps owns stays and guest identity
/// records; Guest360 owns the graph over them, and a merge there re-points the
/// person and rewrites no stay. That is precisely why <b>no <c>person_id</c> is
/// stored here</b>: a stay's link survives a merge it never hears about.
/// </para>
/// <para>
/// Two records that are probably one person stay two until somebody says
/// otherwise. What this application must not do is guess; what it must not
/// prevent is the later merge.
/// </para>
/// </remarks>
public class GuestIdentity
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>A name is not one field — R11.</summary>
    /// <remarks>
    /// <see cref="NameAsGiven"/> is kept because splitting is lossy and
    /// culturally wrong as often as not: a mononym, a patronymic, a name whose
    /// family part comes first. Where a source gives one string, that is it and
    /// the other two are empty.
    /// </remarks>
    public string? NameGiven { get; set; }

    public string? NameFamily { get; set; }

    public string NameAsGiven { get; set; } = string.Empty;

    /// <summary>Person-scoped and durable across stays — S19.</summary>
    /// <remarks>
    /// The distinction from a stay note is not decorative: a preference should
    /// be true next time, a note dies with the stay.
    /// </remarks>
    public string? Preferences { get; set; }

    public RecordOrigin Origin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public long Version { get; set; }

    public ICollection<ContactPoint> Contacts { get; set; } = [];
}

/// <summary>What kind of contact this is.</summary>
public enum ContactKind
{
    Phone = 1,
    Email = 2,
}

/// <summary>How to reach a guest.</summary>
/// <remarks>
/// <para>
/// <b>Encrypted at rest, with a blind index beside it.</b> GuestOps owns the
/// phone index ADR 0089 §CTX-Q2 left waiting for *"the domain that owns the
/// phone number"*: <c>aes_gcm_encrypt(field_key, e164(phone))</c> beside
/// <c>hmac_sha256(index_key, e164(phone))</c>, exact match only and no prefix
/// search. That is what makes Context's <i>phone → guest</i> a single index
/// seek rather than a table scan of decryptions.
/// </para>
/// <para>
/// <b>Masking is a screen rule, never a storage one</b> — GUEST-Q7. The value
/// is whole here and whole on the wire between services; it renders as
/// <c>+91 98470 •••• 12</c> where a person looks, and the reveal takes the
/// stay's write permission and is recorded. Masking alone is a speed bump a
/// busy desk routes around within a week; the trail is the control.
/// </para>
/// </remarks>
public class ContactPoint
{
    public Guid Id { get; set; }

    public Guid GuestId { get; set; }

    public ContactKind Kind { get; set; }

    /// <summary>The value, encrypted.</summary>
    public byte[] ValueCipher { get; set; } = [];

    /// <summary>An HMAC of the normalised form. Indexed; exact match only.</summary>
    public byte[] ValueIndex { get; set; } = [];

    /// <summary>The source's own classification — OHIP's <c>phoneTechType</c>.</summary>
    /// <remarks>
    /// *"The guest's phone number"* is a typed choice among several rather than
    /// a single field (R11).
    /// </remarks>
    public string? TechType { get; set; }

    public string? UseType { get; set; }

    /// <summary>Whether the source marked this primary — nullable on purpose.</summary>
    /// <remarks>
    /// Absent means the source said nothing; <c>false</c> means it said no. A
    /// plain <c>bool</c> would answer a question the PMS never asked.
    /// </remarks>
    public bool? IsPrimary { get; set; }

    public RecordOrigin Origin { get; set; }

    public GuestIdentity? Guest { get; set; }
}

/// <summary>One member of the party on a stay.</summary>
public class StayGuest
{
    public Guid StayId { get; set; }

    public Guid GuestId { get; set; }

    /// <summary>Whether this guest is the stay's primary — nullable on purpose.</summary>
    /// <remarks>
    /// R11: the source produces reservations where <b>nobody</b> is marked
    /// primary, and the system this replaces hard-failed on exactly that.
    /// *"Nobody is marked primary"* is a state, and <c>false</c> everywhere says
    /// something different from <c>null</c> everywhere.
    /// </remarks>
    public bool? IsPrimary { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    public RecordOrigin Origin { get; set; }

    public RoomStay? Stay { get; set; }

    public GuestIdentity? Guest { get; set; }
}
