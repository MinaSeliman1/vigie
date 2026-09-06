using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "Name", "Slug", "CreatedAtUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "Vigie — démonstration", "vigie-demo", new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero) });

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Sites",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoAccount",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.Sql("UPDATE \"Employees\" SET \"IsDemoAccount\" = TRUE WHERE \"Email\" LIKE '%@vigie.demo'");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_OrganizationId",
                table: "Sites",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OrganizationId",
                table: "Employees",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Organizations_OrganizationId",
                table: "Employees",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_Organizations_OrganizationId",
                table: "Sites",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Organizations_OrganizationId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Sites_Organizations_OrganizationId",
                table: "Sites");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Sites_OrganizationId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Employees_OrganizationId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "IsDemoAccount",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Employees");
        }
    }
}
