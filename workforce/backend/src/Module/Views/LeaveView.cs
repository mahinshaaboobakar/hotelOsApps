using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Swaps;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Leave and requests — what somebody has, what they asked for, and what is
/// waiting on the person reading the screen.
/// </summary>
/// <remarks>
/// <para>
/// <b>A balance is stated, never enforced.</b> A request may be raised and
/// approved with the balance overdrawn — the approver sees the number and
/// decides. So the balance here is a fact on the screen and not a gate in
/// front of the button.
/// </para>
/// <para>
/// <b>The queue is what is waiting on <i>you</i>.</b> Both kinds live in it —
/// leave awaiting a decision and a swap two people have already agreed —
/// because they are one person's one list of things to do, and separating them
/// would make the count on the tab disagree with the list under it.
/// </para>
/// </remarks>
public static class LeaveView
{
    /// <summary>The balances, one person's requests, and the approval queue.</summary>
    public static async Task<object?> Board(ModuleCall call, CancellationToken cancellationToken)
    {
        var leave = call.Service<LeaveService>();
        var types = call.Service<LeaveTypeService>();
        var swaps = call.Service<SwapProposalService>();
        var directory = call.Service<IStaffDirectory>();

        // **Whose screen this is.** The queue is what is waiting on ONE person,
        // and the scope carries a user rather than a staff id — the two are
        // different identities and this application does not map between them.
        // So the viewer's posting is named by the caller, and where it is absent
        // the queue is empty rather than everybody's: a supervisor's list shown
        // to somebody else is a decision handed to the wrong person.
        var staffId = call.Optional("staffId")?.GetGuid();

        var catalogue = await types.ListAsync(call.Scope, false, cancellationToken);

        var waiting = staffId is { } approver
            ? await leave.QueueAsync(call.Scope, approver, cancellationToken)
            : [];

        var proposals = staffId is { } decider
            ? await swaps.WaitingOnAsync(call.Scope, decider, cancellationToken)
            : [];

        var mine = staffId is null
            ? []
            : (await leave.PendingAsync(call.Scope, cancellationToken))
                .Where(one => one.StaffId == staffId)
                .ToList();

        var balances = staffId is { } person
            ? await leave.BalancesAsync(call.Scope, person, cancellationToken)
            : new Dictionary<Guid, decimal>();

        var people = waiting.Select(one => one.StaffId)
            .Concat(proposals.Select(one => one.ProposerStaffId))
            .Concat(proposals.Select(one => one.ColleagueStaffId))
            .Distinct()
            .ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, people, cancellationToken);

        var byId = catalogue.ToDictionary(one => one.Id);

