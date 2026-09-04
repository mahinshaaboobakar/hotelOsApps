using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Assignment;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Where the capability register meets the rota — and every answer is a warning.
/// </summary>
/// <remarks>
/// <c>WF-Q16</c>: the platform refuses the physically impossible and warns on a
/// judgment. Every case here is a judgment a hotel makes daily, so nothing in
/// this suite asserts a refusal.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class AssignmentAdviceCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;

    private static DateOnly SomeDay() =>
        new DateOnly(2031, 1, 6).AddDays(Interlocked.Increment(ref slot) * 5);

    private static string OwnDepartment() => $"AD{Interlocked.Increment(ref slot)}";

    [Fact]
    public async Task Somebody_posted_and_free_draws_no_advice()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var department = OwnDepartment();
        var staff = Uuid7.NewUuid7();

        await w.Postings.CreateAsync(scope, Post(staff, department), default);

        var advice = await w.Advisor.AdviseAsync(scope, staff, day, department, default);

        // Empty means nothing to say — not that the assignment was checked and
        // approved, because nothing here approves anything.
        Assert.Empty(advice);
    }

    [Fact]
    public async Task Approved_leave_is_advised_and_a_pending_request_separately()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var department = OwnDepartment();
        var onLeave = Uuid7.NewUuid7();
        var awaiting = Uuid7.NewUuid7();
        var type = await LeaveType(w, scope);

        await w.Postings.CreateAsync(scope, Post(onLeave, department), default);
        await w.Postings.CreateAsync(scope, Post(awaiting, department), default);

        var approved = await w.Leave.RaiseAsync(scope, Ask(onLeave, type, day), default);
        await w.Leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = approved.Id, ExpectedVersion = approved.Version },
            default);

        await w.Leave.RaiseAsync(scope, Ask(awaiting, type, day), default);

        var forLeave = await w.Advisor.AdviseAsync(scope, onLeave, day, department, default);
        var forPending = await w.Advisor.AdviseAsync(scope, awaiting, day, department, default);

        // Two different facts. Rostering somebody whose request is still open is
        // not wrong, but the approver should know the rota now assumes an answer.
        Assert.Equal(AdviceKind.OnApprovedLeave, Assert.Single(forLeave).Kind);
        Assert.Equal(AdviceKind.LeaveRequested, Assert.Single(forPending).Kind);
    }

    [Fact]
    public async Task Covering_another_department_is_advised_never_refused()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var staff = Uuid7.NewUuid7();

        await w.Postings.CreateAsync(scope, Post(staff, OwnDepartment()), default);

        var advice = await w.Advisor.AdviseAsync(scope, staff, day, "BQT", default);

        // Front office covers a banquet on a busy Saturday, and a system that
        // refused it would be worked around by Monday.
        Assert.Equal(AdviceKind.NotPostedToDepartment, Assert.Single(advice).Kind);
    }

    [Fact]
    public async Task A_certificate_expiring_between_today_and_the_shift_is_advised_as_expired()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var department = OwnDepartment();
        var staff = Uuid7.NewUuid7();

        await w.Postings.CreateAsync(scope, Post(staff, department), default);

        await w.Capabilities.RecordAsync(
            scope,
            new RecordCapabilityCommand
            {
                StaffId = staff,
                Name = "Fire warden",
                ValidUntil = day.AddDays(-1),
            },
            default);

        var advice = await w.Advisor.AdviseAsync(scope, staff, day, department, default);

        // Measured against the day being filled, never against today. A
        // certificate valid now and expired by the shift is exactly the case a
        // manager needs to see, and checking against today would hide it.
        var only = Assert.Single(advice);
        Assert.Equal(AdviceKind.CertificationExpired, only.Kind);
        Assert.Contains("Fire warden", only.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_certificate_still_valid_on_the_day_is_advised_as_expiring()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var department = OwnDepartment();
        var staff = Uuid7.NewUuid7();

        await w.Postings.CreateAsync(scope, Post(staff, department), default);

        await w.Capabilities.RecordAsync(
            scope,
            new RecordCapabilityCommand
            {
                StaffId = staff,
                Name = "First aid",
                ValidUntil = day.AddDays(30),
            },
            default);

        var advice = await w.Advisor.AdviseAsync(scope, staff, day, department, default);

        Assert.Equal(AdviceKind.CertificationExpiring, Assert.Single(advice).Kind);
    }

    [Fact]
    public async Task An_ability_with_no_expiry_is_never_advised()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var department = OwnDepartment();
        var staff = Uuid7.NewUuid7();

        await w.Postings.CreateAsync(scope, Post(staff, department), default);

        await w.Capabilities.RecordAsync(
            scope,
            new RecordCapabilityCommand { StaffId = staff, Name = "Speaks Arabic" },
            default);

        // The date is the discriminator — slice 2. An ability cannot lapse, so
        // there is nothing to warn about, and the filtered query never sees it.
        Assert.Empty(await w.Advisor.AdviseAsync(scope, staff, day, department, default));
    }

    [Fact]
    public async Task A_certificate_expiring_far_beyond_the_horizon_is_not_advised()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var department = OwnDepartment();
        var staff = Uuid7.NewUuid7();

        await w.Postings.CreateAsync(scope, Post(staff, department), default);

        await w.Capabilities.RecordAsync(
            scope,
            new RecordCapabilityCommand
            {
                StaffId = staff,
                Name = "Food safety",
                ValidUntil = day.AddDays(400),
            },
            default);

        // The same sixty days the Attention list uses. Two horizons that drifted
        // would put a certificate in one screen's warning and not the other's.
        Assert.Empty(await w.Advisor.AdviseAsync(scope, staff, day, department, default));
    }

    [Fact]
    public async Task Being_already_rostered_that_day_is_advised()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var department = OwnDepartment();
        var staff = Uuid7.NewUuid7();

        await w.Postings.CreateAsync(scope, Post(staff, department), default);

        var shift = await Shift(w, scope);
        await w.Rota.AssignAsync(
            scope,
            new AssignShiftCommand
            {
                StaffId = staff,
                Date = day,
                CatalogueEntryId = shift,
                DepartmentCode = department,
            },
            default);

        var advice = await w.Advisor.AdviseAsync(scope, staff, day, department, default);

        // The rota replaces rather than refuses, so this is what tells a manager
        // the cell they are about to fill is not empty.
        Assert.Equal(AdviceKind.AlreadyRostered, Assert.Single(advice).Kind);
    }

    [Fact]
    public async Task Everything_at_once_is_advised_and_nothing_is_refused()
    {
        var w = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var staff = Uuid7.NewUuid7();
        var type = await LeaveType(w, scope);

        await w.Postings.CreateAsync(scope, Post(staff, OwnDepartment()), default);

        var request = await w.Leave.RaiseAsync(scope, Ask(staff, type, day), default);
        await w.Leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = request.Id, ExpectedVersion = request.Version },
            default);

        await w.Capabilities.RecordAsync(
            scope,
            new RecordCapabilityCommand
            {
                StaffId = staff,
                Name = "Fire warden",
                ValidUntil = day.AddDays(-2),
            },
            default);

        var advice = await w.Advisor.AdviseAsync(scope, staff, day, "BQT", default);

        // On approved leave, outside their department, with a lapsed certificate —
        // and the answer is still three warnings. The manager decides.
        Assert.Equal(
            [AdviceKind.OnApprovedLeave, AdviceKind.NotPostedToDepartment,
             AdviceKind.CertificationExpired],
            advice.Select(a => a.Kind));

        // And the rota still takes the assignment, because the advisor is not
        // wired into it. A manager covering a sick shift at six in the morning is
        // not helped by a validator.
        var shift = await Shift(w, scope);

        await w.Rota.AssignAsync(
            scope,
            new AssignShiftCommand
            {
                StaffId = staff,
                Date = day,
                CatalogueEntryId = shift,
                DepartmentCode = "BQT",
            },
            default);
    }

    private static CreatePostingCommand Post(Guid staff, string department) => new()
    {
        StaffId = staff,
        DepartmentCode = department,
        JobRole = "Attendant",
        EffectiveFrom = new DateOnly(2030, 1, 1),
    };

    private static RaiseLeaveCommand Ask(Guid staff, Guid type, DateOnly day) => new()
    {
        StaffId = staff,
        LeaveTypeId = type,
        From = day,
        To = day,
    };

    private static async Task<Guid> LeaveType(World w, RequestScope scope)
    {
        var type = await w.Types.SetAsync(
            scope,
            new SetLeaveTypeCommand
            {
                Code = $"AL{Interlocked.Increment(ref slot)}",
                Name = "Advice leave",
                AccrualPerMonth = 2m,
            },
            default);

        return type.Id;
    }

    private static async Task<Guid> Shift(World w, RequestScope scope)
    {
        var entry = await w.Shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = "Advice shift",
                ShortCode = $"AS{Interlocked.Increment(ref slot)}",
                Colour = "cyan",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(9, 0),
                    EndsAt = new TimeOnly(17, 0),
                },
                EffectiveFrom = new DateOnly(2030, 1, 1),
            },
            default);

        return entry.Id;
    }

    private sealed record World(
        AssignmentAdvisor Advisor,
        PostingService Postings,
        CapabilityService Capabilities,
        LeaveService Leave,
        LeaveTypeService Types,
        RotaService Rota,
        ShiftCatalogueService Shifts);

    private World Build()
    {
        var db = fixture.Context();
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var clock = TimeProvider.System;

        return new World(
            new AssignmentAdvisor(db, authorizer),
            new PostingService(
                db, authorizer, directory,
                new PostingAnnouncer(new RecordingEventAppender(), directory),
                new TeamService(db, authorizer, directory, clock), clock),
            new CapabilityService(db, authorizer, clock),
            new LeaveService(db, authorizer, new ApproverResolver(db), clock),
            new LeaveTypeService(db, authorizer, directory, clock),
            new RotaService(db, authorizer, clock),
            new ShiftCatalogueService(db, authorizer, clock));
    }
}
