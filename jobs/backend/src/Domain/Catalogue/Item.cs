namespace HotelOS.Jobs.Domain.Catalogue;

/// <summary>
/// A catalogue item — S1 D5/D6: the thing a job is about ("Not cooling"),
/// carrying the defaults a raised job starts from: priority, due-within,
/// restricted (S8 D4), whether a guest may ask for it.
/// </summary>
public class Item
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid CategoryId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>P1 · P2 · P3 — the catalogue link of the priority chain (S1 D4).</summary>
    public string DefaultPriority { get; set; } = Priority.P3;

    /// <summary>The item's own promise; null means "the category's, else the department's" (§2.3).</summary>
    public int? DueWithinMinutes { get; set; }

    public bool RestrictedByDefault { get; set; }

    public bool GuestRequestable { get; set; } = true;

    /// <summary>NONE · OPTIONAL · REQUIRED.</summary>
    public string PhotoOnCompletion { get; set; } = PhotoRule.Optional;

    public bool Active { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
}

/// <summary>Whether resolving needs a photo.</summary>
public static class PhotoRule
{
    public const string None = "NONE";
    public const string Optional = "OPTIONAL";
    public const string Required = "REQUIRED";

    public static readonly IReadOnlyList<string> All = [None, Optional, Required];
}
