using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.ReadModels;

/// <summary>
/// What a room type is <b>called</b>, for a screen to render.
/// </summary>
/// <remarks>
/// <para>
/// <b>Its own file because it is its own table.</b> The room's number could not
/// be a second read model over <c>masterdata.rooms</c> — EF Core allows one
/// keyless entity per table and refuses a second with no linking relationship —
/// so it joined <see cref="MasterDataRoom"/> instead. <c>room_types</c> is
/// mapped by nothing else, so this one stands alone.
/// </para>
/// <para>
/// <b>Keyless, read-only, excluded from migrations</b> — the same terms as its
/// neighbour. The tables are Master Data's; this application holds
/// <c>SELECT</c> on that schema and no DDL anywhere near it, which is the
/// grant, not a convention.
/// </para>
/// <para>
/// <b>Why not a gRPC call per screen.</b> Master Data's own
/// <c>ListRooms</c>would answer this, and one call per row — or a call whose
/// result this application then had to page in step with its own — is a join
/// done over the network. Reading the schema this application is granted
/// <c>SELECT</c> on is what the grant is for; CLAUDE.md's rule is that an
/// application may <i>read</i> master data and may not <i>duplicate</i> it, and
/// nothing here is stored.
/// </para>
/// </remarks>
public sealed class MasterDataRoomTypeName
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>Keyless, over <c>masterdata.room_types</c>. Never written.</summary>
public sealed class MasterDataRoomTypeNameConfiguration
    : IEntityTypeConfiguration<MasterDataRoomTypeName>
{
    public void Configure(EntityTypeBuilder<MasterDataRoomTypeName> builder)
    {
        builder.HasNoKey();
        builder.ToTable("room_types", "masterdata", table => table.ExcludeFromMigrations());
    }
}
