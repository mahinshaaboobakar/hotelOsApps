using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Attendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    in_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    out_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    source = table.Column<int>(type: "integer", nullable: false),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance", x => x.id);
                    table.CheckConstraint("ck_attendance__out_needs_in", "out_at IS NULL OR in_at IS NOT NULL");
                    table.CheckConstraint("ck_attendance__provenance", "(source = 0 AND recorded_by_user_id IS NOT NULL) OR (source <> 0 AND external_reference IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance__property_date",
                schema: "workforce",
                table: "attendance",
                columns: new[] { "property_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "uq_attendance__property_staff_date",
                schema: "workforce",
                table: "attendance",
                columns: new[] { "property_id", "staff_id", "business_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance",
                schema: "workforce");
        }
    }
}
