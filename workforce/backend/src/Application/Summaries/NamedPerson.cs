namespace HotelOS.Workforce.Application.Summaries;

/// <summary>A person, as a read view names them.</summary>
/// <param name="StaffId">Master Data's id — this application's only handle on them.</param>
/// <param name="Name">
/// The display name, read at answer time, or <c>null</c> when Master Data did
/// not answer for this id.
/// </param>
/// <remarks>
/// <para>
/// <b>Nullable, and never filled in.</b> "Master Data has no name for this id"
/// and "this person is called Unknown" are different facts, and a placeholder
/// invented here would be this application deciding what somebody is called —
/// the exact thing the no-second-place-for-a-name rule exists to prevent. The
/// caller renders the absence however its surface should.
/// </para>
/// <para>
/// <b>The id is always present; the name is the borrowed half.</b> That
/// asymmetry is the ruling — <i>serving is not storing</i> — expressed in a
/// type: the id is Workforce's own and persists, the name is read for one
/// answer and goes with it.
/// </para>
/// </remarks>
public sealed record NamedPerson(Guid StaffId, string? Name);
