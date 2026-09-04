namespace HotelOS.Workforce.Domain;

/// <summary>
/// A named group of posted staff within one department, formed to be assigned
/// work.
/// </summary>
/// <remarks>
/// <para>
/// <b>Workforce's, whole</b> — ruled 2026-09-04 on Jobs' <c>S3-D1</c>. The
/// argument is worth keeping beside the type, because the precedent that looks
/// decisive points the other way: ADR 0063 §Q4 kept <c>Zone</c> in Master Data
/// and sent only <c>RoomZoneAssignment</c> to the application, and read
/// mechanically that would split a team the same way — the name to Core, the
/// membership here.
/// </para>
/// <para>
/// It does not transfer, and the difference is one word. <b>A zone is a
/// place</b>: the West Wing is an area of the property whether or not anybody
/// works it. <b>A team is people</b>, and on ADR 0051's test — <i>if every
/// application except Core Administration were uninstalled, would this still
/// describe what the entity is?</i> — "Housekeeping Team A" is a list of nobody,
/// because there are no postings for anyone to be in it. Every people-grouping
/// in ADR 0063 §Q5 went to Workforce; every structural hierarchy that stayed in
/// Core is a place or the organization's shape.
/// </para>
/// <para>
/// <b>A team is not a zone, and both exist.</b> A zone is <i>where</i> the work
/// is and is already on the posting (<c>WF-Q7</c>); a team is <i>who</i> does it
/// together. A property that organises by area assigns to zones, one that
/// organises by crew assigns to teams, and the posting is where the two meet.
/// </para>
/// </remarks>
public class Team
{
    /// <summary>This team's id.</summary>
    public Guid Id { get; set; }

    /// <summary>The property it belongs to.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>
    /// The one department it works in — the canon code, ADR 0119.
    /// </summary>
    /// <remarks>
    /// <b>Exactly one, in v1, and by ruling.</b> Assignment routing is
    /// departmental: Jobs' pool, its concern policy and its accountability
    /// ladder are all per department, so a team spanning two makes <i>which
    /// pool does this job sit in</i> unanswerable. A cross-department task force
    /// is a real thing, is not this, and is asked for when a property asks.
    /// </remarks>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>What the property calls it.</summary>
    /// <remarks>
    /// Free text and no code list. A department is the industry's vocabulary
    /// and is canon; a team is the hotel's own — "Team A", "Morning Crew",
    /// "Tower Block" — and inventing a canon for it would be inventing a
    /// vocabulary nobody asked for.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether it is in use — ADR 0062's canonical flag.</summary>
    /// <remarks>
    /// The verbs are <b>Deactivate / Reactivate</b>, never Archive / Restore. A
    /// team stood down for the season stops being offered and does not vanish,
    /// because work assigned to it is in somebody's history.
    /// </remarks>
    public bool Active { get; set; } = true;

    /// <summary>Logical removal — ADR 0062's second column.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>When it was formed.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }
}
