using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLavalOperationsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SectorId",
                table: "Sites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SectorId",
                table: "Invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                table: "Invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sectors_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_OrganizationId_SectorId",
                table: "Sites",
                columns: new[] { "OrganizationId", "SectorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_SectorId",
                table: "Sites",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_OrganizationId_SiteId_SectorId",
                table: "Invitations",
                columns: new[] { "OrganizationId", "SiteId", "SectorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_SectorId",
                table: "Invitations",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_SiteId",
                table: "Invitations",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_EmployeeId_OrganizationId_SiteId_Se~",
                table: "OrganizationMemberships",
                columns: new[] { "EmployeeId", "OrganizationId", "SiteId", "SectorId", "IsActive" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId_EmployeeId",
                table: "OrganizationMemberships",
                columns: new[] { "OrganizationId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_SectorId",
                table: "OrganizationMemberships",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_SiteId",
                table: "OrganizationMemberships",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_OrganizationId_Code",
                table: "Sectors",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_OrganizationId_Name",
                table: "Sectors",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Sectors_SectorId",
                table: "Invitations",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Sites_SiteId",
                table: "Invitations",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_Sectors_SectorId",
                table: "Sites",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Sectors_SectorId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Sites_SiteId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Sites_Sectors_SectorId",
                table: "Sites");

            migrationBuilder.DropTable(
                name: "OrganizationMemberships");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropIndex(
                name: "IX_Sites_OrganizationId_SectorId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_SectorId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_OrganizationId_SiteId_SectorId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_SectorId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_SiteId",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Invitations");
        }
    }
}
