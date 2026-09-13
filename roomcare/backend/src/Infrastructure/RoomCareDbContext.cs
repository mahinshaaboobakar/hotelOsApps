using HotelOS.Platform;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure.Configuration;
using HotelOS.RoomCare.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Infrastructure;

/// <summary>The <c>roomcare</c> schema — every row Room Care owns, and the Master Data it reads.</summary>
public class RoomCareDbContext(DbContextOptions<RoomCareDbContext> options) : DbContext(options)
{
    /// <summary>The schema the manifest declares; the migrations history lives in it too.</summary>
    public const string Schema = "roomcare";

    public DbSet<RoomState> RoomStates => Set<RoomState>();

    public DbSet<RoomObservation> Observations => Set<RoomObservation>();

    public DbSet<RoomTask> Tasks => Set<RoomTask>();

    public DbSet<TaskPhase> Phases => Set<TaskPhase>();

    public DbSet<TaskAttempt> Attempts => Set<TaskAttempt>();

    public DbSet<TaskAssignment> Assignments => Set<TaskAssignment>();

    public DbSet<TaskWorkSession> WorkSessions => Set<TaskWorkSession>();

    public DbSet<TaskHistory> History => Set<TaskHistory>();

    public DbSet<TaskJobTouch> JobTouches => Set<TaskJobTouch>();

    public DbSet<TaskIssue> Issues => Set<TaskIssue>();

    public DbSet<RoomSupervision> Supervision => Set<RoomSupervision>();

    public DbSet<Restock> Restocks => Set<Restock>();

    public DbSet<PrepareRun> PrepareRuns => Set<PrepareRun>();

    public DbSet<DeepClean> DeepCleans => Set<DeepClean>();

    public DbSet<PostingSeen> Postings => Set<PostingSeen>();

    public DbSet<ShiftPresence> Presence => Set<ShiftPresence>();

    public DbSet<RoomCareManagerGrant> ManagerGrants => Set<RoomCareManagerGrant>();

    public DbSet<ServiceWindow> Windows => Set<ServiceWindow>();

    public DbSet<ServiceStandard> Standards => Set<ServiceStandard>();

    public DbSet<PropertyPolicy> Policies => Set<PropertyPolicy>();

    public DbSet<AreaSchedule> AreaSchedules => Set<AreaSchedule>();

    public DbSet<DeepCleanPlan> DeepCleanPlans => Set<DeepCleanPlan>();

    public DbSet<RoomZoneAssignment> ZoneAssignments => Set<RoomZoneAssignment>();

    public DbSet<MasterDataProperty> MasterDataProperties => Set<MasterDataProperty>();

    public DbSet<MasterDataRoom> MasterDataRooms => Set<MasterDataRoom>();

    public DbSet<MasterDataRoomType> MasterDataRoomTypes => Set<MasterDataRoomType>();

    public DbSet<MasterDataZone> MasterDataZones => Set<MasterDataZone>();

    public DbSet<MasterDataLocation> MasterDataLocations => Set<MasterDataLocation>();

    public DbSet<MasterDataDepartment> MasterDataDepartments => Set<MasterDataDepartment>();

    public DbSet<MasterDataStaff> MasterDataStaff => Set<MasterDataStaff>();

    /// <summary>Every instant is stored as UTC, whatever offset it was computed in.</summary>
    /// <remarks>
    /// Room Care computes instants in a property's own zone — a window's close,
    /// an arrival expected at 15:00 — and Npgsql refuses a non-zero offset for
    /// <c>timestamptz</c>. Converting at the one door every write passes through
    /// means no service has to remember to, and none can forget.
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcInstant>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcInstant>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // The outbox and its publish state, so an event commits with the row
        // that caused it — the SDK's appender, one transaction (EVT-Q3).
        modelBuilder.AddPlatformEventStore();

        RoomTables.Configure(modelBuilder);
        TaskTables.Configure(modelBuilder);
        DayTables.Configure(modelBuilder);
        StandardTables.Configure(modelBuilder);
        MasterDataHouse.Configure(modelBuilder);
    }
}
