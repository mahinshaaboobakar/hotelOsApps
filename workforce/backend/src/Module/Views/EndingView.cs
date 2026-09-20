using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// What ending a posting is about to do, before anybody presses the button.
/// </summary>
/// <remarks>
/// <para>
/// <b>The screen has been inventing this answer.</b> `end-posting.ts` drew a
/// consequence panel — <i>these memberships close with it</i> — and the module
/// built the panel itself: a recorded fixture for one person, and for everybody
/// else a department of <c>"Front Office"</c>, a last day of
/// <c>"Thu 4 Sep 2026"</c>, and <b>an empty list of consequences</b>. The panel
/// that exists to say what else closes asserted <i>nothing</i> for every person
/// in the property.
/// </para>
/// <para>
/// <b>The service could always answer it.</b> <see cref="TeamService"/>'s
/// <c>SupportedTeamsAsync</c> is the query, and <c>PeopleView.End</c>'s own
/// documentation already claimed the panel was that query's answer — a
/// guarantee written down for a mechanism nobody had built, which is the class
/// of comment this platform has a rule about. This is the door that makes the
/// sentence true.
/// </para>
/// <para>
/// <b>One query for the statement and the act.</b> The panel and
/// <c>EndMembershipsForPostingAsync</c> now resolve the same memberships the
/// same way, so what a person is shown and what is closed cannot disagree —
/// which was the whole argument for reading it rather than predicting it.
/// </para>
/// </remarks>
public static class EndingView
{
    /// <summary>What ending this posting closes, as the service knows it.</summary>
    public static async Task<object?> Read(ModuleCall call, CancellationToken cancellationToken)
    {
        var postings = call.Service<PostingService>();
        var teams = call.Service<TeamService>();
        var directory = call.Service<IStaffDirectory>();

        var id = call.Id("posting");

        var held = await postings.ListAsync(
            call.Scope, new ListPostingsQuery { IncludeEnded = true }, cancellationToken);

        var posting = held.FirstOrDefault(one => one.Id == id)
                      ?? throw new NotFoundException("posting", id);

        // The last day defaults to the property's today. A posting ended
        // "today" is worked today — the same convention the membership uses,
        // where a last day is a day worked.
        var lastDay = await PropertyDay.TodayAsync(call, cancellationToken);

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, [posting.StaffId], cancellationToken);

        var departments = await directory.FindDepartmentNamesAsync(
            call.Scope.PropertyId, cancellationToken);

        var supported = await teams.SupportedTeamsAsync(
            call.Scope, posting.StaffId, posting.DepartmentCode, lastDay, cancellationToken);

        return new
        {
            id = posting.Id,

            // Quoted back by the write — the same row the person is looking at.
            version = posting.Version,

            who = names.TryGetValue(posting.StaffId, out var name) ? name : null,

            department = departments.TryGetValue(posting.DepartmentCode, out var known)
                ? known
                : posting.DepartmentCode,

            lastDay = lastDay.ToString("yyyy-MM-dd"),

            // **What actually closes**, from the query the write uses. Empty is
            // a real answer here and means this posting holds nothing open; it
            // is no longer the answer everybody gets.
            alsoEnds = supported
                .Select(one => new
                {
                    team = one.Team.Name,
                    department = departments.TryGetValue(one.Team.DepartmentCode, out var of)
                        ? of
                        : one.Team.DepartmentCode,
                    since = one.Since.ToString("yyyy-MM-dd"),
                })
                .ToArray(),
        };
    }
}
