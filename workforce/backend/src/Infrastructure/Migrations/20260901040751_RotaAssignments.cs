using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RotaAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "policies",
                schema: "workforce",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    overtime_daily_hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    overtime_weekly_hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_policies", x => x.property_id);
                    table.CheckConstraint("ck_policies__overtime_daily_positive", "overtime_daily_hours IS NULL OR overtime_daily_hours > 0");
                    table.CheckConstraint("ck_policies__overtime_weekly_positive", "overtime_weekly_hours IS NULL OR overtime_weekly_hours > 0");
                });

            migrationBuilder.CreateTable(
                name: "shift_assignments",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    catalogue_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    override_starts_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    override_ends_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_assignments", x => x.id);
                    table.CheckConstraint("ck_shift_assignments__department_present", "length(btrim(department_code)) > 0");
                    table.CheckConstraint("ck_shift_assignments__override_complete", "(override_starts_at IS NULL) = (override_ends_at IS NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignments__property_date_department",
                schema: "workforce",
                table: "shift_assignments",
                columns: new[] { "property_id", "date", "department_code" });

            migrationBuilder.CreateIndex(
                name: "uq_shift_assignments__property_staff_date",
                schema: "workforce",
                table: "shift_assignments",
                columns: new[] { "property_id", "staff_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "policies",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "shift_assignments",
                schema: "workforce");
        }
    }
}
