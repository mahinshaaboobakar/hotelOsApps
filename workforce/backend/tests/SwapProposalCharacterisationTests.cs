using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Application.Swaps;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Swap proposals: proposed → accepted → approved, and the accept step that
/// <c>WF-Q9</c>(a) bought.
/// </summary>
/// <remarks>
/// The rule the whole aggregate exists for: <b>a manager's approval must never
/// commit somebody who did not agree.</b>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class SwapProposalCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;

    private static DateOnly SomeDay() =>
        new DateOnly(2028, 1, 3).AddDays(Interlocked.Increment(ref slot) * 7);

    [Fact]
    public async Task A_proposal_takes_effect_only_after_both_the_colleague_and_the_manager()
    {
        var world = await World();

        var proposal = await world.Swaps.ProposeAsync(
            world.Scope, world.Propose(), default);

        Assert.Equal(SwapProposalState.Proposed, proposal.State);
        await AssertUnchanged(world);

        var accepted = await world.Swaps.AcceptAsync(
            world.Scope, Decide(proposal), default);

        Assert.Equal(SwapProposalState.Accepted, accepted.State);
        Assert.NotNull(accepted.AcceptedAt);

        // Still nothing: acceptance is agreement, not authority.
        await AssertUnchanged(world);

        await world.Swaps.ApproveAsync(world.Scope, Decide(accepted), default);

        var cells = await world.Rota.ReadAsync(
            world.Scope, new RotaQuery { From = world.Day, To = world.Day }, default);

        Assert.Equal(world.Evening, cells.Single(c => c.StaffId == world.Anjali).CatalogueEntryId);
        Assert.Equal(world.Morning, cells.Single(c => c.StaffId == world.Sneha).CatalogueEntryId);
    }

    [Fact]
    public async Task A_manager_cannot_approve_what_the_colleague_has_not_accepted()
    {
        var world = await World();

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);

        // The one thing the accept state exists to prevent. Approving here would
        // volunteer somebody's Saturday for them.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => world.Swaps.ApproveAsync(world.Scope, Decide(proposal), default));

        await AssertUnchanged(world);
    }

    [Fact]
    public async Task The_colleague_may_decline_and_the_rota_is_untouched()
    {
        var world = await World();

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);

        var declined = await world.Swaps.DeclineAsync(
            world.Scope,
            new DecideSwapCommand
            {
                Id = proposal.Id,
                ExpectedVersion = proposal.Version,
                Note = "I have a class that evening",
            },
            default);

        Assert.Equal(SwapProposalState.Declined, declined.State);
        await AssertUnchanged(world);
    }

    [Fact]
    public async Task The_manager_may_decline_after_the_colleague_accepted()
    {
        var world = await World();

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);
        var accepted = await world.Swaps.AcceptAsync(world.Scope, Decide(proposal), default);

        var declined = await world.Swaps.DeclineAsync(world.Scope, Decide(accepted), default);

        // One operation for both refusals, because they mean the same thing to
        // the rota: nothing happens. The state it was refused *from* says who.
        Assert.Equal(SwapProposalState.Declined, declined.State);
        await AssertUnchanged(world);
    }

    [Fact]
    public async Task Approval_is_refused_when_one_of_the_two_shifts_is_gone()
    {
        var world = await World();

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);
        var accepted = await world.Swaps.AcceptAsync(world.Scope, Decide(proposal), default);

        await world.Rota.ClearAsync(
            world.Scope,
            new ClearShiftCommand { StaffId = world.Sneha, Date = world.Day },
            default);

        // A swap needs both sides. Refused rather than half-applied — WF-Q16: a
        // record that cannot be true, not a judgment.
        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => world.Swaps.ApproveAsync(world.Scope, Decide(accepted), default));

        Assert.Contains("no longer exists", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_proposal_records_who_entered_it()
    {
        var world = await World();

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);

        // WF-Q9(b): both entry paths, one field. A staff member with a login
        // proposes for themselves; a supervisor proposes for everyone else — and
        // most staff have no account, so most proposals arrive the second way.
        Assert.Equal(world.Scope.UserId, proposal.EnteredByUserId);
    }

    [Fact]
    public async Task The_proposers_approver_is_resolved_when_it_is_raised()
    {
        var world = await World(withHead: true);

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);

        // The same resolver leave uses — one rule, one queue. A proposal that
        // changed hands because a posting moved while it waited is one nobody is
        // accountable for.
        Assert.Equal(world.Head, proposal.ApproverStaffId);
    }

    [Fact]
    public async Task A_swap_with_oneself_is_refused()
    {
        var world = await World();

        var second = await world.Rota.AssignAsync(
            world.Scope,
            new AssignShiftCommand
            {
                StaffId = world.Anjali,
                Date = world.Day.AddDays(1),
                CatalogueEntryId = world.Evening,
                DepartmentCode = "FO",
            },
            default);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => world.Swaps.ProposeAsync(
                world.Scope,
                new ProposeSwapCommand
                {
                    ProposerAssignmentId = world.AnjaliCell,
                    ColleagueAssignmentId = second.Id,
                },
                default));
    }

    [Fact]
    public async Task A_second_live_proposal_over_the_same_shift_is_refused()
    {
        var world = await World();

        await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);

        // Two approvals would exchange the same pair twice, which lands back
        // where it started and reads as nothing having happened.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => world.Swaps.ProposeAsync(world.Scope, world.Propose(), default));
    }

    [Fact]
    public async Task An_approved_swap_cannot_be_cancelled()
    {
        var world = await World();

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);
        var accepted = await world.Swaps.AcceptAsync(world.Scope, Decide(proposal), default);
        var approved = await world.Swaps.ApproveAsync(world.Scope, Decide(accepted), default);

        // The rota has already changed. Undoing it is a new swap somebody
        // decides on, not a cancellation of an old one.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => world.Swaps.CancelAsync(world.Scope, Decide(approved), default));
    }

    [Fact]
    public async Task What_is_waiting_on_a_person_covers_both_stages()
    {
        var world = await World(withHead: true);

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);

        var onColleague = await world.Swaps.WaitingOnAsync(world.Scope, world.Sneha, default);
        Assert.Contains(onColleague, p => p.Id == proposal.Id);

        var accepted = await world.Swaps.AcceptAsync(world.Scope, Decide(proposal), default);

        var onApprover = await world.Swaps.WaitingOnAsync(world.Scope, world.Head, default);
        Assert.Contains(onApprover, p => p.Id == accepted.Id);

        // And it has left the colleague's list, because it no longer needs them.
        var colleagueAgain = await world.Swaps.WaitingOnAsync(world.Scope, world.Sneha, default);
        Assert.DoesNotContain(colleagueAgain, p => p.Id == accepted.Id);
    }

    [Fact]
    public async Task Proposing_asks_for_swap_propose_and_approving_for_swap_approve()
    {
        var world = await World();
        var seen = world.Authorizer.Checks.Count;

        var proposal = await world.Swaps.ProposeAsync(world.Scope, world.Propose(), default);
        var accepted = await world.Swaps.AcceptAsync(world.Scope, Decide(proposal), default);
        await world.Swaps.ApproveAsync(world.Scope, Decide(accepted), default);

        Assert.Equal(
            ["swap.propose", "swap.propose", "swap.approve"],
            world.Authorizer.Checks.Skip(seen).Select(c => c.Permission));
    }

    private static DecideSwapCommand Decide(SwapProposal proposal) =>
        new() { Id = proposal.Id, ExpectedVersion = proposal.Version };

    private async Task AssertUnchanged(Fixture world)
    {
        var cells = await world.Rota.ReadAsync(
            world.Scope, new RotaQuery { From = world.Day, To = world.Day }, default);

        Assert.Equal(world.Morning, cells.Single(c => c.StaffId == world.Anjali).CatalogueEntryId);
        Assert.Equal(world.Evening, cells.Single(c => c.StaffId == world.Sneha).CatalogueEntryId);
    }

    private sealed record Fixture(
        RequestScope Scope,
        RotaService Rota,
        SwapProposalService Swaps,
        RecordingAuthorizer Authorizer,
        DateOnly Day,
        Guid Anjali,
        Guid Sneha,
        Guid Head,
        Guid Morning,
        Guid Evening,
        Guid AnjaliCell,
        Guid SnehaCell)
    {
        public ProposeSwapCommand Propose() => new()
        {
            ProposerAssignmentId = AnjaliCell,
            ColleagueAssignmentId = SnehaCell,
        };
    }

    private async Task<Fixture> World(bool withHead = false)
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var db = fixture.Context();
        var clock = TimeProvider.System;

        var shifts = new ShiftCatalogueService(db, authorizer, clock);
        var rota = new RotaService(db, authorizer, clock);
        var postings = new PostingService(db, authorizer, directory, new RecordingEventAppender(), clock);
        var swaps = new SwapProposalService(db, authorizer, new ApproverResolver(db), clock);

        var scope = fixture.Scope();
        var day = SomeDay();
        var anjali = Uuid7.NewUuid7();
        var sneha = Uuid7.NewUuid7();
        var head = Uuid7.NewUuid7();

        if (withHead)
        {
            // Its own department per test: a department has one head, and a
            // shared fixture would otherwise have several tests competing for
            // the same one — which is how the missing invariant was found.
            var department = $"D{Interlocked.Increment(ref slot)}";

            await postings.CreateAsync(scope, Posting(head, department, isHead: true), default);
            await postings.CreateAsync(scope, Posting(anjali, department), default);
        }

        var morning = await Shift(shifts, scope, "SWM", 7, 15);
        var evening = await Shift(shifts, scope, "SWE", 15, 23);

        var anjaliCell = await rota.AssignAsync(scope, Cell(anjali, day, morning), default);
        var snehaCell = await rota.AssignAsync(scope, Cell(sneha, day, evening), default);

        return new Fixture(
            scope, rota, swaps, authorizer, day, anjali, sneha, head,
            morning, evening, anjaliCell.Id, snehaCell.Id);
    }

    private static CreatePostingCommand Posting(Guid staff, string department, bool isHead = false) => new()
    {
        StaffId = staff,
        DepartmentCode = department,
        JobRole = "Receptionist",
        IsDepartmentHead = isHead,

        // In force **today**, because that is when the approver is resolved — a
        // request is raised now, so the posting that decides who reads it is the
        // one in force now, not the one covering the shift's date. A posting
        // dated in the future correctly resolves to nobody, which is what this
        // test originally proved by accident.
        EffectiveFrom = new DateOnly(2026, 1, 1),
    };

    private static AssignShiftCommand Cell(Guid staff, DateOnly date, Guid entry) => new()
    {
        StaffId = staff,
        Date = date,
        CatalogueEntryId = entry,
        DepartmentCode = "FO",
    };

    private static async Task<Guid> Shift(
        ShiftCatalogueService shifts, RequestScope scope, string code, int from, int to)
    {
        var entry = await shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = $"Shift {code}",
                ShortCode = $"{code}{Interlocked.Increment(ref slot)}",
                Colour = "cyan",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(from, 0),
                    EndsAt = new TimeOnly(to, 0),
                },
                EffectiveFrom = new DateOnly(2027, 1, 1),
            },
            default);

        return entry.Id;
    }
}
