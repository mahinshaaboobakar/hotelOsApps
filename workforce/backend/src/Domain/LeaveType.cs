namespace HotelOS.Workforce.Domain;

/// <summary>A kind of leave this property grants, and how it accrues.</summary>
/// <remarks>
/// <para>
/// Property-configured, seeded from <see cref="LeaveTemplates"/> and editable
/// afterwards. <b>Week-off is not here</b> — <c>WF-Q12</c> makes it a rota
/// marker, an <i>off</i> entry in the shift catalogue with no request and no
/// balance, which is why the owner's list is four and chapter 01's was five.
/// </para>
/// <para>
/// <b>A rate, not an annual allowance.</b> The owner's <i>"monthly 2"</i> cannot
/// be expressed as a number granted on 1 January: a balance that appears in full
/// in January is not the balance a property running two-a-month has in March.
/// What stays refused is the rest of the accrual machinery — carry-forward and
/// its caps, encashment, pro-rata on joining, expiry, tenure slabs — none of
/// which the owner described and all of which usually arrive together.
/// </para>
/// </remarks>
public class LeaveType
{
    /// <summary>This type's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Stable within the property — what a ledger entry names.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>What people read.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Days accrued each month, or null when it is granted by hand.</summary>
    /// <remarks>
    /// Null is comp-off's case in v1 — <c>WF-Q13</c>: the numbers count holidays
    /// worked and HR grants the credit through an adjustment. Null is not zero: a
    /// type that accrues nothing automatically is different from one that accrues
    /// a rate of none.
    /// </remarks>
    public decimal? AccrualPerMonth { get; set; }

    /// <summary>Retired types stop being offered and keep their history.</summary>
    public bool Active { get; set; } = true;

    /// <summary>When it was configured.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }
}
