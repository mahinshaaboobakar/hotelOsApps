using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Master Data's <c>masterdata.room_types</c> — the two columns GuestOps reads,
/// mapped as Master Data maps them.
/// </summary>
/// <remarks>
/// <para>
/// <b>A test-only stand-in for the platform's table, and not Master Data.</b>
/// It exists so a view that reads room-type names can be exercised against a
/// real table rather than against a double that could never hold a row the read
/// model refuses. Room Care's <c>MasterDataStaffSource</c> is the same pattern
/// for <c>masterdata.staff</c>, and this follows it rather than inventing a
/// second shape.
/// </para>
/// <para>
/// <b>The shape is taken from Master Data's own declaration</b>
/// (<c>services/masterdata-service/src/Domain/Catalogue.cs</c>:
/// <c>RoomType.Name</c> is non-nullable, and the id is the master entity's), so
/// a column that diverges there fails here rather than silently reading as
/// something else. It is deliberately NOT the whole table: a stand-in carrying
/// every column would be a second declaration of Master Data's schema, which is
/// the thing this repository keeps two of and regrets.
/// </para>
/// <para>
/// <b>Created by EF, so no DDL is written by hand</b> — the no-native-SQL rule
/// reaches tests, including one seeding another service's schema. The grant
/// that follows is privilege plumbing, which no expression tree can express and
/// which <see cref="GuestOpsScratch"/> already issues for the roles an install
/// hands out.
/// </para>
/// </remarks>
public sealed class MasterDataRoomTypeSource(string connection) : DbContext(
    new DbContextOptionsBuilder<MasterDataRoomTypeSource>()
        .UseSnakeCaseNamingConvention().UseNpgsql(connection).Options)
{
    /// <summary>One room type, as GuestOps reads it.</summary>
    /// <remarks>
    /// <b>The occupancy columns joined it on 2026-09-22</b>, when the read
    /// model widened to project them (ADR 0215). They are Master Data's own
    /// (<c>Catalogue.cs:93-99</c>) and are here because a table missing a
    /// column the read model projects fails with <c>42703</c> — the arrangement
    /// wearing the failure of the thing under test, which this file has already
    /// produced once.
    /// </remarks>
    public sealed class Row
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int BaseOccupancy { get; set; }

        public int MaxOccupancy { get; set; }

        public int MaxAdults { get; set; }

        public int MaxChildren { get; set; }

        public bool ExtraBedAllowed { get; set; }

        public int MaxExtraBeds { get; set; }
    }

    public DbSet<Row> RoomTypes => Set<Row>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Row>().ToTable("room_types", "masterdata").HasKey(row => row.Id);
}
