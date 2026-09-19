using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Tests;

/// <summary>
/// Master Data's <c>masterdata.staff</c>, the four columns Room Care reads, mapped as Master Data maps them —
/// <c>display_name</c> nullable, as <c>People.cs</c> declares <c>string? DisplayName</c>. A test-only stand-in for
/// the platform's table, not Master Data itself: it exists so a test can hold a row Room Care's read model must
/// survive reading, which <see cref="HouseDouble"/> cannot.
/// </summary>
public sealed class MasterDataStaffSource(string connection) : DbContext(
    new DbContextOptionsBuilder<MasterDataStaffSource>().UseSnakeCaseNamingConvention().UseNpgsql(connection).Options)
{
    /// <summary>One staff row.</summary>
    public sealed class Row
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        public string? DisplayName { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
    }

    public DbSet<Row> Staff => Set<Row>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Row>().ToTable("staff", "masterdata").HasKey(r => r.Id);
}
