namespace HotelOS.Workforce.Domain;

/// <summary>
/// One person's place in a team, over a stretch of time.
/// </summary>
/// <remarks>
/// <para>
/// <b>Effective-dated, like a posting</b>, because the question a report asks is
/// <i>who was in this team in March</i> and a row that were simply deleted could
/// not answer it. A live membership is one whose <see cref="LeftOn"/> is null.
/// </para>
/// <para>
/// <b>A member holds a posting in force in the team's department.</b> A team
/// exists to receive work in that department, so a member who cannot be assigned
/// there is a row that lies — and the consequence runs the other way too: ending
/// a posting ends the membership, in the same transaction, or a team routes work
/// to somebody who left the department last month with nothing anywhere saying
/// so.
/// </para>
/// </remarks>
public class TeamMember
{
    /// <summary>This membership's id.</summary>
    public Guid Id { get; set; }

    /// <summary>The property — carried so every query is scoped by it.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Which team.</summary>
    public Guid TeamId { get; set; }

    /// <summary>Master Data's person.</summary>
    public Guid StaffId { get; set; }

    /// <summary>The first day they were in it.</summary>
    public DateOnly JoinedOn { get; set; }

    /// <summary>The last day, or null while they are still in it.</summary>
    public DateOnly? LeftOn { get; set; }

    /// <summary>When the row was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }

    /// <summary>Is this membership live on a given day?</summary>
    /// <param name="on">The day.</param>
    /// <returns>Whether they were in the team then.</returns>
    public bool IsInForceOn(DateOnly on) =>
        JoinedOn <= on && (LeftOn is null || LeftOn >= on);
}
