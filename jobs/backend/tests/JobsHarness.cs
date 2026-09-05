using HotelOS.Jobs.Application.Assignment;
using HotelOS.Jobs.Application.Cancellation;
using HotelOS.Jobs.Application.Catalogue;
using HotelOS.Jobs.Application.Completion;
using HotelOS.Jobs.Application.Concerns;
using HotelOS.Jobs.Application.Course;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Application.Policies;
using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Jobs.Application.Work;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using HotelOS.Platform.TestSupport;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// Every service wired against one context, the recording doubles and a
/// controllable clock — plus the Marina Bay catalogue the walkthrough's frames
/// use: Engineering › Air conditioning › Not cooling, Housekeeping › Bottle of
/// water › Still water, and their resolutions.
/// </summary>
public sealed class JobsHarness
{
    public JobsHarness(JobsFixture fixture, DateTimeOffset? now = null)
    {
        Fixture = fixture;
        Db = fixture.Context();
        Clock = new FrozenClock(now ?? new DateTimeOffset(2026, 9, 2, 9, 31, 0, TimeSpan.Zero));
        Authorizer = new RecordingAuthorizer();
        Events = new RecordingEventAppender();
        Directory = new DirectoryDouble();
        Directory.Organization = OrganizationId;
        Records = new JobRecords(Db, Clock);
        Announcer = new JobAnnouncer(Events);
        Assignment = new AssignmentService(Db, Authorizer, Directory, Announcer, Records);
        Jobs = new JobService(
            Db, Authorizer, Directory, new JobPolicyResolver(Db), new JobNumbering(Db, Directory), Assignment, Announcer, Records);
        Work = new WorkSessionService(Db, Announcer, Records);
        Completion = new CompletionService(Db, Authorizer, Announcer, Records);
        Cancellation = new CancellationService(Db, Authorizer, Announcer, Records);
        Course = new CourseService(Db, Authorizer, Announcer, Records);
        Queries = new JobQueries(Db, Authorizer, Clock);
        Catalogue = new CatalogueService(Db, Authorizer, Clock);
        PropertyCatalogue = new PropertyCatalogueService(Db, Authorizer, Clock);
        Policies = new ConcernPolicyService(Db, Authorizer, Clock);
        Presence = new PresenceService(Db, Authorizer, Clock);
        Sweep = new ConcernSweep(Db, Directory, new Nudger(Db, Directory), Announcer, Clock);
        AutoClose = new AutoClose(Db, Announcer, Records);
        DayStart = new DayStart(Db, Directory, Announcer, Records);
    }

    public JobsFixture Fixture { get; }

    /// <summary>A property of this harness's own — tests share one database, never one property.</summary>
    public Guid PropertyId { get; } = Guid.CreateVersion7();

    public JobsDbContext Db { get; }

    public FrozenClock Clock { get; }

    public RecordingAuthorizer Authorizer { get; }

    public RecordingEventAppender Events { get; }

    public DirectoryDouble Directory { get; }

    public JobRecords Records { get; }

    public JobAnnouncer Announcer { get; }

    public JobService Jobs { get; }

    public AssignmentService Assignment { get; }

    public WorkSessionService Work { get; }

    public CompletionService Completion { get; }

    public CancellationService Cancellation { get; }

    public CourseService Course { get; }

    public JobQueries Queries { get; }

    public CatalogueService Catalogue { get; }

    public PropertyCatalogueService PropertyCatalogue { get; }

    public ConcernPolicyService Policies { get; }

    public PresenceService Presence { get; }

    public ConcernSweep Sweep { get; }

    public AutoClose AutoClose { get; }

    public DayStart DayStart { get; }

    public Guid OrganizationId { get; } = Guid.CreateVersion7();

    public Item NotCooling { get; private set; } = null!;

    public Item StillWater { get; private set; } = null!;

    public Resolution RefrigerantToppedUp { get; private set; } = null!;

    public Resolution Other { get; private set; } = null!;

    public Guid Room1204 { get; } = Guid.CreateVersion7();

    /// <summary>The scope the sweep's three passes run in — the tick's, not a user's.</summary>
    /// <remarks>
    /// <c>RequestScope.ForBackgroundWork</c>, exactly as <see cref="ConcernActivities"/>
    /// mints it per property per tick: Jobs' own service identity, this
    /// property, and no user — nobody asked, so nobody is recorded as having
    /// asked (<c>WF-Q11</c> (8)).
    /// </remarks>
    public RequestScope Sweeping => RequestScope.ForBackgroundWork(new ServiceIdentity("jobs"), PropertyId);

    /// <summary>A user scope at the fixture's property, with the organisation for curating.</summary>
    public RequestScope Scope(Guid? user = null) => new()
    {
        Caller = CallerKind.User, PropertyId = PropertyId, OrganizationId = OrganizationId, UserId = user ?? Guid.CreateVersion7(),
    };

