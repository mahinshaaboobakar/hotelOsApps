using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Leave;

/// <summary>
/// Leave: raised, warned, decided — and a balance that is a ledger.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here blocks on a balance.</b> <c>WF-Q5</c> is warn-and-allow —
/// <i>"hotels override reality daily"</i> — so an exhausted balance is reported
/// with its number and the decision stays the approver's. A negative balance is
/// a real state, not an error: it is an approved overdraw, and every screen that
/// shows a balance must survive a minus sign.
/// </para>
/// <para>
/// <b>The approver is resolved from this application's own postings</b> — the
/// reporting manager when the posting names one, the department head otherwise.
/// That is why Workforce can answer <i>"whose request is this"</i> at all:
/// ADR 0116 §6 makes department membership derive from postings, permanently.
/// </para>
/// </remarks>
public class LeaveService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    ApproverResolver approvers,
    TimeProvider clock)
{
    /// <summary>Raise a request, for oneself or on somebody's behalf.</summary>
    public async Task<LeaveRequest> RaiseAsync(
        RequestScope scope, RaiseLeaveCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.LeaveRequest, "property", scope.PropertyId, cancellationToken);

        if (command.To < command.From)
        {
            throw new InvalidRequestException("leave ends on or after the day it starts");
        }

        await RequireOfferedAsync(scope.PropertyId, command.LeaveTypeId, cancellationToken);
        await RefuseOverlapAsync(scope, command, cancellationToken);

        var now = clock.GetUtcNow();
        var request = new LeaveRequest
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            StaffId = command.StaffId,
            LeaveTypeId = command.LeaveTypeId,
            From = command.From,
            To = command.To,
            Note = command.Note?.Trim() ?? string.Empty,
            State = LeaveRequestState.Requested,

            // Provenance, always — WF-Q9(b) at its third surface. Whether this
            // was self-raised or raised on somebody's behalf is the difference
            // between a record and a claim.
            EnteredByUserId = scope.UserId,

            // Resolved now rather than at decision time: a request that changed
            // hands because a posting moved while it waited is one nobody is
            // accountable for.
            ApproverStaffId = await approvers.ResolveAsync(
                scope.PropertyId,
                command.StaffId,
                DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime),
                cancellationToken),

            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);

        return request;
    }

    /// <summary>Grant it, and debit the balance.</summary>
    /// <remarks>
    /// <b>The debit happens here and not at request time.</b> Debiting on request
    /// would let an undecided request hide capacity from everybody else, and a
    /// declined one would then need an unwind that could be missed.
    /// </remarks>
    public async Task<LeaveRequest> ApproveAsync(
        RequestScope scope, DecideLeaveCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.LeaveApprove, "property", scope.PropertyId, cancellationToken);

        var request = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(request, command.ExpectedVersion);
        RequireState(request, LeaveRequestState.Requested);

        var now = clock.GetUtcNow();

        request.State = LeaveRequestState.Approved;
        request.DecisionNote = command.Note?.Trim() ?? string.Empty;
        request.DecidedAt = now;
        request.UpdatedAt = now;
        request.Version += 1;

        Post(request, -request.Days, LeaveLedgerKind.Approval, request.From, now);

        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <summary>Refuse it. The balance never moves.</summary>
    public async Task<LeaveRequest> DeclineAsync(
        RequestScope scope, DecideLeaveCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.LeaveApprove, "property", scope.PropertyId, cancellationToken);

        var request = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(request, command.ExpectedVersion);
        RequireState(request, LeaveRequestState.Requested);

        var now = clock.GetUtcNow();

        request.State = LeaveRequestState.Declined;
        request.DecisionNote = command.Note?.Trim() ?? string.Empty;
        request.DecidedAt = now;
        request.UpdatedAt = now;
        request.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <summary>Withdraw it, crediting the balance back if it had been granted.</summary>
    /// <remarks>
    /// The symmetry the ledger implies. A debit with no matching credit turns a
    /// cancellation into a silent forfeit, which is the kind of arithmetic
    /// somebody notices a year later and cannot reconstruct.
    /// </remarks>
    public async Task<LeaveRequest> CancelAsync(
        RequestScope scope, DecideLeaveCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.LeaveRequest, "property", scope.PropertyId, cancellationToken);

        var request = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(request, command.ExpectedVersion);

        if (request.State is LeaveRequestState.Declined or LeaveRequestState.Cancelled)
        {
            throw new InvalidRequestException($"this request is already {request.State}");
        }

        var now = clock.GetUtcNow();
        var wasApproved = request.State == LeaveRequestState.Approved;

        request.State = LeaveRequestState.Cancelled;
        request.UpdatedAt = now;
        request.Version += 1;

        if (wasApproved)
        {
            Post(request, request.Days, LeaveLedgerKind.Cancellation, request.From, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <summary>Put a balance where HR says it should be.</summary>
    /// <remarks>
    /// The manual floor. A note is required: an adjustment nobody explained is
    /// the one ledger entry that cannot be defended when somebody asks.
    /// </remarks>
    public async Task<LeaveLedgerEntry> AdjustAsync(
        RequestScope scope, AdjustBalanceCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.LeaveApprove, "property", scope.PropertyId, cancellationToken);

        var note = command.Note?.Trim() ?? string.Empty;

        if (note.Length == 0)
        {
            throw new InvalidRequestException("an adjustment says why");
        }

        if (command.Days == 0m)
        {
            throw new InvalidRequestException("an adjustment of nothing is not an adjustment");
        }

        await RequireOfferedAsync(scope.PropertyId, command.LeaveTypeId, cancellationToken);

        var now = clock.GetUtcNow();
        var entry = new LeaveLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            StaffId = command.StaffId,
            LeaveTypeId = command.LeaveTypeId,
            Days = command.Days,
            Kind = LeaveLedgerKind.Adjustment,
            OccurredOn = DateOnly.FromDateTime(now.UtcDateTime),
            RecordedByUserId = scope.UserId,
            Note = note,
            CreatedAt = now,
        };

        db.LeaveLedger.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return entry;
    }

    /// <summary>What somebody has, by type.</summary>
    /// <remarks>
    /// <b>Summed, never stored.</b> And it may be negative — an approved
    /// overdraw is a real state under <c>WF-Q5</c>, which is why this returns a
    /// number rather than something that clamps at zero.
    /// </remarks>
    public async Task<IReadOnlyDictionary<Guid, decimal>> BalancesAsync(
        RequestScope scope, Guid staffId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var sums = await db.LeaveLedger
            .Where(e => e.PropertyId == scope.PropertyId && e.StaffId == staffId)
            .GroupBy(e => e.LeaveTypeId)
            .Select(g => new { TypeId = g.Key, Days = g.Sum(e => e.Days) })
            .ToListAsync(cancellationToken);

        return sums.ToDictionary(s => s.TypeId, s => s.Days);
    }

    /// <summary>The requests waiting on one approver.</summary>
    public async Task<IReadOnlyList<LeaveRequest>> QueueAsync(
        RequestScope scope, Guid approverStaffId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        return await db.LeaveRequests
            .Where(r => r.PropertyId == scope.PropertyId
                        && r.ApproverStaffId == approverStaffId
                        && r.State == LeaveRequestState.Requested)
            .OrderBy(r => r.From)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Every request this property is waiting on, longest first.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Requests still in <see cref="LeaveRequestState.Requested"/>.</returns>
    /// <remarks>
    /// <para>
    /// <b>Property-scoped, where <see cref="QueueAsync"/> is per approver.</b>
    /// Two different questions: <i>what needs me</i>, and <i>what is this
    /// property waiting on</i>. The second had no answer until the widget round
    /// tried to draw it.
    /// </para>
    /// <para>
    /// Ordered by <see cref="LeaveRequest.CreatedAt"/> rather than by
    /// <see cref="LeaveRequest.From"/> — deliberately the opposite of
    /// <see cref="QueueAsync"/>, and for a reason rather than by accident. An
    /// approver's queue is worked in the order the days fall; a <i>waiting</i>
    /// figure is about the person who raised it and has heard nothing.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<LeaveRequest>> PendingAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        return await db.LeaveRequests
            .Where(r => r.PropertyId == scope.PropertyId
                        && r.State == LeaveRequestState.Requested)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private void Post(
        LeaveRequest request, decimal days, LeaveLedgerKind kind, DateOnly on, DateTimeOffset now) =>
        db.LeaveLedger.Add(new LeaveLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            PropertyId = request.PropertyId,
            StaffId = request.StaffId,
            LeaveTypeId = request.LeaveTypeId,
            Days = days,
            Kind = kind,
            OccurredOn = on,
            LeaveRequestId = request.Id,
            CreatedAt = now,
        });

    private async Task RefuseOverlapAsync(
        RequestScope scope, RaiseLeaveCommand command, CancellationToken cancellationToken)
    {
        // Two live requests covering one day would debit the balance twice for
        // one absence. A person cannot be away twice, so this is refused rather
        // than warned.
        var clashing = await db.LeaveRequests.AnyAsync(
            r => r.PropertyId == scope.PropertyId
                 && r.StaffId == command.StaffId
                 && (r.State == LeaveRequestState.Requested
                     || r.State == LeaveRequestState.Approved)
                 && r.From <= command.To
                 && command.From <= r.To,
            cancellationToken);

        if (clashing)
        {
            throw new InvalidRequestException(
                "this person already has leave requested or approved over those days");
        }
    }

    private async Task RequireOfferedAsync(
        Guid propertyId, Guid leaveTypeId, CancellationToken cancellationToken)
    {
        var offered = await db.LeaveTypes.AnyAsync(
            t => t.Id == leaveTypeId && t.PropertyId == propertyId && t.Active, cancellationToken);

        if (!offered)
        {
            throw new InvalidRequestException(
                "that leave type is not offered at this property, or has been retired");
        }
    }

    private async Task<LeaveRequest> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        var request = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.PropertyId == scope.PropertyId, cancellationToken);

        return request ?? throw new NotFoundException("leave request", id);
    }

    private static void RequireState(LeaveRequest request, LeaveRequestState expected)
    {
        if (request.State != expected)
        {
            throw new InvalidRequestException(
                $"this request is {request.State} and cannot be decided again");
        }
    }

    private static void RequireVersion(LeaveRequest request, long expected)
    {
        if (request.Version != expected)
        {
            throw new ConcurrencyException("leave request", request.Id, expected);
        }
    }
}
