namespace HotelOS.Workforce.Application.Shifts;

/// <summary>Add a shift to the property's catalogue.</summary>
public sealed record CreateShiftCommand
{
    /// <summary>What people read.</summary>
    public required string Name { get; init; }

    /// <summary>What fits a rota cell, and survives a photocopier.</summary>
    public required string ShortCode { get; init; }

    /// <summary>How the week reads at a glance.</summary>
    public required string Colour { get; init; }

    /// <summary>The hours, from the day the catalogue entry begins.</summary>
    public required ShiftHoursCommand Hours { get; init; }

    /// <summary>The first day this shift may be assigned.</summary>
    public required DateOnly EffectiveFrom { get; init; }
}

/// <summary>Change how a shift reads. Not its hours.</summary>
/// <remarks>
/// Display attributes change <b>in place</b>: renaming <i>Morning</i> to
/// <i>AM</i> does not corrupt a past rota, and versioning the name would make
/// one shift appear under two names in one week's history. What
/// <c>WF-Q15</c> protects is what was <i>worked</i>, and that is the times —
/// which is <see cref="RescheduleShiftCommand"/>.
/// </remarks>
public sealed record RenameShiftCommand
{
    /// <summary>The catalogue entry.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>New name, or null to leave it.</summary>
    public string? Name { get; init; }

    /// <summary>New short code, or null to leave it.</summary>
    public string? ShortCode { get; init; }

    /// <summary>New colour, or null to leave it.</summary>
    public string? Colour { get; init; }
}

/// <summary>Change a shift's hours, forward from a chosen date.</summary>
/// <remarks>
/// <c>WF-Q15</c>. The date is the manager's and is required: defaulting it to
/// today would silently apply an edit to a rota already published for this week,
/// and defaulting it to tomorrow would be a guess about when the change takes
/// effect. Somebody decides, and the record says who and when.
/// </remarks>
public sealed record RescheduleShiftCommand
{
    /// <summary>The catalogue entry whose hours change.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>The new hours.</summary>
    public required ShiftHoursCommand Hours { get; init; }

    /// <summary>The first day they apply.</summary>
    public required DateOnly EffectiveFrom { get; init; }
}

/// <summary>Stop offering a shift, keeping every rota it was worked under.</summary>
public sealed record RetireShiftCommand
{
    /// <summary>The catalogue entry.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }
}

/// <summary>A set of hours, as a caller states them.</summary>
/// <remarks>
/// <para>
/// All four times absent is an <b>off</b> shift — <c>WF-Q12</c>'s week-off, a
/// rota marker with no request and no balance. It is expressed by absence rather
/// than by a flag, so an off shift carrying times cannot be written.
/// </para>
/// <para>
/// The second pair makes a split shift. Present without the first is refused:
/// there is no shift whose second span exists and whose first does not.
/// </para>
/// </remarks>
public sealed record ShiftHoursCommand
{
    /// <summary>When it starts, or null for an off shift.</summary>
    public TimeOnly? StartsAt { get; init; }

    /// <summary>When it ends. Earlier than the start crosses midnight.</summary>
    public TimeOnly? EndsAt { get; init; }

    /// <summary>A second span's start, for a split shift.</summary>
    public TimeOnly? SecondStartsAt { get; init; }

    /// <summary>A second span's end.</summary>
    public TimeOnly? SecondEndsAt { get; init; }
}
