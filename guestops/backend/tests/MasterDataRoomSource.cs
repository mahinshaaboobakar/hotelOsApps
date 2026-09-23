using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Master Data's <c>masterdata.rooms</c> — the columns GuestOps reads.
/// </summary>
/// <remarks>
/// <para>
/// <b>The sibling of <see cref="MasterDataRoomTypeSource"/>, and it exists for
/// the same reason one directory over.</b> This database provisions the
/// <c>guestops</c> schema and nothing else, so a view reading rooms meets
/// <c>42P01</c> before reaching anything a test is about.
/// </para>
/// <para>
/// <b>Six columns, because the read model has six.</b> Not a copy of Master
/// Data's room — that would be the duplicated master data the constitution
/// forbids, in a test — but exactly what
/// <c>Infrastructure/ReadModels/MasterDataRooms.cs</c> projects. A table
/// missing a column the read model selects fails with <c>42703</c>, which is
/// the arrangement wearing the failure of the thing under test.
/// </para>
/// <para>
/// <b>Created by EF, so no DDL is written by hand</b> — the no-native-SQL rule
/// reaches tests, including one seeding another service's schema.
/// </para>
/// </remarks>
/// <param name="connection">The provisioner's connection to the scratch database.</param>
public sealed class MasterDataRoomSource(string connection) : DbContext(
    new DbContextOptionsBuilder<MasterDataRoomSource>()
        .UseSnakeCaseNamingConvention().UseNpgsql(connection).Options)
{
    /// <summary>One room, as GuestOps reads it.</summary>
    public sealed class Row
    {
        public Guid Id { get; set; }

        public Guid PropertyId { get; set; }

        public Guid RoomTypeId { get; set; }

        /// <summary>What the desk calls it — <c>308</c>, <c>PH-2</c>.</summary>
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>ADR 0062's flag. An inactive room is not sellable.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Logical removal, distinct from inactive.</summary>
        public DateTimeOffset? DeletedAt { get; set; }
    }

    public DbSet<Row> Rooms => Set<Row>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Row>().ToTable("rooms", "masterdata").HasKey(row => row.Id);
}
