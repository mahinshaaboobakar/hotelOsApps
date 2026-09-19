using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Jobs.Infrastructure.ReadModels;

/// <summary>
/// One row of <c>masterdata.staff</c> — a person's display name by login.
/// </summary>
/// <remarks>
/// <para>
/// <b>Room Care's read model</b> (<c>roomcare/…/ReadModels/MasterDataHouse.cs</c>):
/// the architect directed on 2026-09-19 that the bar's name be read the way Room
/// Care reads it, and two applications reading one table two ways would name one
/// person twice.
/// </para>
/// <para>
/// <b>One difference, and it is Master Data's type:</b> <c>display_name</c> is
/// nullable there (<c>Domain/People.cs</c>, <c>string? DisplayName</c>), and
/// Room Care declares it non-nullable. A staff row with no display name then has
/// nothing to materialise into. Nullable here, and a null reads as no name —
/// which is what Master Data's own wire does with it (<c>?? string.Empty</c>).
/// </para>
/// <para>
/// <b>Four columns.</b> The name is read at answer time and never kept, for the
/// reason <see cref="MasterDataProperty"/> gives: reading master data is allowed
/// and copying it is not. Keyless and excluded from migrations for the same
/// reason as its neighbours.
/// </para>
/// </remarks>
public sealed class MasterDataStaff
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? DisplayName { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Keyless, over <c>masterdata.staff</c>. Never written.</summary>
public sealed class MasterDataStaffConfiguration : IEntityTypeConfiguration<MasterDataStaff>
{
    public void Configure(EntityTypeBuilder<MasterDataStaff> builder)
    {
        builder.HasNoKey();
        builder.ToTable("staff", "masterdata", table => table.ExcludeFromMigrations());
    }
}
