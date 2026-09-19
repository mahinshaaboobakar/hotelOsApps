using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.RoomCare.Infrastructure.Migrations
{
    /// <summary>
    /// Records that Master Data's <c>staff.display_name</c> is nullable in Room Care's model. The table is Master
    /// Data's, excluded from Room Care's migrations, so nothing runs: this keeps the model snapshot true to the
    /// read model, which EF refuses to migrate without (HH's finding, 2026-09-19).
    /// </summary>
    public partial class MasterDataStaffDisplayNameNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
