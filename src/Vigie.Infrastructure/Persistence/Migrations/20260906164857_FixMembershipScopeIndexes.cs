using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMembershipScopeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId_SiteId_Se~",
                table: "OrganizationMemberships");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId",
                table: "OrganizationMemberships",
                columns: new[] { "EmployeeId", "OrganizationId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"SiteId\" IS NULL AND \"SectorId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId_SectorId",
                table: "OrganizationMemberships",
                columns: new[] { "EmployeeId", "OrganizationId", "SectorId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"SectorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId_SiteId",
                table: "OrganizationMemberships",
                columns: new[] { "EmployeeId", "OrganizationId", "SiteId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"SiteId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId_SectorId",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId_SiteId",
                table: "OrganizationMemberships");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId_SiteId_Se~",
                table: "OrganizationMemberships",
                columns: new[] { "EmployeeId", "OrganizationId", "SiteId", "SectorId", "IsActive" },
                unique: true);
        }
    }
}
