using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Summaries;

/// <summary>A day when two or more of one department are away.</summary>
/// <param name="DepartmentCode">Which department.</param>
/// <param name="On">Which day.</param>
/// <param name="Away">How many of them are away.</param>
/// <param name="Posted">How many are posted there at all.</param>
/// <remarks>
/// <b>Both numbers, never a ratio.</b> Two away is different in a department of
/// three and in one of thirty, and a percentage computed here would decide for
/// the reader which of those they were looking at.
/// </remarks>
public sealed record LeaveOverlap(string DepartmentCode, DateOnly On, int Away, int Posted);

/// <summary>A certification about to lapse.</summary>
/// <param name="Capability">What it certifies — the hotel's own word for it.</param>
/// <param name="Person">Whose.</param>
/// <param name="LapsesOn">When.</param>
/// <param name="InDays">How many days from today.</param>
public sealed record ExpiringCertification(
    string Capability, NamedPerson Person, DateOnly LapsesOn, int InDays);

/// <summary>The week ahead's measurable risks — Coming Up's answer.</summary>
/// <param name="OverlappingLeave">Days where a department loses two or more.</param>
/// <param name="CertsExpiring">Certifications lapsing inside the window.</param>
/// <param name="Overlaps">Those days, soonest first.</param>
/// <param name="Expiring">Those certifications, soonest first.</param>
/// <remarks>
/// <b>Two rows the approved catalogue asked for are not here, and never were
/// computed.</b> <i>Unfilled</i> and <i>thin</i> need a staffing demand model —
/// how many people a department needs on a Thursday — and Workforce has none.
/// A rota with four people on it is not thin or full; it is four people. The
/// honesty rule makes that absent rather than approximate, and the widget says
/// so on its own face. They return when a demand model does, and this record is
/// where they go.
/// </remarks>
public sealed record ComingUpView(
    int OverlappingLeave,
    int CertsExpiring,
    IReadOnlyList<LeaveOverlap> Overlaps,
    IReadOnlyList<ExpiringCertification> Expiring);

/// <summary>
/// The next seven days, for the risks this application can measure.
/// </summary>
/// <remarks>
/// <para>
/// <b>The department comes from the posting</b>, as it does everywhere here: a
/// leave request carries a person, and where that person works is a fact about
/// their posting which can change while the request waits.
/// </para>
/// <para>
/// <b>Requested leave counts as away.</b> A day where three people have <i>asked</i>
/// to be off is exactly the day a manager wants to see before approving the
/// third — counting only what is already approved would surface the problem
/// after it had been created. Both states are what
/// <see cref="Assignment.AssignmentAdvisor"/> already treats as covering a day.
/// </para>
/// </remarks>
public class ComingUpSummary(
    WorkforceDbContext db,
    CapabilityService capabilities,
    IKernelAuthorizer authorizer,
    IStaffDirectory directory,
    TimeProvider clock)
{
    /// <summary>How far ahead this looks.</summary>
    /// <remarks>
    /// Seven days, which is the frame's own window and the horizon a rota is
    /// planned over. The certification query's window is the ruling's outermost
    /// sixty; the seven is applied to its result rather than asked for
    /// separately, so one query serves both surfaces.
    /// </remarks>
    private const int Days = 7;

    /// <summary>What the week ahead holds.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The week.</returns>
    public async Task<ComingUpView> ReadAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var horizon = today.AddDays(Days);

        var overlaps = await OverlapsAsync(scope, today, horizon, cancellationToken);
        var expiring = await ExpiringAsync(scope, today, horizon, cancellationToken);

        return new ComingUpView(overlaps.Count, expiring.Count, overlaps, expiring);
    }

    /// <summary>Days where one department loses two or more people.</summary>
    private async Task<IReadOnlyList<LeaveOverlap>> OverlapsAsync(
        RequestScope scope, DateOnly today, DateOnly horizon, CancellationToken cancellationToken)
    {
        var away = await db.LeaveRequests
            .Where(r => r.PropertyId == scope.PropertyId
                        && r.From <= horizon
                        && r.To >= today
                        && (r.State == LeaveRequestState.Approved
                            || r.State == LeaveRequestState.Requested))
            .ToListAsync(cancellationToken);

        if (away.Count == 0)
        {
            return [];
        }

        var postings = await db.Postings
            .Where(p => p.PropertyId == scope.PropertyId
                        && p.EffectiveFrom <= horizon
                        && (p.EffectiveTo == null || p.EffectiveTo >= today))
            .ToListAsync(cancellationToken);

        var department = postings
            .GroupBy(posting => posting.StaffId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(posting => posting.IsPrimary).First()
                    .DepartmentCode);

        var size = postings
            .GroupBy(posting => posting.DepartmentCode)
            .ToDictionary(group => group.Key, group => group.Select(p => p.StaffId).Distinct().Count());

        // One entry per person per day they are away, then grouped. A day with
        // two people away is two entries; the count is of distinct people, so a
        // second request from one person cannot make a department look emptier
        // than it is.
        var byDay = new Dictionary<(string Department, DateOnly On), HashSet<Guid>>();

        foreach (var request in away)
        {
            if (!department.TryGetValue(request.StaffId, out var code))
            {
                continue;
            }

            for (var day = Later(request.From, today); day <= Earlier(request.To, horizon);
                 day = day.AddDays(1))
            {
                var key = (code, day);
                if (!byDay.TryGetValue(key, out var people))
                {
                    people = [];
                    byDay[key] = people;
                }

                people.Add(request.StaffId);
            }
        }

        return
        [
            .. byDay
                .Where(entry => entry.Value.Count >= 2)
                .Select(entry => new LeaveOverlap(
                    entry.Key.Department,
                    entry.Key.On,
                    entry.Value.Count,
                    size.GetValueOrDefault(entry.Key.Department)))
                .OrderBy(overlap => overlap.On)
                .ThenByDescending(overlap => overlap.Away)
                .ThenBy(overlap => overlap.DepartmentCode, StringComparer.Ordinal),
        ];
    }

    /// <summary>Certifications lapsing inside the window.</summary>
    private async Task<IReadOnlyList<ExpiringCertification>> ExpiringAsync(
        RequestScope scope, DateOnly today, DateOnly horizon, CancellationToken cancellationToken)
    {
        var attention = await capabilities.AttentionAsync(
            scope, new AttentionQuery(), cancellationToken);

        var lapsing = attention
            .Where(capability => capability.ValidUntil is { } until
                                 && until >= today && until <= horizon)
            .OrderBy(capability => capability.ValidUntil)
            .ToList();

        var names = await directory.FindNamesAsync(
            scope.PropertyId,
            [.. lapsing.Select(capability => capability.StaffId).Distinct()],
            cancellationToken);

        return
        [
            .. lapsing.Select(capability => new ExpiringCertification(
                capability.Name,
                new NamedPerson(capability.StaffId, names.GetValueOrDefault(capability.StaffId)),
                capability.ValidUntil!.Value,
                capability.ValidUntil!.Value.DayNumber - today.DayNumber)),
        ];
    }

    private static DateOnly Later(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Earlier(DateOnly a, DateOnly b) => a < b ? a : b;
}
