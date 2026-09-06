using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.ReadModels;

/// <summary>
/// Master Data's canonical rows, read by this application through the grant.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR 0092 §4 names this path</b> — <i>an application reads canonical
/// Master Data through the platform-standard grant</i> — and install step 4
/// already issues it: <c>GRANT hotelos_masterdata_reader TO
/// hotelos_app_workforce</c>. This file is what uses it.
/// </para>
/// <para>
/// <b>Keyless, and never written.</b> The grant is read-only and these types
/// carry no key, so an <c>Add</c> or an <c>Update</c> against them does not
/// compile into anything EF will accept. That is the no-duplicated-master-data
/// rule expressed as a type rather than as a comment: this application cannot
/// express a write to another schema's rows.
/// </para>
/// <para>
/// <b>Serving is not storing.</b> Nothing here is persisted into
/// <c>workforce</c> and nothing is cached: a department deactivated a moment
/// ago must not still accept a posting, and a stale identity link would
/// announce a tuple for a user who no longer exists.
/// </para>
/// <para>
/// <b>Why this replaced a gRPC client.</b> The client reached Master Data and
/// was refused — <i>`workforce` presented a certificate but no access token</i>
/// — because an application certificate is not <c>TransportPrincipalKind
/// .Service</c>. Neither door was this stream's to open: admitting the
/// application kind token-less would put application capability in Master
/// Data's authenticator, where ADR 0093's authority table puts it in the
/// Kernel; forwarding a bearer token would rebuild the second identity channel
/// ADR 0014 deleted <c>on_behalf_of</c> to remove.
/// </para>
/// </remarks>
public sealed class StaffRow
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public Guid? UserId { get; set; }
    public bool Active { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>One department of one property.</summary>
public sealed class DepartmentRow
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>
/// The link narrowing an organization-scoped person to one property.
/// </summary>
/// <remarks>
/// ADR 0052 split <c>StaffPropertyAssignment</c>: Core kept
/// <c>StaffPropertyScope</c> — staff, property, and nothing else — and the
/// role, the primary flag and the effective dating went to this application.
/// <c>masterdata.staff</c> therefore carries no <c>property_id</c>, and a
/// staff read that ignored this table would answer across the whole
/// organization.
/// </remarks>
public sealed class StaffPropertyScopeRow
{
    public Guid StaffId { get; set; }
    public Guid PropertyId { get; set; }
    public bool Active { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>The property itself.</summary>
public sealed class PropertyRow
{
    public Guid Id { get; set; }
    public string? Country { get; set; }
}

/// <summary>Keyless, over Master Data's tables. Never written.</summary>
public sealed class MasterDataRowConfiguration :
    IEntityTypeConfiguration<StaffRow>,
    IEntityTypeConfiguration<DepartmentRow>,
    IEntityTypeConfiguration<StaffPropertyScopeRow>,
    IEntityTypeConfiguration<PropertyRow>
{
    /// <summary>The schema this application reads and never writes.</summary>
    public const string MasterDataSchema = "masterdata";

    /// <summary>
    /// Mapped, and excluded from this application's migrations.
    /// </summary>
    /// <remarks>
    /// <b>Without the exclusion the next <c>migrations add</c> emits
    /// <c>CreateTable("staff", "masterdata")</c></b> — EF has no way to know
    /// these rows belong to somebody else, so it plans to build them. Applied
    /// against a property that already has Master Data, that migration fails;
    /// applied first, it would create a second, empty <c>masterdata.staff</c>
    /// owned by this application, which is the duplicated-master-data rule
    /// broken by a tool rather than by an author. Context reaches the same
    /// end through <c>ToView</c>, which EF never scaffolds; these are tables,
    /// so the exclusion has to be said out loud.
    /// </remarks>
    private static void Read(EntityTypeBuilder builder, string table)
    {
        builder.HasNoKey();
        builder.ToTable(table, MasterDataSchema, mapping => mapping.ExcludeFromMigrations());
    }

    public void Configure(EntityTypeBuilder<StaffRow> builder) => Read(builder, "staff");

    public void Configure(EntityTypeBuilder<DepartmentRow> builder) => Read(builder, "departments");

    public void Configure(EntityTypeBuilder<StaffPropertyScopeRow> builder) => Read(builder, "staff_property_scopes");

    public void Configure(EntityTypeBuilder<PropertyRow> builder) => Read(builder, "properties");
}
