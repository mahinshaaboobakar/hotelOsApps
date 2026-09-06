using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure.Configuration;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Infrastructure;

/// <summary>
/// The <c>jobs</c> schema — design §2: the lean job, its twelve satellites, the
/// organisation-scoped catalogue and the property's policies. Table shapes
/// live in the three <c>Configuration</c> files; this holds only the sets.
/// </summary>
public class JobsDbContext(DbContextOptions<JobsDbContext> options) : DbContext(options)
{
    /// <summary>The schema the manifest declares; install refuses another.</summary>
    public const string Schema = "jobs";

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<JobAssignment> Assignments => Set<JobAssignment>();

    public DbSet<JobStatusHistory> StatusHistory => Set<JobStatusHistory>();

    public DbSet<JobWorkSession> WorkSessions => Set<JobWorkSession>();

    public DbSet<JobResolution> Resolutions => Set<JobResolution>();

    public DbSet<JobNote> Notes => Set<JobNote>();

    public DbSet<JobAttachment> Attachments => Set<JobAttachment>();

    public DbSet<JobLink> Links => Set<JobLink>();

    public DbSet<JobConcernHistory> ConcernHistory => Set<JobConcernHistory>();

    public DbSet<JobNudge> Nudges => Set<JobNudge>();

    public DbSet<JobReminder> Reminders => Set<JobReminder>();

    public DbSet<JobRating> Ratings => Set<JobRating>();

    public DbSet<PropertyJobSequence> Sequences => Set<PropertyJobSequence>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<ItemAlias> ItemAliases => Set<ItemAlias>();

    public DbSet<Resolution> CatalogueResolutions => Set<Resolution>();

    public DbSet<PropertyItemPolicy> ItemPolicies => Set<PropertyItemPolicy>();

    public DbSet<ConcernPolicy> ConcernPolicies => Set<ConcernPolicy>();

    public DbSet<ConcernPolicyRule> ConcernRules => Set<ConcernPolicyRule>();

    public DbSet<ConcernLadderStep> LadderSteps => Set<ConcernLadderStep>();

    public DbSet<ConcernSubscription> Subscriptions => Set<ConcernSubscription>();

    public DbSet<ServiceHours> ServiceHours => Set<ServiceHours>();

    public DbSet<DepartmentPresence> Presence => Set<DepartmentPresence>();

    public DbSet<ClosingPolicy> ClosingPolicies => Set<ClosingPolicy>();

    public DbSet<HoldPolicy> HoldPolicies => Set<HoldPolicy>();

    /// <summary>What the general manager has granted — design §4.2.</summary>
    public DbSet<JobsManagerGrant> JobsManagerGrants => Set<JobsManagerGrant>();

    /// <summary>Master Data's own tables, read through the install grant — never written.</summary>
    public DbSet<ReadModels.MasterDataProperty> MasterDataProperties =>
        Set<ReadModels.MasterDataProperty>();

    /// <inheritdoc cref="MasterDataProperties" />
    public DbSet<ReadModels.MasterDataDepartment> MasterDataDepartments =>
        Set<ReadModels.MasterDataDepartment>();

    /// <inheritdoc cref="MasterDataProperties" />
    public DbSet<ReadModels.MasterDataLocation> MasterDataLocations =>
        Set<ReadModels.MasterDataLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // The outbox and its publish_state, in this schema, so an event commits
        // with the row that caused it — EVT-Q3, the SDK's appender.
        modelBuilder.AddPlatformEventStore();

        JobTables.Configure(modelBuilder);
        CatalogueTables.Configure(modelBuilder);
        PolicyTables.Configure(modelBuilder);
        GrantTables.Configure(modelBuilder);

        // Master Data's tables, keyless and excluded from this application's
        // migrations — ADR 0092 §4's install grant, which is how an application
        // reads master data. It is not a platform service and has no token for
        // the wire (ADR 0093 §PKG-Q8), so the grant is the path, not a fallback.
        modelBuilder.ApplyConfiguration(new ReadModels.MasterDataPropertyConfiguration());
        modelBuilder.ApplyConfiguration(new ReadModels.MasterDataDepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new ReadModels.MasterDataLocationConfiguration());
    }
}
