using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_DecidedBy",
                table: "SwapRequests",
                column: "DecidedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_ReceiverId",
                table: "SwapRequests",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Certifications_CertificationTypeId",
                table: "Certifications",
                column: "CertificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_EmployeeId",
                table: "Assignments",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Employees_EmployeeId",
                table: "Assignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Shifts_ShiftId",
                table: "Assignments",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Availabilities_Employees_EmployeeId",
                table: "Availabilities",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certifications_CertificationTypes_CertificationTypeId",
                table: "Certifications",
                column: "CertificationTypeId",
                principalTable: "CertificationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certifications_Employees_EmployeeId",
                table: "Certifications",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Sites_SiteId",
                table: "Shifts",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteCertificationRequirements_CertificationTypes_Certificat~",
                table: "SiteCertificationRequirements",
                column: "CertificationTypeId",
                principalTable: "CertificationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteCertificationRequirements_Sites_SiteId",
                table: "SiteCertificationRequirements",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SwapRequests_Assignments_AssignmentId",
                table: "SwapRequests",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SwapRequests_Employees_DecidedBy",
                table: "SwapRequests",
                column: "DecidedBy",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SwapRequests_Employees_ReceiverId",
                table: "SwapRequests",
                column: "ReceiverId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Employees_EmployeeId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Shifts_ShiftId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Availabilities_Employees_EmployeeId",
                table: "Availabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Certifications_CertificationTypes_CertificationTypeId",
                table: "Certifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Certifications_Employees_EmployeeId",
                table: "Certifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Sites_SiteId",
                table: "Shifts");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteCertificationRequirements_CertificationTypes_Certificat~",
                table: "SiteCertificationRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteCertificationRequirements_Sites_SiteId",
                table: "SiteCertificationRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_SwapRequests_Assignments_AssignmentId",
                table: "SwapRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SwapRequests_Employees_DecidedBy",
                table: "SwapRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SwapRequests_Employees_ReceiverId",
                table: "SwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_SwapRequests_DecidedBy",
                table: "SwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_SwapRequests_ReceiverId",
                table: "SwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_Certifications_CertificationTypeId",
                table: "Certifications");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_EmployeeId",
                table: "Assignments");
        }
    }
}
