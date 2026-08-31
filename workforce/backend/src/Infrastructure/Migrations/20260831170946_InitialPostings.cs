using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workforce");

            migrationBuilder.CreateTable(
                name: "postings",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    job_role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_department_head = table.Column<bool>(type: "boolean", nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporting_manager_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postings", x => x.id);
                    table.CheckConstraint("ck_postings__department_code_present", "length(btrim(department_code)) > 0");
                    table.CheckConstraint("ck_postings__window_ordered", "effective_to IS NULL OR effective_to >= effective_from");
                });

            migrationBuilder.CreateIndex(
                name: "ix_postings__property_department",
                schema: "workforce",
                table: "postings",
                columns: new[] { "property_id", "department_code" });

            migrationBuilder.CreateIndex(
                name: "ix_postings__property_staff",
                schema: "workforce",
                table: "postings",
                columns: new[] { "property_id", "staff_id" });

            migrationBuilder.CreateIndex(
                name: "ix_postings__property_zone",
                schema: "workforce",
                table: "postings",
                columns: new[] { "property_id", "zone_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "postings",
                schema: "workforce");
        }
    }
}
