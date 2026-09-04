using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Application.Postings;

/// <summary>Create a posting.</summary>
/// <remarks>
/// A command rather than the wire message, so the service is testable without a
/// gRPC surface and so the shape it validates is its own — the same split every
/// platform service uses.
/// </remarks>
public sealed record CreatePostingCommand
{
    /// <summary>Master Data's person. This application never creates one.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>The department's canon code — ADR 0119.</summary>
    public required string DepartmentCode { get; init; }

    /// <summary>What they do there — the hotel's own vocabulary, not a canon.</summary>
    public required string JobRole { get; init; }

    /// <summary>The posting that answers "where does this person work?" — <c>WF-Q3</c>.</summary>
    public bool IsPrimary { get; init; }

    /// <summary>This posting makes them the department's head — ADR 0063's table.</summary>
    public bool IsDepartmentHead { get; init; }

    /// <summary>Optional — most postings have none.</summary>
    public Guid? ZoneId { get; init; }

    /// <summary>Who they answer to — a staff id, because most staff have no login.</summary>
    public Guid? ReportingManagerStaffId { get; init; }

    /// <summary>
    /// The first day the posting is in force. Required, and deliberately not
    /// defaulted to today.
    /// </summary>
    /// <remarks>
    /// Defaulting would silently backdate an arrangement somebody meant to begin
    /// next month, and a backdated posting is a backdated authorization.
    /// </remarks>
    public required DateOnly EffectiveFrom { get; init; }
}

/// <summary>Amend a posting, without moving it.</summary>
/// <remarks>
/// <para>
/// <b>Neither the person nor the department can be patched</b>, and their
/// absence is the enforcement: a client cannot express the mistake. Moving a
/// posting to a different person or department is ending one and creating
/// another, because <c>department#posted</c> was announced for the old pair —
/// an update that quietly re-pointed it would leave a tuple granting access to a
/// department the person no longer works in, which is exactly the direction
/// ADR 0061's invariant forbids.
/// </para>
/// <para>
/// <c>null</c> means "leave it alone"; a present value means "set it to this",
/// including back to none for the two nullable fields.
/// </para>
/// </remarks>
public sealed record UpdatePostingCommand
{
    /// <summary>The posting to amend.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read. A mismatch is refused, not merged.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>New job role, or null to leave it.</summary>
    public string? JobRole { get; init; }

    /// <summary>New primary flag, or null to leave it.</summary>
    public bool? IsPrimary { get; init; }

    /// <summary>New headship flag, or null to leave it.</summary>
    public bool? IsDepartmentHead { get; init; }

    /// <summary>Present and empty clears the zone; absent leaves it.</summary>
    public Optional<Guid?> ZoneId { get; init; }

    /// <summary>Present and empty clears the reporting line; absent leaves it.</summary>
    public Optional<Guid?> ReportingManagerStaffId { get; init; }
}

/// <summary>Close a posting's window.</summary>
public sealed record EndPostingCommand
{
    /// <summary>The posting to close.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>The last day the posting is in force.</summary>
    public required DateOnly EffectiveTo { get; init; }
}

/// <summary>Which page of a list, and how big.</summary>
/// <param name="Page">0-based; 0 is the first page.</param>
/// <param name="Size">
/// A request rather than an instruction — the service clamps it, and echoes
/// back the size it actually applied.
/// </param>
public readonly record struct PagedQuery(int Page, int Size);

/// <summary>One page of postings, and what the pager needs to draw itself.</summary>
/// <param name="Postings">The rows on this page.</param>
/// <param name="Page">The page served, 0-based.</param>
/// <param name="Size">The size APPLIED, after the clamp.</param>
/// <param name="Total">Rows matching the query — not rows on this page.</param>
public sealed record PostingPage(
    IReadOnlyList<Posting> Postings, int Page, int Size, int Total);

/// <summary>Which postings to list.</summary>
public sealed record ListPostingsQuery
{
    /// <summary>Only this person's postings.</summary>
    public Guid? StaffId { get; init; }

    /// <summary>Only this canon department.</summary>
    public string? DepartmentCode { get; init; }

    /// <summary>Only postings covering this zone — the Context resolver's query.</summary>
    public Guid? ZoneId { get; init; }

    /// <summary>Which page, 0-based, and how big.</summary>
    /// <remarks>
    /// Null asks for the first page at the default size — <c>CORE-Q13</c>'s
    /// paged pattern, which applies here because the count is a fact: this list
    /// is the property's headcount, and every other read in Workforce is
    /// bounded by a day, a week, a month or a department.
    /// </remarks>
    public PagedQuery? Paging { get; init; }

    /// <summary>
    /// Include postings whose window has closed. Default false: the ordinary
    /// question is "who works here now".
    /// </summary>
    public bool IncludeEnded { get; init; }
}

/// <summary>
/// A value that may be absent, distinctly from being present and null.
/// </summary>
/// <remarks>
/// A nullable field cannot express the difference between <i>"leave the zone
/// alone"</i> and <i>"remove the zone"</i>, and both are things a supervisor
/// does. Rather than a second boolean per field — which can disagree with the
/// value beside it — the distinction is in the type, so the ambiguous state is
/// inexpressible.
/// </remarks>
public readonly record struct Optional<T>
{
    private Optional(T value, bool present)
    {
        Value = value;
        IsPresent = present;
    }

    /// <summary>The value, meaningful only when <see cref="IsPresent"/>.</summary>
    public T? Value { get; }

    /// <summary>Whether the caller said anything about this field at all.</summary>
    public bool IsPresent { get; }

    /// <summary>The caller said nothing — leave the field as it is.</summary>
    public static Optional<T> Absent => default;

    /// <summary>The caller asked for this value, which may itself be null.</summary>
    public static Optional<T> Of(T value) => new(value, true);
}
