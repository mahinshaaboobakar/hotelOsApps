using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Leave;

/// <summary>
/// Every movement of every balance, and the balance itself is their sum.
/// </summary>
/// <remarks>
/// A subject partial of <see cref="LeaveService"/>, whose primary part carries
/// this service's contract. **One class, two files, on purpose**: these members
/// and the primary part's write to the SAME scoped <c>WorkforceDbContext</c> in
/// ONE <c>SaveChangesAsync</c>, so a second service here would put a
/// cross-service write inside that transaction. The subject boundary is real;
/// the transaction is what decides it is taken as files rather than as services.
/// </remarks>
public partial class LeaveService
{
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

            // The day the correction happened AT THE PROPERTY — ADR 0211. A
            // ledger row is read back as "what happened on the 3rd", and the
            // UTC day put an evening correction in Guatemala on the next one.
            OccurredOn = OperatingDay.OrUnavailable(
                await days.TodayAsync(scope, cancellationToken)),
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

    /// <summary>What several people have, by person and type — one query.</summary>
    /// <remarks>
    /// <para>
    /// For the approver's queue, which shows the balance a decision would leave
    /// behind (owner, 2026-09-20, `64g` §4 B). Asking <see cref="BalancesAsync"/>
    /// per row would be a round trip and an authorization per person, for one
    /// screen that already knows every person it is about — CLAUDE.md's
    /// per-round-trip review of a hot path.
    /// </para>
    /// <para>
    /// Summed here as they are there, and a person with no ledger row is absent
    /// from the result rather than present with a zero: nothing has been posted
    /// for them, and a zero is a claim that something was.
    /// </para>
    /// </remarks>
    /// <param name="scope">Who is asking, and where.</param>
    /// <param name="staffIds">The people.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The balance for each person and type that has one.</returns>
    public async Task<IReadOnlyDictionary<(Guid StaffId, Guid TypeId), decimal>> BalancesForAsync(
        RequestScope scope,
        IReadOnlyCollection<Guid> staffIds,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        if (staffIds.Count == 0)
        {
            return new Dictionary<(Guid, Guid), decimal>();
        }

        var sums = await db.LeaveLedger
            .Where(e => e.PropertyId == scope.PropertyId && staffIds.Contains(e.StaffId))
            .GroupBy(e => new { e.StaffId, e.LeaveTypeId })
            .Select(g => new { g.Key.StaffId, g.Key.LeaveTypeId, Days = g.Sum(e => e.Days) })
            .ToListAsync(cancellationToken);

        return sums.ToDictionary(s => (s.StaffId, s.LeaveTypeId), s => s.Days);
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
}
