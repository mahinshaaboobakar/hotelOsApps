using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Summaries;

/// <summary>One department's people away over a stretch of days.</summary>
/// <param name="DepartmentCode">Which department.</param>
/// <param name="People">Who — named at answer time.</param>
/// <param name="From">The first day any of them is away.</param>
/// <param name="To">The last.</param>
/// <remarks>
/// <b>The people, not just the count.</b> A count answers <i>how short are we</i>
/// and the names answer <i>who do I ask</i>, and the second is what somebody
/// opens this for. The count is <c>People.Count</c> and is not carried
/// separately: two numbers that must agree are one number and a defect waiting.
/// </remarks>
public sealed record DepartmentAway(
    string DepartmentCode, IReadOnlyList<NamedPerson> People, DateOnly From, DateOnly To);

/// <summary>Who is away — On Leave's answer.</summary>
/// <param name="AwayToday">People away today.</param>
/// <param name="AwayThisWeek">People away on any day of the week ahead.</param>
/// <param name="Today">Away today, by department.</param>
/// <param name="RestOfWeek">Away later in the week, by department.</param>
/// <remarks>
/// <b>A person away for three days is one person in <see cref="AwayThisWeek"/>.</b>
/// Counting absences rather than people would make a week's holiday read as five
/// people missing, which is the number a manager would act on.
/// </remarks>
public sealed record OnLeaveView(
    int AwayToday,
    int AwayThisWeek,
    IReadOnlyList<DepartmentAway> Today,
    IReadOnlyList<DepartmentAway> RestOfWeek);

/// <summary>
/// Who is away today, and for the rest of the week — the read behind On Leave.
/// </summary>
/// <remarks>
/// <para>
/// <b>Approved leave only.</b> Coming Up counts requested leave too, because its
/// question is <i>what should I know before I approve the next one</i>. This
/// question is <i>who is not here</i>, and somebody whose request is still
/// waiting is at work.
/// </para>
/// <para>
/// <b>The department is the posting's.</b> A leave request carries a person and
/// no department, and rightly — where somebody works can change while a request
/// waits, and reading it off the request would file them under a department they
/// had already left.
/// </para>
/// </remarks>
public class OnLeaveSummary(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    IStaffDirectory directory,
    TimeProvider clock)
{
    /// <summary>The week this looks over, today included.</summary>
    private const int Days = 7;

    /// <summary>Who is away, today and after it.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The week's absences.</returns>
    public async Task<OnLeaveView> ReadAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var horizon = today.AddDays(Days - 1);

        var away = await db.LeaveRequests
            .Where(r => r.PropertyId == scope.PropertyId
                        && r.From <= horizon
                        && r.To >= today
                        && r.State == LeaveRequestState.Approved)
            .ToListAsync(cancellationToken);

        if (away.Count == 0)
        {
            return new OnLeaveView(0, 0, [], []);
        }

        var department = await DepartmentsAsync(scope, away, today, horizon, cancellationToken);

        var names = await directory.FindNamesAsync(
            scope.PropertyId,
            [.. away.Select(request => request.StaffId).Distinct()],
            cancellationToken);

        var placed = away
            .Where(request => department.ContainsKey(request.StaffId))
            .ToList();

        var todayAway = placed.Where(request => request.From <= today && request.To >= today);
        var laterAway = placed.Where(request => request.From > today);

        return new OnLeaveView(
            AwayToday: todayAway.Select(request => request.StaffId).Distinct().Count(),
            AwayThisWeek: placed.Select(request => request.StaffId).Distinct().Count(),
            Today: Group(todayAway, department, names),
            RestOfWeek: Group(laterAway, department, names));
    }

    /// <summary>One row per department, with its people and the days they span.</summary>
    private static IReadOnlyList<DepartmentAway> Group(
        IEnumerable<LeaveRequest> requests,
        IReadOnlyDictionary<Guid, string> department,
        IReadOnlyDictionary<Guid, string> names) =>
    [
        .. requests
            .GroupBy(request => department[request.StaffId])
            .Select(group => new DepartmentAway(
                group.Key,
                [
                    .. group
                        .Select(request => request.StaffId)
                        .Distinct()
                        .Select(staffId => new NamedPerson(
                            staffId, names.GetValueOrDefault(staffId))),
                ],
                group.Min(request => request.From),
                group.Max(request => request.To)))
            .OrderByDescending(row => row.People.Count)
            .ThenBy(row => row.DepartmentCode, StringComparer.Ordinal),
    ];

    /// <summary>Where each of these people is posted, primary posting first.</summary>
    /// <remarks>
    /// Somebody with no posting in force is <b>left out</b> rather than filed
    /// under a blank department: this application knows they are away and does
    /// not know where they work, and an empty group heading would claim a
    /// department exists that does not.
    /// </remarks>
    private async Task<Dictionary<Guid, string>> DepartmentsAsync(
        RequestScope scope,
        IReadOnlyList<LeaveRequest> away,
        DateOnly today,
        DateOnly horizon,
        CancellationToken cancellationToken)
    {
        var people = away.Select(request => request.StaffId).Distinct().ToList();

        var postings = await db.Postings
            .Where(p => p.PropertyId == scope.PropertyId
                        && people.Contains(p.StaffId)
                        && p.EffectiveFrom <= horizon
                        && (p.EffectiveTo == null || p.EffectiveTo >= today))
            .ToListAsync(cancellationToken);

        return postings
            .GroupBy(posting => posting.StaffId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(posting => posting.IsPrimary).First()
                    .DepartmentCode);
    }
}
