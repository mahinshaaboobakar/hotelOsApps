using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Swaps;

/// <summary>
/// Staff swap proposals: proposed → accepted → approved, and the rota changes
/// once.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q9</c>: staff propose, the colleague accepts, the manager approves. The
/// acceptance is its own state because <b>a manager's approval must never commit
/// somebody who did not agree</b>.
/// </para>
/// <para>
/// The exchange itself is <see cref="ShiftExchange"/> — the same implementation
/// the manager's own swap uses, so an approved proposal and a manager's
/// rearrangement cannot produce different rotas.
/// </para>
/// </remarks>
public class SwapProposalService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    ApproverResolver approvers,
    TimeProvider clock)
{
    /// <summary>Ask a colleague to exchange shifts.</summary>
    public async Task<SwapProposal> ProposeAsync(
        RequestScope scope, ProposeSwapCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.SwapPropose, "property", scope.PropertyId, cancellationToken);

        var mine = await LoadCellAsync(scope, command.ProposerAssignmentId, cancellationToken);
        var theirs = await LoadCellAsync(scope, command.ColleagueAssignmentId, cancellationToken);

        if (mine.StaffId == theirs.StaffId)
        {
            throw new InvalidRequestException(
                "a swap exchanges two people's shifts — these are the same person's");
        }

        await RefuseDuplicateAsync(scope.PropertyId, mine.Id, theirs.Id, cancellationToken);

        var now = clock.GetUtcNow();
        var proposal = new SwapProposal
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = scope.PropertyId,
            ProposerStaffId = mine.StaffId,
            ColleagueStaffId = theirs.StaffId,
            ProposerAssignmentId = mine.Id,
            ColleagueAssignmentId = theirs.Id,
            State = SwapProposalState.Proposed,
            Note = command.Note?.Trim() ?? string.Empty,

            // Both entry paths, one field — WF-Q9(b). Whether a staff member
            // raised this or a supervisor raised it for them is the difference
            // between a record and a claim.
            EnteredByUserId = scope.UserId,

            // The proposer's approver, resolved now: a proposal that changed
            // hands because a posting moved while it waited is one nobody is
            // accountable for.
            ApproverStaffId = await approvers.ResolveAsync(
                scope.PropertyId, mine.StaffId, Today(), cancellationToken),

            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.SwapProposals.Add(proposal);
        await db.SaveChangesAsync(cancellationToken);

        return proposal;
    }

    /// <summary>The colleague agrees. Nothing has changed on the rota yet.</summary>
    public async Task<SwapProposal> AcceptAsync(
        RequestScope scope, DecideSwapCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.SwapPropose, "property", scope.PropertyId, cancellationToken);

        var proposal = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(proposal, command.ExpectedVersion);
        RequireState(proposal, SwapProposalState.Proposed);

        var now = clock.GetUtcNow();

        proposal.State = SwapProposalState.Accepted;
        proposal.AcceptedAt = now;
        proposal.UpdatedAt = now;
        proposal.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return proposal;
    }

    /// <summary>Refuse it — as the colleague, or as the manager.</summary>
    /// <remarks>
    /// One operation for both, because they mean the same thing to the rota:
    /// nothing happens. The <i>state it was refused from</i> is what says who
    /// refused it, which the record already carries.
    /// </remarks>
    public async Task<SwapProposal> DeclineAsync(
        RequestScope scope, DecideSwapCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.SwapPropose, "property", scope.PropertyId, cancellationToken);

        var proposal = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(proposal, command.ExpectedVersion);

        if (proposal.State is not (SwapProposalState.Proposed or SwapProposalState.Accepted))
        {
            throw new InvalidRequestException(
                $"this proposal is {proposal.State} and cannot be declined");
        }

        var now = clock.GetUtcNow();

        proposal.State = SwapProposalState.Declined;
        proposal.DecisionNote = command.Note?.Trim() ?? string.Empty;
        proposal.DecidedAt = now;
        proposal.UpdatedAt = now;
        proposal.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return proposal;
    }

    /// <summary>Approve it, and exchange the two cells.</summary>
    /// <remarks>
    /// <b>Only from <see cref="SwapProposalState.Accepted"/>.</b> Approving a
    /// merely-proposed swap would be the manager committing somebody who has not
    /// agreed, which is the one thing the accept state exists to prevent.
    /// </remarks>
    public async Task<SwapProposal> ApproveAsync(
        RequestScope scope, DecideSwapCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.SwapApprove, "property", scope.PropertyId, cancellationToken);

        var proposal = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(proposal, command.ExpectedVersion);
        RequireState(proposal, SwapProposalState.Accepted);

        // A swap needs both sides. If a cell was cleared or reassigned while the
        // proposal sat waiting, approval is **refused** rather than half-applied
        // — WF-Q16: a record that cannot be true, not a judgment.
        var mine = await FindCellAsync(scope, proposal.ProposerAssignmentId, cancellationToken)
            ?? throw new InvalidRequestException(
                "the proposer's shift no longer exists — this swap cannot be approved");

        var theirs = await FindCellAsync(scope, proposal.ColleagueAssignmentId, cancellationToken)
            ?? throw new InvalidRequestException(
                "the colleague's shift no longer exists — this swap cannot be approved");

        var now = clock.GetUtcNow();

        // The same exchange the manager's own swap performs, so an approved
        // proposal and a rearrangement cannot produce different rotas.
        ShiftExchange.Apply(mine, theirs, now);

        proposal.State = SwapProposalState.Approved;
        proposal.DecisionNote = command.Note?.Trim() ?? string.Empty;
        proposal.DecidedAt = now;
        proposal.UpdatedAt = now;
        proposal.Version += 1;

        // One SaveChanges: both cells and the proposal, or none of them.
        await db.SaveChangesAsync(cancellationToken);
        return proposal;
    }

    /// <summary>Withdraw a proposal.</summary>
    public async Task<SwapProposal> CancelAsync(
        RequestScope scope, DecideSwapCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.SwapPropose, "property", scope.PropertyId, cancellationToken);

        var proposal = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(proposal, command.ExpectedVersion);

        if (proposal.State is SwapProposalState.Approved)
        {
            // An approved swap has already changed the rota. Undoing it is a new
            // swap somebody decides on, not a cancellation of an old one.
            throw new InvalidRequestException(
                "this swap has been approved and the rota has changed — swap back instead");
        }

        var now = clock.GetUtcNow();

        proposal.State = SwapProposalState.Cancelled;
        proposal.UpdatedAt = now;
        proposal.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return proposal;
    }

    /// <summary>What is waiting on one person, at whichever stage.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="staffId">The colleague or the approver.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Proposals awaiting this person's answer.</returns>
    /// <remarks>
    /// One query for both audiences, because the question a screen asks is
    /// <i>"what needs me"</i> — and a colleague and an approver are the same
    /// person as often as not in a small hotel.
    /// </remarks>
    public async Task<IReadOnlyList<SwapProposal>> WaitingOnAsync(
        RequestScope scope, Guid staffId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        return await db.SwapProposals
            .Where(p => p.PropertyId == scope.PropertyId
                        && ((p.State == SwapProposalState.Proposed
                             && p.ColleagueStaffId == staffId)
                            || (p.State == SwapProposalState.Accepted
                                && p.ApproverStaffId == staffId)))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private async Task RefuseDuplicateAsync(
        Guid propertyId, Guid mine, Guid theirs, CancellationToken cancellationToken)
    {
        // A second live proposal over the same pair of cells would let two
        // approvals exchange them twice, which lands back where it started and
        // reads as nothing having happened.
        var live = await db.SwapProposals.AnyAsync(
            p => p.PropertyId == propertyId
                 && (p.State == SwapProposalState.Proposed
                     || p.State == SwapProposalState.Accepted)
                 && (p.ProposerAssignmentId == mine || p.ColleagueAssignmentId == mine
                     || p.ProposerAssignmentId == theirs || p.ColleagueAssignmentId == theirs),
            cancellationToken);

        if (live)
        {
            throw new InvalidRequestException(
                "one of these shifts is already in a swap proposal awaiting an answer");
        }
    }

    private async Task<ShiftAssignment?> FindCellAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken) =>
        await db.ShiftAssignments.FirstOrDefaultAsync(
            a => a.Id == id && a.PropertyId == scope.PropertyId, cancellationToken);

    private async Task<ShiftAssignment> LoadCellAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken) =>
        await FindCellAsync(scope, id, cancellationToken)
        ?? throw new NotFoundException("shift assignment", id);

    private async Task<SwapProposal> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        var proposal = await db.SwapProposals.FirstOrDefaultAsync(
            p => p.Id == id && p.PropertyId == scope.PropertyId, cancellationToken);

        return proposal ?? throw new NotFoundException("swap proposal", id);
    }

    private static void RequireState(SwapProposal proposal, SwapProposalState expected)
    {
        if (proposal.State != expected)
        {
            throw new InvalidRequestException(
                $"this proposal is {proposal.State}, and that step needs it to be {expected}");
        }
    }

    private static void RequireVersion(SwapProposal proposal, long expected)
    {
        if (proposal.Version != expected)
        {
            throw new ConcurrencyException("swap proposal", proposal.Id, expected);
        }
    }
}
