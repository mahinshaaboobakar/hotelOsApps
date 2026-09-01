using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RotaAndDuty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "duties",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duty_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    handover_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duties", x => x.id);
                    table.CheckConstraint("ck_duties__span_ordered", "ends_at > starts_at");
                });

            migrationBuilder.CreateTable(
                name: "shift_catalogue",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    short_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    colour = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_catalogue", x => x.id);
                    table.CheckConstraint("ck_shift_catalogue__code_present", "length(btrim(short_code)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "shift_hours",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalogue_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ends_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    second_starts_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    second_ends_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_hours", x => x.id);
                    table.CheckConstraint("ck_shift_hours__second_needs_first", "second_starts_at IS NULL OR starts_at IS NOT NULL");
                    table.CheckConstraint("ck_shift_hours__second_span_complete", "(second_starts_at IS NULL) = (second_ends_at IS NULL)");
                    table.CheckConstraint("ck_shift_hours__span_complete", "(starts_at IS NULL) = (ends_at IS NULL)");
                    table.CheckConstraint("ck_shift_hours__window_ordered", "effective_to IS NULL OR effective_to >= effective_from");
                });

            migrationBuilder.CreateIndex(
                name: "ix_duties__property_start",
                schema: "workforce",
                table: "duties",
                columns: new[] { "property_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "uq_shift_catalogue__property_code",
                schema: "workforce",
                table: "shift_catalogue",
                columns: new[] { "property_id", "short_code" },
                unique: true,
                filter: "active");

            migrationBuilder.CreateIndex(
                name: "ix_shift_hours__entry_from",
                schema: "workforce",
                table: "shift_hours",
                columns: new[] { "catalogue_entry_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "uq_shift_hours__one_open_revision",
                schema: "workforce",
                table: "shift_hours",
                column: "catalogue_entry_id",
                unique: true,
                filter: "effective_to IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duties",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "shift_catalogue",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "shift_hours",
                schema: "workforce");
        }
    }
}
