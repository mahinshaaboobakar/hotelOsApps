namespace HotelOS.Workforce.Application.Duties;

/// <summary>Assign the Manager on Duty over a span.</summary>
/// <remarks>
/// Two datetimes, not a date — <c>WF-Q8</c>. A duty running 20:00 → 08:00 covers
/// two dates, and no per-day shape can say so.
/// </remarks>
public sealed record AssignDutyCommand
{
    /// <summary>Who holds it — any active staff member, from any department.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>When the duty begins.</summary>
    public required DateTimeOffset StartsAt { get; init; }

    /// <summary>When it ends.</summary>
    public required DateTimeOffset EndsAt { get; init; }

    /// <summary>What the incoming manager should know. Optional, and blocks nothing.</summary>
    public string? HandoverNote { get; init; }
}

/// <summary>Amend a duty already on the register.</summary>
/// <remarks>
/// The holder may change — somebody swaps a night — and so may the span. Both
/// re-check the overlap, because either can make two duties cover one instant.
/// </remarks>
public sealed record AmendDutyCommand
{
    /// <summary>The duty to amend.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>A different holder, or null to leave it.</summary>
    public Guid? StaffId { get; init; }

    /// <summary>A different start, or null to leave it.</summary>
    public DateTimeOffset? StartsAt { get; init; }

    /// <summary>A different end, or null to leave it.</summary>
    public DateTimeOffset? EndsAt { get; init; }

    /// <summary>A new handover note, or null to leave it.</summary>
    public string? HandoverNote { get; init; }
}

/// <summary>Take a duty off the register.</summary>
public sealed record WithdrawDutyCommand
{
    /// <summary>The duty to withdraw.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }
}