        return new
        {
            balances = catalogue.Select(type => new
            {
                type = type.Name,
                days = balances.TryGetValue(type.Id, out var held) ? held : 0m,
                // The entitlement, where the type accrues. Null where HR grants
                // it by hand: "2 of null" is drawn as a bare number, and a zero
                // would claim an allowance of none.
                of = type.AccrualPerMonth is { } rate ? rate * 12 : (decimal?)null,
                note = type.AccrualPerMonth is { } monthly
                    ? "accrues " + monthly.ToString("0.##") + " / month"
                    : "granted by HR",
            }).ToList(),
            requests = mine.Select(one => Request(one, byId)).ToList(),
            waiting = Queue(waiting, proposals, byId, names),
            // The swap detail pane is the open proposal's, and there is no open
            // proposal until somebody picks one. Null rather than the first in
            // the queue: a pane that opened itself onto a decision is a decision
            // somebody did not choose to look at.
            swap = (object?)null,
        };
    }

    /// <summary>Raise a request, or withdraw one.</summary>
    public static Task<object?> Request(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "raise" => Raise(call, cancellationToken),
            "withdraw" => Withdraw(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a leave method"),
        };

    /// <summary>Approve, decline, or correct a balance.</summary>
    public static Task<object?> Decide(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "approve" => Approve(call, cancellationToken),
            "decline" => Decline(call, cancellationToken),
            "adjust" => Adjust(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a decision method"),
        };

    /// <summary>Propose an exchange, accept one, or decline it.</summary>
    public static Task<object?> Propose(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "propose" => ProposeSwap(call, cancellationToken),
            "accept" => AcceptSwap(call, cancellationToken),
            "declineSwap" => DeclineSwap(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a swap method"),
        };

    /// <summary>Approve a swap the two people have already agreed.</summary>
    public static async Task<object?> DecideSwap(
        ModuleCall call, CancellationToken cancellationToken)
    {
        if (call.Method != "approveSwap")
        {
            throw new InvalidRequestException(call.Method + " is not a swap approval");
        }

        var proposal = await call.Service<SwapProposalService>().ApproveAsync(
            call.Scope, SwapDecision(call), cancellationToken);

        return new { id = proposal.Id, version = proposal.Version, state = proposal.State.ToString() };
    }

    private static async Task<object?> Raise(ModuleCall call, CancellationToken cancellationToken)
    {
        var request = await call.Service<LeaveService>().RaiseAsync(
            call.Scope,
            new RaiseLeaveCommand
            {
                StaffId = call.Id("staffId"),
                LeaveTypeId = call.Id("typeId"),
                From = call.Date("from"),
                To = call.Date("to"),
                Note = call.Optional("note")?.GetString(),
            },
            cancellationToken);

        return new { id = request.Id, version = request.Version, days = request.Days };
    }

    private static async Task<object?> Withdraw(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var request = await call.Service<LeaveService>().CancelAsync(
            call.Scope, Decision(call), cancellationToken);

        return new { id = request.Id, version = request.Version, state = request.State.ToString() };
    }

    private static async Task<object?> Approve(ModuleCall call, CancellationToken cancellationToken)
    {
        var request = await call.Service<LeaveService>().ApproveAsync(
            call.Scope, Decision(call), cancellationToken);

        return new { id = request.Id, version = request.Version, state = request.State.ToString() };
    }

    private static async Task<object?> Decline(ModuleCall call, CancellationToken cancellationToken)
    {
        var request = await call.Service<LeaveService>().DeclineAsync(
            call.Scope, Decision(call), cancellationToken);

        return new { id = request.Id, version = request.Version, state = request.State.ToString() };
    }

    /// <summary>
    /// Correct a balance, with the reason recorded beside the number.
    /// </summary>
    /// <remarks>
    /// The note is required by the command rather than optional here: a
    /// correction nobody explained is a number that will be queried in six
    /// months by somebody with no way to find out why.
    /// </remarks>
    private static async Task<object?> Adjust(ModuleCall call, CancellationToken cancellationToken)
    {
        var entry = await call.Service<LeaveService>().AdjustAsync(
            call.Scope,
            new AdjustBalanceCommand
            {
                StaffId = call.Id("staffId"),
                LeaveTypeId = call.Id("typeId"),
                Days = call.Required("days").GetDecimal(),
                Note = call.Text("note"),
            },
            cancellationToken);

        return new { id = entry.Id };
    }

    private static async Task<object?> ProposeSwap(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var proposal = await call.Service<SwapProposalService>().ProposeAsync(
            call.Scope,
            new ProposeSwapCommand
            {
                ProposerAssignmentId = call.Id("mine"),
                ColleagueAssignmentId = call.Id("theirs"),
                Note = call.Optional("note")?.GetString(),
            },
            cancellationToken);

        return new { id = proposal.Id, version = proposal.Version };
    }

    private static async Task<object?> AcceptSwap(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var proposal = await call.Service<SwapProposalService>().AcceptAsync(
            call.Scope, SwapDecision(call), cancellationToken);

        return new { id = proposal.Id, version = proposal.Version, state = proposal.State.ToString() };
    }

    private static async Task<object?> DeclineSwap(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var proposal = await call.Service<SwapProposalService>().DeclineAsync(
            call.Scope, SwapDecision(call), cancellationToken);

        return new { id = proposal.Id, version = proposal.Version, state = proposal.State.ToString() };
    }

    /// <summary>The id, the version read, and what the decider wants recorded.</summary>
    private static DecideLeaveCommand Decision(ModuleCall call) => new()
    {
        Id = call.Id("id"),
        ExpectedVersion = call.Required("version").GetInt64(),
        Note = call.Optional("note")?.GetString(),
    };

    /// <summary>The same three fields, in the swap domain's own command.</summary>
    /// <remarks>
    /// Two records rather than one shared across both domains: a swap and a
    /// leave request are different aggregates, and a command type spanning them
    /// would make either free to gain a field the other cannot carry.
    /// </remarks>
    private static DecideSwapCommand SwapDecision(ModuleCall call) => new()
    {
        Id = call.Id("id"),
        ExpectedVersion = call.Required("version").GetInt64(),
        Note = call.Optional("note")?.GetString(),
    };

    /// <summary>One of somebody's own requests.</summary>
    private static object Request(LeaveRequest request, IReadOnlyDictionary<Guid, LeaveType> types)
        => new
        {
            type = types.TryGetValue(request.LeaveTypeId, out var type) ? type.Name : null,
            note = string.IsNullOrWhiteSpace(request.Note) ? "—" : request.Note,
            dates = Dates(request.From, request.To),
            days = request.Days,
            state = request.State.ToString(),
        };

    /// <summary>What is waiting on the reader: both kinds, in one list.</summary>
    private static List<object> Queue(
        IReadOnlyList<LeaveRequest> leave,
        IReadOnlyList<SwapProposal> swaps,
        IReadOnlyDictionary<Guid, LeaveType> types,
        IReadOnlyDictionary<Guid, string> names)
    {
        var rows = leave.Select(one => (object)new
        {
            who = names.TryGetValue(one.StaffId, out var name) ? name : null,
            what = (types.TryGetValue(one.LeaveTypeId, out var type) ? type.Name : "Leave")
                   + " · " + one.Days + " days",
            kind = "Leave",
            dates = Dates(one.From, one.To),
        }).ToList();

        rows.AddRange(swaps.Select(one => (object)new
        {
            who = Pair(one, names),
            what = "Swap — accepted, awaiting you",
            kind = "Swap",
            dates = one.AcceptedAt?.ToString("d MMM") ?? "—",
        }));

        return rows;
    }

    /// <summary>"Anjali Menon and Sneha Iyer", or whichever half has a name.</summary>
    private static string? Pair(SwapProposal swap, IReadOnlyDictionary<Guid, string> names)
    {
        names.TryGetValue(swap.ProposerStaffId, out var proposer);
        names.TryGetValue(swap.ColleagueStaffId, out var colleague);

        return proposer is null && colleague is null
            ? null
            : string.Join(" & ", new[] { proposer, colleague }.Where(one => one is not null));
    }

    /// <summary>"7 – 8 Sep", or "3 Aug" when it is one day.</summary>
    private static string Dates(DateOnly from, DateOnly to)
        => from == to
            ? from.ToString("d MMM")
            : from.Month == to.Month
                ? from.Day + " – " + to.ToString("d MMM")
                : from.ToString("d MMM") + " – " + to.ToString("d MMM");
}
