using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Leave: a ledger, an approver resolved from postings, and a balance that may
/// go negative.
/// </summary>
/// <remarks>
/// The rules are <c>WF-Q5</c> (warn-and-allow, so a negative balance is a real
/// state), <c>WF-Q11</c> (a rate, not an annual allowance), <c>WF-Q12</c>
/// (Week-off is not a leave type), <c>WF-Q13</c> (comp-off is granted, not
/// accrued) and the country-seed ruling.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class LeaveCharacterisationTests(WorkforceFixture fixture)
{
    private static int day = -1;

    private static DateOnly SomeMonday() =>
        new DateOnly(2027, 1, 4).AddDays(Interlocked.Increment(ref day) * 14);

    [Fact]
    public async Task The_seed_template_is_chosen_by_the_property_and_never_a_literal()
    {
        var scope = fixture.OtherPropertyScope();
        var (_, types, directory, _) = Build();
        directory.Country = "IN";

        await types.SeedAsync(scope, default);
        var seeded = await types.ListAsync(scope, includeRetired: false, default);

        // The owner's own four, because this property is in India — not because
        // the list was written into the product.
        Assert.Equal(["CASUAL", "COMPOFF", "EARNED", "SICK"], seeded.Select(t => t.Code));

        // "Monthly 2" — a rate, which an annual allowance cannot express.
        Assert.Equal(2m, seeded.Single(t => t.Code == "CASUAL").AccrualPerMonth);

        // WF-Q13: comp-off is granted by HR, so it accrues nothing. Null is not
        // zero — a type granted by hand is different from one whose rate is none.
        Assert.Null(seeded.Single(t => t.Code == "COMPOFF").AccrualPerMonth);

        // WF-Q12: Week-off is a rota marker and is not here.
        Assert.DoesNotContain(seeded, t => t.Code.Contains("OFF", StringComparison.Ordinal)
                                           && t.Code != "COMPOFF");
    }

    [Fact]
    public async Task A_gulf_property_gets_a_different_vocabulary()
    {
        var scope = fixture.OtherPropertyScope();
        var (_, types, directory, _) = Build();
        directory.Country = "AE";

        await types.SeedAsync(scope, default);
        var seeded = await types.ListAsync(scope, includeRetired: false, default);

        // Casual and Earned are one region's words. Seeding them everywhere is
        // the country-in-the-product mistake the ruling exists to prevent.
        Assert.Contains(seeded, t => t.Code == "ANNUAL");
        Assert.DoesNotContain(seeded, t => t.Code == "CASUAL");
    }

    [Fact]
    public async Task A_property_that_has_not_said_where_it_is_gets_the_neutral_template()
    {
        var scope = fixture.OtherPropertyScope();
        var (_, types, directory, _) = Build();
        directory.Country = null;

        await types.SeedAsync(scope, default);
        var seeded = await types.ListAsync(scope, includeRetired: false, default);

        // Neutral rather than nearest: guessing a region from a currency or a
        // timezone would be the same mistake wearing a different field.
        Assert.Equal(["ANNUAL", "SICK"], seeded.Select(t => t.Code));
    }

    [Fact]
    public async Task Seeding_twice_adds_nothing_and_overwrites_nothing()
    {
        var scope = fixture.OtherPropertyScope();
        var (_, types, directory, _) = Build();
        directory.Country = "IN";

        var first = await types.SeedAsync(scope, default);

        var casual = (await types.ListAsync(scope, false, default)).Single(t => t.Code == "CASUAL");
        await types.SetAsync(
            scope,
            new SetLeaveTypeCommand
            {
                Id = casual.Id,
                ExpectedVersion = casual.Version,
                Code = casual.Code,
                Name = casual.Name,
                AccrualPerMonth = 3m,
            },
            default);

        var second = await types.SeedAsync(scope, default);
        var after = (await types.ListAsync(scope, false, default)).Single(t => t.Code == "CASUAL");

        // A property that has configured its types has made decisions the seed
        // must not undo.
        Assert.Equal(4, first);
        Assert.Equal(0, second);
        Assert.Equal(3m, after.AccrualPerMonth);
    }

    [Fact]
    public async Task Approval_debits_the_balance_and_request_does_not()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN1");
        var staff = Guid.CreateVersion7();
        var from = SomeMonday();

        var request = await leave.RaiseAsync(
            scope, Raise(staff, type, from, from.AddDays(2)), default);

        var beforeDecision = await leave.BalancesAsync(scope, staff, default);

        // Debiting on request would let an undecided request hide capacity from
        // everybody else, and a declined one would need an unwind that could be
        // missed.
        Assert.Empty(beforeDecision);

        await leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = request.Id, ExpectedVersion = request.Version },
            default);

        var after = await leave.BalancesAsync(scope, staff, default);
        Assert.Equal(-3m, after[type]);
    }

    [Fact]
    public async Task Cancelling_an_approved_request_credits_the_balance_back()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN2");
        var staff = Guid.CreateVersion7();
        var from = SomeMonday();

        var request = await leave.RaiseAsync(
            scope, Raise(staff, type, from, from.AddDays(1)), default);

        var approved = await leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = request.Id, ExpectedVersion = request.Version },
            default);

        await leave.CancelAsync(
            scope,
            new DecideLeaveCommand { Id = approved.Id, ExpectedVersion = approved.Version },
            default);

        var balances = await leave.BalancesAsync(scope, staff, default);

        // A debit with no matching credit turns a cancellation into a silent
        // forfeit — the kind of arithmetic somebody notices a year later and
        // cannot reconstruct.
        Assert.Equal(0m, balances[type]);
    }

    [Fact]
    public async Task Declining_never_moves_the_balance()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN3");
        var staff = Guid.CreateVersion7();
        var from = SomeMonday();

        var request = await leave.RaiseAsync(scope, Raise(staff, type, from, from), default);

        await leave.DeclineAsync(
            scope,
            new DecideLeaveCommand
            {
                Id = request.Id,
                ExpectedVersion = request.Version,
                Note = "Two already away that week",
            },
            default);

        Assert.Empty(await leave.BalancesAsync(scope, staff, default));
    }

    [Fact]
    public async Task An_exhausted_balance_goes_negative_rather_than_refusing()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN4");
        var staff = Guid.CreateVersion7();
        var from = SomeMonday();

        await leave.AdjustAsync(
            scope,
            new AdjustBalanceCommand
            {
                StaffId = staff,
                LeaveTypeId = type,
                Days = 1m,
                Note = "Opening balance",
            },
            default);

        var request = await leave.RaiseAsync(
            scope, Raise(staff, type, from, from.AddDays(4)), default);

        await leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = request.Id, ExpectedVersion = request.Version },
            default);

        var balances = await leave.BalancesAsync(scope, staff, default);

        // WF-Q5: warn-and-allow. Hotels override reality daily, so an overdrawn
        // balance is an approved state and not an error — and every screen that
        // shows one must survive a minus sign.
        Assert.Equal(-4m, balances[type]);
    }

    [Fact]
    public async Task The_approver_is_the_reporting_manager_when_the_posting_names_one()
    {
        var (leave, types, _, postings) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN5");
        var staff = Guid.CreateVersion7();
        var manager = Guid.CreateVersion7();
        var head = Guid.CreateVersion7();
        var from = SomeMonday();

        await postings.CreateAsync(scope, Post(head, "HK", isHead: true), default);
        await postings.CreateAsync(
            scope, Post(staff, "HK") with { ReportingManagerStaffId = manager }, default);

        var request = await leave.RaiseAsync(scope, Raise(staff, type, from, from), default);

        // One rule, one queue: the reporting manager when the posting names one.
        // Chapter 01's "manager or head" with no precedence was two queues.
        Assert.Equal(manager, request.ApproverStaffId);
    }

    [Fact]
    public async Task The_approver_is_the_department_head_when_no_manager_is_named()
    {
        var (leave, types, _, postings) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN6");
        var staff = Guid.CreateVersion7();
        var head = Guid.CreateVersion7();
        var from = SomeMonday();

        await postings.CreateAsync(scope, Post(head, "SPA", isHead: true), default);
        await postings.CreateAsync(scope, Post(staff, "SPA"), default);

        var request = await leave.RaiseAsync(scope, Raise(staff, type, from, from), default);

        // Resolved through this application's own postings, which is why
        // Workforce can answer "whose request is this" at all — ADR 0116 §6.
        Assert.Equal(head, request.ApproverStaffId);
    }

    [Fact]
    public async Task A_department_heads_own_leave_has_no_approver_yet()
    {
        var (leave, types, _, postings) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN7");
        var head = Guid.CreateVersion7();
        var from = SomeMonday();

        await postings.CreateAsync(scope, Post(head, "ENG", isHead: true), default);

        var request = await leave.RaiseAsync(scope, Raise(head, type, from, from), default);

        // It goes to the general manager, and that hook is unwritten (ADR 0114
        // §5). Null is the honest answer: inventing a holder would be worse than
        // an unassigned queue somebody can see.
        Assert.Null(request.ApproverStaffId);
    }

    [Fact]
    public async Task Overlapping_leave_for_one_person_is_refused()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN8");
        var staff = Guid.CreateVersion7();
        var from = SomeMonday();

        await leave.RaiseAsync(scope, Raise(staff, type, from, from.AddDays(3)), default);

        // Two live requests over one day would debit the balance twice for one
        // absence, and a person cannot be away twice.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => leave.RaiseAsync(
                scope, Raise(staff, type, from.AddDays(2), from.AddDays(5)), default));
    }

    [Fact]
    public async Task A_decided_request_cannot_be_decided_again()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANN9");
        var from = SomeMonday();

        var request = await leave.RaiseAsync(
            scope, Raise(Guid.CreateVersion7(), type, from, from), default);

        var approved = await leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = request.Id, ExpectedVersion = request.Version },
            default);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => leave.DeclineAsync(
                scope,
                new DecideLeaveCommand { Id = approved.Id, ExpectedVersion = approved.Version },
                default));
    }

    [Fact]
    public async Task An_adjustment_says_why_and_is_attributed()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANNA");
        var staff = Guid.CreateVersion7();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => leave.AdjustAsync(
                scope,
                new AdjustBalanceCommand
                {
                    StaffId = staff,
                    LeaveTypeId = type,
                    Days = 2m,
                    Note = "  ",
                },
                default));

        var entry = await leave.AdjustAsync(
            scope,
            new AdjustBalanceCommand
            {
                StaffId = staff,
                LeaveTypeId = type,
                Days = 2m,
                Note = "Comp-off for Onam",
            },
            default);

        // The manual floor, recorded and attributed — never a silent overwrite.
        Assert.Equal(LeaveLedgerKind.Adjustment, entry.Kind);
        Assert.Equal(scope.UserId, entry.RecordedByUserId);
    }

    [Fact]
    public async Task A_request_records_who_raised_it()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANNB");
        var from = SomeMonday();

        var request = await leave.RaiseAsync(
            scope, Raise(Guid.CreateVersion7(), type, from, from), default);

        // WF-Q9(b)'s provenance obligation at its third surface: without it the
        // record quietly claims a staff member raised what a supervisor raised
        // for them.
        Assert.Equal(scope.UserId, request.EnteredByUserId);
    }

    [Fact]
    public async Task Leave_that_ends_before_it_starts_is_refused()
    {
        var (leave, types, _, _) = Build();
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANNC");
        var from = SomeMonday();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => leave.RaiseAsync(
                scope, Raise(Guid.CreateVersion7(), type, from, from.AddDays(-1)), default));
    }

    [Fact]
    public async Task Raising_asks_for_leave_request_and_deciding_for_leave_approve()
    {
        var (leave, types, _, _) = Build(out var authorizer);
        var scope = fixture.Scope();
        var type = await Type(types, scope, "ANND");
        var from = SomeMonday();
        var seen = authorizer.Checks.Count;

        var request = await leave.RaiseAsync(
            scope, Raise(Guid.CreateVersion7(), type, from, from), default);
        await leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = request.Id, ExpectedVersion = request.Version },
            default);

        Assert.Equal(
            ["leave.request", "leave.approve"],
            authorizer.Checks.Skip(seen).Select(c => c.Permission));
    }

    private static RaiseLeaveCommand Raise(Guid staff, Guid type, DateOnly from, DateOnly to) =>
        new() { StaffId = staff, LeaveTypeId = type, From = from, To = to };

    private static CreatePostingCommand Post(Guid staff, string department, bool isHead = false) =>
        new()
        {
            StaffId = staff,
            DepartmentCode = department,
            JobRole = "Attendant",
            IsDepartmentHead = isHead,
            EffectiveFrom = new DateOnly(2026, 1, 1),
        };

    private static async Task<Guid> Type(LeaveTypeService types, RequestScope scope, string code)
    {
        var type = await types.SetAsync(
            scope,
            new SetLeaveTypeCommand { Code = code, Name = $"Leave {code}", AccrualPerMonth = 2m },
            default);

        return type.Id;
    }

    private (LeaveService Leave, LeaveTypeService Types, StaffDirectoryDouble Directory,
        PostingService Postings) Build() => Build(out _);

    private (LeaveService Leave, LeaveTypeService Types, StaffDirectoryDouble Directory,
        PostingService Postings) Build(out RecordingAuthorizer authorizer)
    {
        authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var db = fixture.Context();

        return (
            new LeaveService(db, authorizer, new ApproverResolver(db), TimeProvider.System),
            new LeaveTypeService(db, authorizer, directory, TimeProvider.System),
            directory,
            new PostingService(
                db, authorizer, directory,
                new PostingAnnouncer(new RecordingEventAppender(), directory),
                new TeamService(db, authorizer, directory, TimeProvider.System),
                TimeProvider.System));
    }
}
