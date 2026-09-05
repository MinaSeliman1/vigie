using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Availabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Availabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Certifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Role = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    WeeklyQuotaHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequiredLifeguards = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteCertificationRequirements",
                columns: table => new
                {
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificationTypeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteCertificationRequirements", x => new { x.SiteId, x.CertificationTypeId });
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OpeningSeason = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SwapRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ShiftId_EmployeeId",
                table: "Assignments",
                columns: new[] { "ShiftId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_EmployeeId_Date",
                table: "Availabilities",
                columns: new[] { "EmployeeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certifications_EmployeeId_CertificationTypeId",
                table: "Certifications",
                columns: new[] { "EmployeeId", "CertificationTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_SiteId_StartUtc",
                table: "Shifts",
                columns: new[] { "SiteId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteCertificationRequirements_CertificationTypeId",
                table: "SiteCertificationRequirements",
                column: "CertificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_AssignmentId_Status",
                table: "SwapRequests",
                columns: new[] { "AssignmentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Availabilities");

            migrationBuilder.DropTable(
                name: "Certifications");

            migrationBuilder.DropTable(
                name: "CertificationTypes");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "SiteCertificationRequirements");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "SwapRequests");
        }
    }
}
