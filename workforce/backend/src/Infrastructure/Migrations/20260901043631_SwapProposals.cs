using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Workforce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SwapProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "swap_proposals",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposer_staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    colleague_staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposer_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    colleague_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    entered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approver_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_swap_proposals", x => x.id);
                    table.CheckConstraint("ck_swap_proposals__two_people", "proposer_staff_id <> colleague_staff_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_swap_proposals__property_state_approver",
                schema: "workforce",
                table: "swap_proposals",
                columns: new[] { "property_id", "state", "approver_staff_id" });

            migrationBuilder.CreateIndex(
                name: "ix_swap_proposals__property_state_colleague",
                schema: "workforce",
                table: "swap_proposals",
                columns: new[] { "property_id", "state", "colleague_staff_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "swap_proposals",
                schema: "workforce");
        }
    }
}
