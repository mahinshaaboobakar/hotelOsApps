namespace HotelOS.Jobs.Domain.Catalogue;

/// <summary>Another way of saying an item — R8: "AC not working" finds "Not cooling". Search only, never routing.</summary>
public class ItemAlias
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public string Alias { get; set; } = string.Empty;

    /// <summary>BCP-47, or null for any language.</summary>
    public string? Language { get; set; }
}
