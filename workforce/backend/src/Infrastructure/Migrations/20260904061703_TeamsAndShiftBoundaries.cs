using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeamsAndShiftBoundaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_boundaries",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    catalogue_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    on_now_after = table.Column<int>(type: "integer", nullable: false),
                    announced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_boundaries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_on = table.Column<DateOnly>(type: "date", nullable: false),
                    left_on = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_members", x => x.id);
                    table.CheckConstraint("ck_team_members__window_ordered", "left_on IS NULL OR left_on >= joined_on");
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                    table.CheckConstraint("ck_teams__department_code_present", "length(btrim(department_code)) > 0");
                    table.CheckConstraint("ck_teams__name_present", "length(btrim(name)) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_boundaries__property_date",
                schema: "workforce",
                table: "shift_boundaries",
                columns: new[] { "property_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "uq_shift_boundaries__announced_once",
                schema: "workforce",
                table: "shift_boundaries",
                columns: new[] { "property_id", "department_code", "catalogue_entry_id", "business_date", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_team_members__property_staff",
                schema: "workforce",
                table: "team_members",
                columns: new[] { "property_id", "staff_id" });

            migrationBuilder.CreateIndex(
                name: "ix_team_members__property_team",
                schema: "workforce",
                table: "team_members",
                columns: new[] { "property_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "uq_team_members__team_staff_live",
                schema: "workforce",
                table: "team_members",
                columns: new[] { "team_id", "staff_id" },
                unique: true,
                filter: "left_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_teams__property_department",
                schema: "workforce",
                table: "teams",
                columns: new[] { "property_id", "department_code" });

            migrationBuilder.CreateIndex(
                name: "uq_teams__property_department_name",
                schema: "workforce",
                table: "teams",
                columns: new[] { "property_id", "department_code", "name" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shift_boundaries",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "team_members",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "workforce");
        }
    }
}