    /// <summary>Seed the two-department catalogue the frames draw.</summary>
    public async Task SeedCatalogueAsync()
    {
        var now = Clock.GetUtcNow();
        var ac = new Category { Id = Guid.CreateVersion7(), OrganizationId = OrganizationId, Code = "AC", Name = "Air conditioning", DepartmentCode = "ENG", CreatedAt = now, UpdatedAt = now, Version = 1 };
        var water = new Category { Id = Guid.CreateVersion7(), OrganizationId = OrganizationId, Code = "WATER", Name = "Bottle of water", DepartmentCode = "HK", CreatedAt = now, UpdatedAt = now, Version = 1 };
        NotCooling = new Item { Id = Guid.CreateVersion7(), OrganizationId = OrganizationId, CategoryId = ac.Id, Code = "AC_NOT_COOLING", Name = "Not cooling", DefaultPriority = Priority.P2, DueWithinMinutes = 40, CreatedAt = now, UpdatedAt = now, Version = 1 };
        StillWater = new Item { Id = Guid.CreateVersion7(), OrganizationId = OrganizationId, CategoryId = water.Id, Code = "WATER_STILL", Name = "Still water", DefaultPriority = Priority.P3, DueWithinMinutes = 10, CreatedAt = now, UpdatedAt = now, Version = 1 };
        RefrigerantToppedUp = new Resolution { Id = Guid.CreateVersion7(), OrganizationId = OrganizationId, CategoryId = ac.Id, Name = "Refrigerant topped up" };
        Other = new Resolution { Id = Guid.CreateVersion7(), OrganizationId = OrganizationId, Name = "Other", NoteRequired = true };
        Db.Categories.AddRange(ac, water);
        Db.Items.AddRange(NotCooling, StillWater);
        Db.ItemAliases.Add(new ItemAlias { Id = Guid.CreateVersion7(), ItemId = NotCooling.Id, Alias = "AC not working" });
        Db.CatalogueResolutions.AddRange(RefrigerantToppedUp, Other);
        await Db.SaveChangesAsync();
    }

    /// <summary>The Engineering policy of settings frame 1: P1 40 min at 75 %, stuck 8 / 15, the four-step ladder.</summary>
    public async Task<ConcernPolicy> SeedEngineeringPolicyAsync()
    {
        var policy = new ConcernPolicy { Id = Guid.CreateVersion7(), PropertyId = PropertyId, Name = "Engineering", DepartmentCode = "ENG", CreatedAt = Clock.GetUtcNow(), UpdatedAt = Clock.GetUtcNow(), Version = 1 };
        Db.ConcernPolicies.Add(policy);
        Db.ConcernRules.Add(new ConcernPolicyRule { Id = Guid.CreateVersion7(), PolicyId = policy.Id, Priority = Priority.P1, DueWithinMinutes = 40, AtRiskPercent = 75, NotAcceptedMinutes = 8, NoSessionMinutes = 15, ManagerAtRisk = true, RunsOutsidePresence = true });
        Db.ConcernRules.Add(new ConcernPolicyRule { Id = Guid.CreateVersion7(), PolicyId = policy.Id, Priority = Priority.P2, DueWithinMinutes = 120, AtRiskPercent = 75, NotAcceptedMinutes = 20, NoSessionMinutes = 45 });
        Db.ConcernRules.Add(new ConcernPolicyRule { Id = Guid.CreateVersion7(), PolicyId = policy.Id, Priority = Priority.P3, AtRiskPercent = 80, NotAcceptedMinutes = 60 });
        foreach (var (step, role, trigger, delay) in new[]
        {
            (1, LadderRole.Assignee, Concern.AtRisk, 0), (2, LadderRole.Supervisor, Concern.Breached, 0),
            (3, LadderRole.Manager, Concern.Breached, 15), (4, LadderRole.JobsManager, Concern.Breached, 45),
        })
        {
            Db.LadderSteps.Add(new ConcernLadderStep { Id = Guid.CreateVersion7(), PolicyId = policy.Id, Priority = Priority.P1, StepNo = step, Role = role, Trigger = trigger, DelayMinutes = delay });
        }

        await Db.SaveChangesAsync();
        return policy;
    }

    /// <summary>Raise Not cooling in Room 1204 — a guest of stay 7F2A, via the guest app, as the flow's P1.</summary>
    public Task<Job> RaiseNotCoolingAsync(RequestScope scope, Guid? stay = null, Guid? assignTo = null, DateOnly? scheduledFor = null) =>
        Jobs.RaiseAsync(scope, new RaiseJobCommand
        {
            ItemId = NotCooling.Id, LocationId = Room1204, Summary = "Room feels warm since noon",
            FlowPriority = Priority.P1, RaisedVia = RaisedVia.GuestApp, RaisedKind = RaisedKind.Guest,
            StayId = stay ?? Guid.CreateVersion7(), AssignToUserId = assignTo, ScheduledFor = scheduledFor,
        }, default);
}

/// <summary>A clock a test moves by hand.</summary>
public sealed class FrozenClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;

    public void Set(DateTimeOffset to) => _now = to;
}
