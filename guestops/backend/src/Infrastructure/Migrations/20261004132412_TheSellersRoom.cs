using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.GuestOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TheSellersRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "room_id",
                schema: "guestops",
                table: "stop_sells",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "room_id",
                schema: "guestops",
                table: "stop_sells");
        }
    }
}
