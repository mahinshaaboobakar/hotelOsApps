namespace HotelOS.Workforce.Application.Teams;

/// <summary>Form a team in one department.</summary>
public sealed record FormTeamCommand
{
    /// <summary>The department it works in — the canon code.</summary>
    public required string DepartmentCode { get; init; }

    /// <summary>What the property calls it.</summary>
    public required string Name { get; init; }
}

/// <summary>Rename a team, or stand it down and back up.</summary>
/// <remarks>
/// One command for the three, because they carry the same two things and
/// splitting them would be three near-identical records — the shape
/// <see cref="Swaps.DecideSwapCommand"/> already settled here.
/// </remarks>
public sealed record AmendTeamCommand
{
    /// <summary>The team.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>Its new name, or null to leave it alone.</summary>
    public string? Name { get; init; }
}

/// <summary>Put somebody in a team, or take them out.</summary>
public sealed record TeamMembershipCommand
{
    /// <summary>The team.</summary>
    public required Guid TeamId { get; init; }

    /// <summary>The person.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>
    /// The day it takes effect — joining or leaving.
    /// </summary>
    /// <remarks>
    /// Stated by the caller rather than defaulted to today, for the reason a
    /// posting states its own: a supervisor forming next week's crew on Friday
    /// is describing next week, and a date this service chose would be a fact
    /// nobody decided.
    /// </remarks>
    public required DateOnly On { get; init; }
}
