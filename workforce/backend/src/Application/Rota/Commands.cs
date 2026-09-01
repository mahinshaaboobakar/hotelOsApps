namespace HotelOS.Workforce.Application.Rota;

/// <summary>Put somebody on a shift for a day.</summary>
/// <remarks>
/// Assigning where a cell is already filled <b>replaces</b> it — that is what
/// clicking a filled cell and choosing a different shift means, and refusing it
/// would make the rota unusable. What never replaces silently is
/// <see cref="CopyWeekCommand"/>.
/// </remarks>
public sealed record AssignShiftCommand
{
    /// <summary>Who works.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>Which day.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Which shift from the property's catalogue.</summary>
    public required Guid CatalogueEntryId { get; init; }

    /// <summary>Which department the day is worked for.</summary>
    /// <remarks>
    /// Required, because a person with two postings can be rostered to either
    /// and the rota must say which. The caller passes the canon code the
    /// person is posted to.
    /// </remarks>
    public required string DepartmentCode { get; init; }

    /// <summary>A one-off start for this day only.</summary>
    public TimeOnly? OverrideStartsAt { get; init; }

    /// <summary>The one-off end.</summary>
    public TimeOnly? OverrideEndsAt { get; init; }
}

/// <summary>Empty a cell.</summary>
public sealed record ClearShiftCommand
{
    /// <summary>Whose day.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>Which day.</summary>
    public required DateOnly Date { get; init; }
}

/// <summary>Copy a week's rota forward, filling empty cells only.</summary>
/// <remarks>
/// <b>Fills empty cells only</b> — the mockup's own caption, and the rule that
/// makes the button safe to press. Overwriting would silently undo a decision
/// somebody had already made about the new week, and a manager who wanted that
/// would say so by clearing the cell first.
/// </remarks>
public sealed record CopyWeekCommand
{
    /// <summary>The Monday of the week to copy from.</summary>
    public required DateOnly From { get; init; }

    /// <summary>The Monday of the week to copy into.</summary>
    public required DateOnly To { get; init; }

    /// <summary>Only this department's people, or null for the property.</summary>
    public string? DepartmentCode { get; init; }
}

/// <summary>Exchange two people's shifts on their days.</summary>
/// <remarks>
/// <para>
/// <b>The manager's tool, and consent-free</b> — <c>WF-Q9</c>'s two-verb split.
/// This is an action: pick two cells, exchange them, done, because the manager
/// is the authority. The staff-initiated proposal with its accept step is a
/// different object and arrives in slice 4.
/// </para>
/// <para>
/// <b>Both cells change or neither does.</b> One transaction: a half-applied
/// swap leaves one person covering two shifts and the other none.
/// </para>
/// </remarks>
public sealed record SwapShiftsCommand
{
    /// <summary>One cell.</summary>
    public required Guid FirstAssignmentId { get; init; }

    /// <summary>The other.</summary>
    public required Guid SecondAssignmentId { get; init; }
}

/// <summary>Which cells to read.</summary>
public sealed record RotaQuery
{
    /// <summary>The first day shown.</summary>
    public required DateOnly From { get; init; }

    /// <summary>The last day shown.</summary>
    public required DateOnly To { get; init; }

    /// <summary>Only this department, or null for the property.</summary>
    public string? DepartmentCode { get; init; }

    /// <summary>Only this person, or null for everybody.</summary>
    public Guid? StaffId { get; init; }
}

/// <summary>Set the property's overtime threshold.</summary>
/// <remarks>
/// Null clears a threshold rather than meaning zero: a property that has not set
/// one must not have every rota flagged.
/// </remarks>
public sealed record SetOvertimeThresholdCommand
{
    /// <summary>Hours in a day after which the rota warns, or null.</summary>
    public decimal? DailyHours { get; init; }

    /// <summary>Hours in a week after which it warns, or null.</summary>
    public decimal? WeeklyHours { get; init; }
}
