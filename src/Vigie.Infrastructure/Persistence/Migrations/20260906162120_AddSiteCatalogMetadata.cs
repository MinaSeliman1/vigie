using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteCatalogMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Sites",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsMunicipal",
                table: "Sites",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "Sites",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "IsMunicipal",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "Sites");
        }
    }
}
