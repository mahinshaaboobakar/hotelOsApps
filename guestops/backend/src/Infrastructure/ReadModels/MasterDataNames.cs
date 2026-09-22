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

    /// <summary>How many the rate includes — Master Data's, not this application's.</summary>
    /// <remarks>
    /// <para>
    /// <b>Projected from 2026-09-22, ADR 0215.</b> This class read <c>Id</c> and
    /// <c>Name</c> and nothing else, which is why New booking could not tell
    /// whether a room type sleeps the party in front of the desk. The columns
    /// have been in <c>masterdata.room_types</c> all along — six of them, plus
    /// a bed configuration — and ADR 0214, which claimed the platform held no
    /// such attribute, is withdrawn. Nothing is added to Master Data.
    /// </para>
    /// <para>
    /// <b>A stored 2 is the domain's default, not "unknown"</b>
    /// (<c>Catalogue.cs:93</c>). This projection carries the number as stored
    /// and draws no inference from it: a property that has never opened the
    /// room type and one that set two guests deliberately are indistinguishable
    /// here, and deciding what that means is the owner's, not a read model's.
    /// </para>
    /// </remarks>
    public int BaseOccupancy { get; set; }

    /// <summary>The most the room type sleeps, extra beds included.</summary>
    /// <remarks>
    /// <b>The type's, and a room's can differ.</b>
    /// <c>masterdata.rooms.max_occupancy</c> is a nullable override that
    /// Context's room composer already resolves, so an individual room may take
    /// more or fewer than its type. Choosing a TYPE is what this serves; the
    /// effective number for an assigned ROOM comes from Context, and this
    /// projection must not be joined to a room to answer that.
    /// </remarks>
    public int MaxOccupancy { get; set; }

    public int MaxAdults { get; set; }

    public int MaxChildren { get; set; }

    public bool ExtraBedAllowed { get; set; }

    public int MaxExtraBeds { get; set; }
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
