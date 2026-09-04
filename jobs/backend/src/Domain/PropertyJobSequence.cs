namespace HotelOS.Jobs.Domain;

/// <summary>One counter per property — S1 D3: the number in <c>MRN-ENG-142</c> is shared across departments.</summary>
public class PropertyJobSequence
{
    public Guid PropertyId { get; set; }

    /// <summary>The upper-cased property code the number carries, cached from Master Data at first use.</summary>
    public string PropertyCode { get; set; } = string.Empty;

    public long Next { get; set; } = 1;
}
