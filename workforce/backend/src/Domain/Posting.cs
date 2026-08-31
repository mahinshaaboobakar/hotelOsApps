namespace HotelOS.Workforce.Domain;

/// <summary>
/// Where a person works, as what, and from when — the aggregate the rest of the
/// platform has been waiting for.
/// </summary>
/// <remarks>
/// <para>
/// ADR 0063 §Q5 removed three columns from Master Data because leaving them
/// there created two authorities for one question: <c>staff.department_id</c>,
/// <c>staff.reports_to_staff_id</c> and <c>departments.head_staff_id</c>. They
/// are all here, joined by the effective dating and the primary flag ADR 0052
/// sent from <c>StaffPropertyAssignment</c>, and by the zone <c>WF-Q7</c>
/// placed on the posting.
/// </para>
/// <para>
/// <b>This is the writer for <c>department#posted</c>.</b> ADR 0116 §6 makes
/// department-scoped authorization derive from Workforce postings *only,
/// permanently* — before this application has data, department membership is
/// empty by design, with no interim mechanism to replace. Every dormant
/// department folder grant in My Hotel comes alive the day the first posting
/// with an identity link is saved.
/// </para>
/// <para>
/// <b>No duplicated master data.</b> <see cref="StaffId"/> references Master
/// Data's person and nothing about them is copied here; departments are
/// referenced by <b>canon code</b> (ADR 0119 — the code is the identity, and a
/// property activates codes rather than creating them).
/// </para>
/// </remarks>
public class Posting
{
    /// <summary>This posting's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary. Every query is scoped by it.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Master Data's staff id — the person, never a copy of them.</summary>
    public Guid StaffId { get; set; }

    /// <summary>The department's canon code — ADR 0119.</summary>
    /// <remarks>
    /// A string rather than a foreign key to a local table, and rather than a
    /// UUID. The canon ships compiled into Master Data and is identical in every
    /// installation; a UUID cannot be both per-property and identical
    /// everywhere, and the code can. Reports group on it across a whole group of
    /// hotels with no mapping table.
    /// </remarks>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>What they do there.</summary>
    /// <remarks>
    /// Free text on purpose. The canon governs <i>departments</i>, which are the
    /// industry's vocabulary; a job title is the individual hotel's, and a
    /// closed list of them would be a platform release every time a property
    /// invented "Guest Experience Executive".
    /// </remarks>
    public string JobRole { get; set; } = string.Empty;

    /// <summary>The posting that answers "where does this person work?".</summary>
    /// <remarks>
    /// <c>WF-Q3</c>: multiple postings per person are structural — ADR 0052's
    /// primary flag exists for exactly this — and the UI keeps one primary plus
    /// additional. A person can be posted to Kitchen and to Banquets at once and
    /// still have one answer when something needs a single one.
    /// </remarks>
    public bool IsPrimary { get; set; }

    /// <summary>This person heads the department.</summary>
    /// <remarks>
    /// ADR 0063's relationship table puts <i>Department → current head Staff</i>
    /// here rather than on <c>masterdata.departments</c>, and this application
    /// is where it is set. It is an attribute of the posting rather than an
    /// operation on the department, which is why there is no
    /// <c>SetDepartmentHead</c> RPC.
    /// <para>
    /// It also decides who approves: the leave and swap approver resolves to the
    /// reporting manager when a posting names one, and to the department head
    /// otherwise.
    /// </para>
    /// </remarks>
    public bool IsDepartmentHead { get; set; }

    /// <summary>The zone this posting covers, or none.</summary>
    /// <remarks>
    /// <para>
    /// <c>WF-Q7</c>, owner 2026-08-31: <i>"from Workforce"</i> — zone assignment
    /// is a posting, not a Room Care morning allocation. It lives <b>on the
    /// posting</b> rather than in a standalone staff↔zone link because the
    /// posting already carries the department that gives the zone its meaning:
    /// <i>"Anita has zone 3"</i> is an incomplete fact, and
    /// <i>"as Housekeeping"</i> completes it. Putting it here makes the
    /// incomplete state inexpressible rather than merely discouraged.
    /// </para>
    /// <para>
    /// Nullable because most postings have none — a receptionist is posted to
    /// Front Office and to no area — and a required zone would make the ordinary
    /// case carry a field somebody has to invent a meaning for.
    /// </para>
    /// <para>
    /// The zone <i>entity</i> stays Master Data's (ADR 0063 kept it), and zones
    /// are <b>typed</b>: two departments working one floor hold two typed zones
    /// rather than sharing one.
    /// </para>
    /// </remarks>
    public Guid? ZoneId { get; set; }

    /// <summary>Who they answer to — a staff id, not a user id.</summary>
    /// <remarks>
    /// A reporting line is about people. Most staff have no login, so keying it
    /// to an account would leave the majority of the organogram unrepresentable.
    /// </remarks>
    public Guid? ReportingManagerStaffId { get; set; }

    /// <summary>The first day this posting is in force.</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>The last day, or null while the posting is open.</summary>
    /// <remarks>
    /// <b>Ending a posting is not deleting it.</b> A rota worked last March was
    /// worked under the posting in force then, and removing the row to revoke
    /// access would take the history with it. The row survives and the
    /// announcement is what withdraws the authorization.
    /// </remarks>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>When the posting was recorded.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it was last amended.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency, and the event's version.</summary>
    /// <remarks>
    /// Two supervisors editing one posting is ordinary rather than exceptional,
    /// and the loser is told instead of silently overwritten. The same counter
    /// is what an appended event carries as its <c>entity_version</c>, so two
    /// events about one aggregate can never collide on
    /// <c>uq_events__aggregate_version</c>.
    /// </remarks>
    public long Version { get; set; }

    /// <summary>Is this posting in force on <paramref name="on"/>?</summary>
    /// <remarks>
    /// A method rather than a stored <c>is_active</c> column: the answer depends
    /// on the clock, and a stored value that depends on the clock is wrong every
    /// day at midnight until something rewrites it. The same reason the platform
    /// derives the current business date instead of storing it, and the reason
    /// there is no <c>is_current_mod</c> flag on the duty register.
    /// </remarks>
    public bool IsInForceOn(DateOnly on) =>
        on >= EffectiveFrom && (EffectiveTo is null || on <= EffectiveTo);
}
