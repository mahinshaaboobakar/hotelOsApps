using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Application.Attendance;

/// <summary>Record what somebody actually did on a business day.</summary>
/// <remarks>
/// <b>One command for arriving, leaving and being absent</b>, because they are
/// one record seen at different moments. A separate <c>ClockIn</c> and
/// <c>ClockOut</c> would suit a device and fight a supervisor, who enters both at
/// four in the afternoon from a paper sheet — and v1's only writer is the
/// supervisor.
/// </remarks>
public sealed record RecordAttendanceCommand
{
    /// <summary>Whose day.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>The business day.</summary>
    public required DateOnly BusinessDate { get; init; }

    /// <summary>When they arrived, or null for an absence.</summary>
    public TimeOnly? InAt { get; init; }

    /// <summary>When they left, or null while the shift is open.</summary>
    public TimeOnly? OutAt { get; init; }

    /// <summary>Where this came from. Manual is v1's only writer.</summary>
    public AttendanceSource Source { get; init; } = AttendanceSource.Manual;

    /// <summary>What the device or import called this event.</summary>
    public string? ExternalReference { get; init; }

    /// <summary>Anything worth recording.</summary>
    public string? Note { get; init; }
}

/// <summary>Correct a record already written.</summary>
/// <remarks>
/// Amending is ordinary rather than exceptional: somebody forgot to sign out,
/// a sheet was misread, a device double-read. What is <b>not</b> ordinary is a
/// correction nobody can see, which is why the record keeps its provenance and
/// its version.
/// </remarks>
public sealed record AmendAttendanceCommand
{
    /// <summary>The record.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>A corrected arrival, or null to leave it.</summary>
    public TimeOnly? InAt { get; init; }

    /// <summary>A corrected departure, or null to leave it.</summary>
    public TimeOnly? OutAt { get; init; }

    /// <summary>Clear the arrival — marking somebody absent after all.</summary>
    /// <remarks>
    /// Explicit, because <c>null</c> already means <i>"leave it alone"</i> on the
    /// two fields above. Without this a supervisor could never undo a mistaken
    /// arrival, and the alternative — deleting and re-entering — loses the trail.
    /// </remarks>
    public bool ClearIn { get; init; }

    /// <summary>Clear the departure — reopening a shift closed by mistake.</summary>
    public bool ClearOut { get; init; }

    /// <summary>Why the correction was made.</summary>
    public string? Note { get; init; }
}

/// <summary>Which days to look at.</summary>
public sealed record AttendanceQuery
{
    /// <summary>The first business day.</summary>
    public required DateOnly From { get; init; }

    /// <summary>The last business day.</summary>
    public required DateOnly To { get; init; }

    /// <summary>Only this department, or null for the property.</summary>
    public string? DepartmentCode { get; init; }

    /// <summary>Only this person, or null for everybody.</summary>
    public Guid? StaffId { get; init; }
}
