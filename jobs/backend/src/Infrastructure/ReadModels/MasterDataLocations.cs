using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Jobs.Infrastructure.ReadModels;

/// <summary>
/// One row of <c>masterdata.locations</c> — where a job is.
/// </summary>
/// <remarks>
/// <para>
/// Four columns: the id a job stores, the property that owns it, the name a
/// screen renders, and <c>deleted_at</c>, which is here because it changes the
/// answer. A location that has been removed is not a place a job may be raised
/// against, and the gRPC call this replaced expressed that by answering
/// <c>NotFound</c>; reading the table without the column would silently accept
/// a deleted node and put a job somewhere that no longer exists.
/// </para>
/// <para>
/// Keyless and excluded from migrations, for the reason
/// <see cref="MasterDataProperty"/> states — including why this is read through
/// ADR 0092 §4's install grant rather than over the wire.
/// </para>
/// </remarks>
public sealed class MasterDataLocation
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Keyless, over <c>masterdata.locations</c>. Never written.</summary>
public sealed class MasterDataLocationConfiguration : IEntityTypeConfiguration<MasterDataLocation>
{
    public void Configure(EntityTypeBuilder<MasterDataLocation> builder)
    {
        builder.HasNoKey();
        builder.ToTable("locations", "masterdata", table => table.ExcludeFromMigrations());
    }
}
