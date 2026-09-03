using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.GuestOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TheFeedsPulse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbound_feed_marks",
                schema: "guestops",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_fact_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbound_feed_marks", x => new { x.property_id, x.integration_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbound_feed_marks",
                schema: "guestops");
        }
    }
}
