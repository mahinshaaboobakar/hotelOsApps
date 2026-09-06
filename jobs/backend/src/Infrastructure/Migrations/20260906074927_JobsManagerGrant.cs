using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Jobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JobsManagerGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jobs_manager_grant",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs_manager_grant", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_manager_grant_property_id_revoked_at",
                schema: "jobs",
                table: "jobs_manager_grant",
                columns: new[] { "property_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_manager_grant_property_id_user_id",
                schema: "jobs",
                table: "jobs_manager_grant",
                columns: new[] { "property_id", "user_id" },
                unique: true,
                filter: "revoked_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jobs_manager_grant",
                schema: "jobs");
        }
    }
}
