using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.ReadModels;

/// <summary>
/// What a room and a room type are <b>called</b>, for a screen to render.
/// </summary>
/// <remarks>
/// <para>
/// <b>A second read model rather than two more columns on the first.</b>
/// <see cref="MasterDataRoom"/> says outright that it carries "what
/// availability needs to count and nothing a screen might be tempted to
/// render", and that is a decision worth keeping true: counting and naming are
/// different jobs, and a model that did both would be the one that keeps
/// growing until it is a copy of master data.
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
public sealed class MasterDataRoomName
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>What the desk calls it — <c>214</c>, <c>1204</c>, <c>PH-2</c>.</summary>
    public string RoomNumber { get; set; } = string.Empty;
}

/// <summary>Keyless, over <c>masterdata.rooms</c>. Never written.</summary>
public sealed class MasterDataRoomNameConfiguration
    : IEntityTypeConfiguration<MasterDataRoomName>
{
    public void Configure(EntityTypeBuilder<MasterDataRoomName> builder)
    {
        builder.HasNoKey();
        builder.ToTable("rooms", "masterdata", table => table.ExcludeFromMigrations());
    }
}

/// <summary>What a room type is called — <c>Deluxe King</c>.</summary>
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
