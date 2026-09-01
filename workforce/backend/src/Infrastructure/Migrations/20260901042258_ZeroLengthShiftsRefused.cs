using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ZeroLengthShiftsRefused : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_shift_hours__second_span_not_empty",
                schema: "workforce",
                table: "shift_hours",
                sql: "second_starts_at IS NULL OR second_starts_at <> second_ends_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shift_hours__span_not_empty",
                schema: "workforce",
                table: "shift_hours",
                sql: "starts_at IS NULL OR starts_at <> ends_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_shift_hours__second_span_not_empty",
                schema: "workforce",
                table: "shift_hours");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shift_hours__span_not_empty",
                schema: "workforce",
                table: "shift_hours");
        }
    }
}
