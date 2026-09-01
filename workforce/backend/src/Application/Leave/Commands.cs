namespace HotelOS.Workforce.Application.Leave;

/// <summary>Ask for somebody to be away.</summary>
/// <remarks>
/// Three fields and a person. Workforce is a manager and HR application, so most
/// of these are raised on somebody's behalf — and the record says so through
/// <c>entered_by</c> rather than through a second command.
/// </remarks>
public sealed record RaiseLeaveCommand
{
    /// <summary>Who would be away.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>Which kind of leave.</summary>
    public required Guid LeaveTypeId { get; init; }

    /// <summary>The first day away.</summary>
    public required DateOnly From { get; init; }

    /// <summary>The last day away.</summary>
    public required DateOnly To { get; init; }

    /// <summary>Why.</summary>
    public string? Note { get; init; }
}

/// <summary>Approve, decline or cancel a request.</summary>
/// <remarks>
/// One command for three decisions, because they differ in what they <i>mean</i>
/// rather than in what they carry — and three near-identical records would drift
/// apart the first time one of them gained a field.
/// </remarks>
public sealed record DecideLeaveCommand
{
    /// <summary>The request.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>What the approver wants recorded.</summary>
    public string? Note { get; init; }
}

/// <summary>Put a balance where HR says it should be.</summary>
public sealed record AdjustBalanceCommand
{
    /// <summary>Whose balance.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>Which kind of leave.</summary>
    public required Guid LeaveTypeId { get; init; }

    /// <summary>Days, signed. Positive credits, negative debits.</summary>
    public required decimal Days { get; init; }

    /// <summary>Why. Required — an unexplained adjustment cannot be defended.</summary>
    public required string Note { get; init; }
}

/// <summary>Configure one leave type.</summary>
public sealed record SetLeaveTypeCommand
{
    /// <summary>The type, or null to add one.</summary>
    public Guid? Id { get; init; }

    /// <summary>The version the caller read, when amending.</summary>
    public long? ExpectedVersion { get; init; }

    /// <summary>Stable within the property.</summary>
    public required string Code { get; init; }

    /// <summary>What people read.</summary>
    public required string Name { get; init; }

    /// <summary>Days accrued each month, or null when granted by hand.</summary>
    public decimal? AccrualPerMonth { get; init; }
}
