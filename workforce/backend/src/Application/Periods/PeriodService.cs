using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Periods;

/// <summary>
/// The month-end numbers — the rota, attendance and the leave ledger, read
/// together.
/// </summary>
/// <remarks>
/// <para>
/// <b>A period is the sum of its days</b>, and the days already exist:
/// <see cref="DayComparison"/> produces one row per person per day with planned
/// against actual, and this aggregates them. Recomputing lateness or worked hours
/// here would be a second implementation of arithmetic that is already right —
/// and the two would drift in the direction nobody checks, because a monthly
/// total and a daily row are rarely read side by side.
/// </para>
/// <para>
/// <b>Nothing is stored.</b> Every input can be corrected after the fact — a
/// mispunched clock-out is the ordinary case — so a stored total would be right
/// until somebody fixed one.
/// </para>
/// <para>
/// <b>The period boundaries are the caller's.</b> This application does not know
/// when a hotel payroll month begins: the fiscal year is Core Administration's
/// (ADR 0052 keeps it on <c>Property</c>), and inventing a month here would be an
/// installable application deciding a property's calendar.
/// </para>
/// </remarks>
public class PeriodService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    DayComparison days)
{
    /// <summary>The figures for everybody in a window.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="query">Which days, and whose.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>One period per person the window knows anything about.</returns>
    public async Task<IReadOnlyList<WorkforcePeriod>> ComputeAsync(
        RequestScope scope, AttendanceQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        // The composed call checks the same permission on the same object. Two
        // checks rather than one, and deliberately not worked around: the entry
        // point authorizes because it is the entry point, and DayComparison
        // authorizes because it is reachable on its own. It is two calls for the
        // whole report, not two per person.
        var rows = await days.CompareAsync(scope, query, cancellationToken);

        var policy = await db.Policies.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId, cancellationToken);

        var leave = await LeaveByPersonAsync(scope, query, cancellationToken);

        var periods = new List<WorkforcePeriod>();
        var people = rows.Select(r => r.StaffId).Concat(leave.Keys).Distinct();

        foreach (var staffId in people)
        {
            var theirs = rows.Where(r => r.StaffId == staffId).ToList();

            periods.Add(new WorkforcePeriod(
                StaffId: staffId,
                From: query.From,
                To: query.To,

                // Rostered **to work**. A week-off cell is a rota marker, not a
                // day posted — WF-Q12 — so counting it would tell payroll
                // somebody was scheduled on their day off.
                DaysPosted: theirs.Count(r => r.Rostered && r.PlannedHours > 0m),
                DaysPresent: theirs.Count(r => r.Attended),
                DaysAbsent: theirs.Count(r => r.Absent),
                LateCount: theirs.Count(r => r.LateBy is not null),
                HoursWorked: theirs.Sum(r => r.Worked ?? 0m),
                OvertimeHours: Overtime(theirs, policy),
                LeaveTakenByType: leave.GetValueOrDefault(staffId)
                                  ?? new Dictionary<Guid, decimal>())
            {
                UnplannedDays = theirs.Count(r => r.Unplanned),
            });
        }

        return periods;
    }

    /// <summary>Hours beyond the property daily threshold, day by day.</summary>
    /// <remarks>
    /// <para>
    /// <b>Daily only, and the reason is that a month has no weeks.</b> The policy
    /// carries a weekly threshold too, and the planning warning applies it —
    /// because there the caller window <i>is</i> a week. Over a month it is not
    /// computable: it needs a week-start, which is a property setting nothing
    /// establishes, and picking Monday would be this application deciding a
    /// hotel's calendar.
    /// </para>
    /// <para>
    /// <b>No threshold means no overtime, not zero overtime.</b> A property that
    /// has never opened the policy screen has not agreed to a labour rule, and
    /// reporting a figure computed against one this application invented is worse
    /// than reporting none.
    /// </para>
    /// </remarks>
    private static decimal Overtime(IEnumerable<DayRow> rows, WorkforcePolicy? policy) =>
        policy?.OvertimeDailyHours is { } daily
            ? rows.Sum(r => Math.Max(0m, (r.Worked ?? 0m) - daily))
            : 0m;

    /// <summary>Approved leave days falling inside the window, by person and type.</summary>
    /// <remarks>
    /// <b>Clipped to the window, not counted whole.</b> Ten days spanning a month
    /// end are not ten days in either month, and a request is the wrong unit for a
    /// period figure — which is why this counts days rather than reading the
    /// ledger's single debit, whose <c>occurred_on</c> is the request's first day.
    /// </remarks>
    private async Task<Dictionary<Guid, Dictionary<Guid, decimal>>> LeaveByPersonAsync(
        RequestScope scope, AttendanceQuery query, CancellationToken cancellationToken)
    {
        var requests = db.LeaveRequests.Where(
            r => r.PropertyId == scope.PropertyId
                 && r.State == LeaveRequestState.Approved
                 && r.From <= query.To
                 && query.From <= r.To);

        if (query.StaffId is { } staffId)
        {
            requests = requests.Where(r => r.StaffId == staffId);
        }

        var taken = new Dictionary<Guid, Dictionary<Guid, decimal>>();

        foreach (var request in await requests.ToListAsync(cancellationToken))
        {
            var first = request.From > query.From ? request.From : query.From;
            var last = request.To < query.To ? request.To : query.To;
            var days = last.DayNumber - first.DayNumber + 1;

            if (days <= 0)
            {
                continue;
            }

            if (!taken.TryGetValue(request.StaffId, out var byType))
            {
                byType = [];
                taken[request.StaffId] = byType;
            }

            byType[request.LeaveTypeId] = byType.GetValueOrDefault(request.LeaveTypeId) + days;
        }

        return taken;
    }
}
