using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Jobs.Infrastructure.ReadModels;

/// <summary>
/// One row of <c>masterdata.properties</c>, as this application reads it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read through the install grant, not over the wire.</b> This was a
/// <c>MasterDataServiceClient</c> until 2026-09-06, and it would have been
/// refused the first time a flow reached it: an application presents a
/// certificate and no access token, and <b>an application is not a platform
/// service</b> — ADR 0093 §PKG-Q8. The ruled read path is ADR 0092 §4's install
/// grant, and <c>hotelos_masterdata_reader</c> is already held by
/// <c>hotelos_app_jobs</c>. Workforce met the same refusal on the same day.
/// </para>
/// <para>
/// <b>Five columns, and only five.</b> Everything a job needs of a property: the
/// organisation it belongs to, the code that prefixes a job number, the name the
/// bar's identity clause reads (page 64 §3, as Room Care reads it), and the
/// timezone every displayed instant is rendered in. A read model that mirrored
/// the property would drift into being a copy of master data, which the
/// constitution forbids — this application may <i>read</i> master data and may
/// never <i>duplicate</i> it, and nothing here is stored.
/// </para>
/// <para>
/// <b>Keyless, and excluded from this application's migrations.</b> The table is
/// Master Data's; Jobs holds <c>SELECT</c> on that schema and no DDL anywhere
/// near it. Generating it from here would put two components in charge of one
/// table, and the second one would lose.
/// </para>
/// </remarks>
public sealed class MasterDataProperty
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Timezone { get; set; } = string.Empty;
}

/// <summary>Keyless, over <c>masterdata.properties</c>. Never written.</summary>
public sealed class MasterDataPropertyConfiguration : IEntityTypeConfiguration<MasterDataProperty>
{
    public void Configure(EntityTypeBuilder<MasterDataProperty> builder)
    {
        builder.HasNoKey();
        builder.ToTable("properties", "masterdata", table => table.ExcludeFromMigrations());
    }
}
