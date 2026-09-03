using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Swaps;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Summaries;

/// <summary>What kind of thing is waiting.</summary>
public enum PendingKind
{
    /// <summary>Two people agreeing to exchange shifts.</summary>
    Swap,

    /// <summary>Somebody asking to be away.</summary>
    Leave,
}

/// <summary>One thing this property is waiting on.</summary>
/// <param name="Kind">Swap or leave.</param>
/// <param name="Raiser">Who raised it.</param>
/// <param name="Colleague">The other side of a swap; <c>null</c> for leave.</param>
/// <param name="DepartmentCode">
/// Where the raiser is posted, or empty when they hold no posting in force.
/// </param>
/// <param name="WaitingDays">
/// How long it has been waiting, in whole days. Derived from a clock, and
/// therefore computed here — a screen that worked it out would be a second
/// implementation of a value that depends on when you ask.
/// </param>
public sealed record PendingRequest(
    PendingKind Kind,
    NamedPerson Raiser,
    NamedPerson? Colleague,
    string DepartmentCode,
    int WaitingDays);

/// <summary>What is waiting on a decision — Pending Requests' answer.</summary>
/// <param name="Swaps">Swaps waiting, property-wide.</param>
/// <param name="Leave">Leave requests waiting, property-wide.</param>
/// <param name="Rows">Both kinds together, longest-waiting first.</param>
public sealed record PendingRequestsView(
    int Swaps, int Leave, IReadOnlyList<PendingRequest> Rows);

/// <summary>
/// Swaps and leave waiting on somebody — the read behind Pending Requests.
/// </summary>
/// <remarks>
/// <para>
/// <b>Property-scoped, which is the question that had no answer.</b> Both owning
/// services could say what was waiting on <i>one approver</i>; neither could say
/// what the property was waiting on. Each has gained a <c>PendingAsync</c>, and
/// this composes the two rather than querying their tables — the states that
/// count as waiting are each service's own business, and a copy of that list
/// here would be the copy that drifts.
/// </para>
/// <para>
/// <b>Age is time waiting.</b> Both queues order by when the request was raised
/// rather than by the day it concerns, and the merged list orders the same way,
/// because the number beside a row is about a person who has heard nothing.
/// </para>
/// <para>
/// <b>The department comes from the posting.</b> Neither a swap nor a leave
/// request carries one, and rightly: a person's department is a property of
/// where they are posted and can change while a request waits. Reading it off
/// the request would file somebody under a department they had already left.
/// </para>
/// </remarks>
public class PendingRequestsSummary(
    WorkforceDbContext db,
    SwapProposalService swaps,
    LeaveService leave,
    IStaffDirectory directory,
    TimeProvider clock)
{
    /// <summary>Everything waiting, longest first.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The queue.</returns>
    public async Task<PendingRequestsView> ReadAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        // Both authorize with `roster.read`; neither is asked twice, and this
        // adds no third check of its own.
        var pendingSwaps = await swaps.PendingAsync(scope, cancellationToken);
        var pendingLeave = await leave.PendingAsync(scope, cancellationToken);

        var people = pendingSwaps.Select(s => s.ProposerStaffId)
            .Concat(pendingSwaps.Select(s => s.ColleagueStaffId))
            .Concat(pendingLeave.Select(r => r.StaffId))
            .Distinct()
            .ToList();

        var names = await directory.FindNamesAsync(scope.PropertyId, people, cancellationToken);
        var departments = await DepartmentsAsync(scope, people, cancellationToken);

        var now = clock.GetUtcNow();

        var rows = pendingSwaps
            .Select(proposal => (
                Raised: proposal.CreatedAt,
                Row: new PendingRequest(
                    PendingKind.Swap,
                    Person(proposal.ProposerStaffId, names),
                    Person(proposal.ColleagueStaffId, names),
                    departments.GetValueOrDefault(proposal.ProposerStaffId, string.Empty),
                    Waiting(proposal.CreatedAt, now))))
            .Concat(pendingLeave.Select(request => (
                Raised: request.CreatedAt,
                Row: new PendingRequest(
                    PendingKind.Leave,
                    Person(request.StaffId, names),
                    null,
                    departments.GetValueOrDefault(request.StaffId, string.Empty),
                    Waiting(request.CreatedAt, now)))))
            .OrderBy(entry => entry.Raised)
            .Select(entry => entry.Row)
            .ToList();

        return new PendingRequestsView(pendingSwaps.Count, pendingLeave.Count, rows);
    }

    /// <summary>Where each person is posted, by their primary posting in force.</summary>
    /// <remarks>
    /// The primary one when somebody holds two — <c>WF-Q3</c> allows a second
    /// posting, and a request is filed under the department a person is mainly
    /// in rather than under whichever row came back first.
    /// </remarks>
    private async Task<Dictionary<Guid, string>> DepartmentsAsync(
        RequestScope scope, IReadOnlyList<Guid> people, CancellationToken cancellationToken)
    {
        if (people.Count == 0)
        {
            return [];
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var postings = await db.Postings
            .Where(p => p.PropertyId == scope.PropertyId
                        && people.Contains(p.StaffId)
                        && p.EffectiveFrom <= today
                        && (p.EffectiveTo == null || p.EffectiveTo >= today))
            .ToListAsync(cancellationToken);

        return postings
            .GroupBy(posting => posting.StaffId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(posting => posting.IsPrimary).First()
                    .DepartmentCode);
    }

    /// <summary>Whole days between raising and now, never negative.</summary>
    /// <remarks>
    /// A clock skew that put a request in the future would otherwise show a
    /// negative age, which reads as a defect rather than as "just now".
    /// </remarks>
    private static int Waiting(DateTimeOffset raised, DateTimeOffset now) =>
        Math.Max(0, (int)(now - raised).TotalDays);

    private static NamedPerson Person(Guid staffId, IReadOnlyDictionary<Guid, string> names) =>
        new(staffId, names.GetValueOrDefault(staffId));
}
