using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Jobs.Infrastructure.ReadModels;

/// <summary>
/// One row of <c>masterdata.departments</c> — the code a job is routed by.
/// </summary>
/// <remarks>
/// <para>
/// Three columns: the id a job stores, the property that owns the row, and the
/// code every policy, ladder and subscription in this design is written in.
/// </para>
/// <para>
/// <b>The name is still not mapped — and the reason recorded here is no longer
/// the reason.</b> Until 2026-09-30 this paragraph ended:
/// </para>
/// <para>
/// <i>"The department's name is not here — a screen that shows a department
/// shows the code it was configured with, and a name would be the first column
/// of a copy of master data."</i>
/// </para>
/// <para>
/// <b>ADR 0342 §1 inverts its first half.</b> The ruling gives the two separate
/// jobs — <i>code identifies; name describes</i> — and sets a department's
/// display as <c>Housekeeping · HK</c>: the name primary, the code secondary,
/// and <i>"do not hide the department code"</i>. So a screen showing the code
/// alone is now the defect that sentence was written to justify, and this
/// application has one — the selector at <c>settings/tabs.ts:84</c>, whose
/// value and label are both <c>p.department</c>.
/// </para>
/// <para>
/// <b>Its second half was asked, and withdrawn unanswered.</b> ADR 0342 also
/// rules that there is one authoritative Master Data department projection and
/// refuses <i>"a second department vocabulary or application-local lookup"</i>,
/// so whether reading one further column of a table this application already
/// reads is that lookup — or is this grant simply being used — went up as
/// <c>CORE-Q43b</c>. <b>Withdrawn 2026-09-30</b> (approved ruling, relayed the
/// same day): the department's name is to arrive through Context's capability
/// rather than through this table, so the question stops arising. <b>The column
/// is not mapped, and this is not a file waiting for permission to map it.</b>
/// The facts the question turned on are kept because they are facts, and the
/// next person to reach for the name will rediscover them: the read path is ADR
/// 0092 §4's install grant, which is the database role
/// <c>hotelos_masterdata_reader</c> and not a column list; the table carries
/// <c>name</c> beside <c>code</c>, both required; and
/// <see cref="MasterDataProperty"/> already maps a master-data <c>name</c> for
/// the bar's identity clause, storing nothing. <b>None of that is permission —
/// the ruling routes the name elsewhere.</b>
/// </para>
/// <para>
/// <b>This read model is staged for replacement, not deletion.</b> Approved
/// ruling, 2026-09-30: Jobs' four master-data read models — this one,
/// <see cref="MasterDataLocation"/>, <see cref="MasterDataProperty"/> and
/// <see cref="MasterDataStaff"/> — stay until Context's capability serves what
/// they serve, <i>"so that removing them does not turn debt into an outage"</i>.
/// Deleting one before that is the outage; the swap is what closes this file,
/// and it is the swap rather than a deletion.
/// </para>
/// <para>
/// <b>The property column is the filter, always.</b> This schema holds every
/// property's departments; a lookup by code alone would answer with another
/// hotel's department, which is a cross-property leak rather than a bug in a
/// screen. Every query here names <c>PropertyId</c>.
/// </para>
/// <para>
/// Keyless and excluded from migrations, for the reason its neighbour states —
/// see <see cref="MasterDataProperty"/>, which also records why this is read
/// through ADR 0092 §4's install grant rather than over the wire.
/// </para>
/// </remarks>
public sealed class MasterDataDepartment
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Code { get; set; } = string.Empty;
}

/// <summary>Keyless, over <c>masterdata.departments</c>. Never written.</summary>
public sealed class MasterDataDepartmentConfiguration : IEntityTypeConfiguration<MasterDataDepartment>
{
    public void Configure(EntityTypeBuilder<MasterDataDepartment> builder)
    {
        builder.HasNoKey();
        builder.ToTable("departments", "masterdata", table => table.ExcludeFromMigrations());
    }
}
