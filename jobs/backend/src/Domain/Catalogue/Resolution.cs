namespace HotelOS.Jobs.Domain.Catalogue;

/// <summary>
/// A way a job gets fixed — S1 D7: "Filter replaced", "Refrigerant topped up".
/// Scoped to an item, or to every item of a category, or universal.
/// </summary>
public class Resolution
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Null means universal — offered on every item.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Null means every item of the category.</summary>
    public Guid? ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>True for "Other": the text box becomes mandatory.</summary>
    public bool NoteRequired { get; set; }

    public bool Active { get; set; } = true;

    public DateTimeOffset? DeletedAt { get; set; }
}
