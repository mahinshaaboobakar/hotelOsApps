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
/// The department's name is not here — a screen that shows a department shows
/// the code it was configured with, and a name would be the first column of a
/// copy of master data.
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
