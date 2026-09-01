using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Leave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leave_ledger",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    days = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_ledger", x => x.id);
                    table.CheckConstraint("ck_leave_ledger__days_not_zero", "days <> 0");
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_from = table.Column<DateOnly>(type: "date", nullable: false),
                    leave_to = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    entered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approver_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_requests", x => x.id);
                    table.CheckConstraint("ck_leave_requests__range_ordered", "leave_to >= leave_from");
                });

            migrationBuilder.CreateTable(
                name: "leave_types",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    accrual_per_month = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_types", x => x.id);
                    table.CheckConstraint("ck_leave_types__accrual_not_negative", "accrual_per_month IS NULL OR accrual_per_month >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_leave_ledger__property_staff_type",
                schema: "workforce",
                table: "leave_ledger",
                columns: new[] { "property_id", "staff_id", "leave_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests__property_approver_state",
                schema: "workforce",
                table: "leave_requests",
                columns: new[] { "property_id", "approver_staff_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests__property_staff_from",
                schema: "workforce",
                table: "leave_requests",
                columns: new[] { "property_id", "staff_id", "leave_from" });

            migrationBuilder.CreateIndex(
                name: "uq_leave_types__property_code",
                schema: "workforce",
                table: "leave_types",
                columns: new[] { "property_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leave_ledger",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "leave_requests",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "leave_types",
                schema: "workforce");
        }
    }
}
