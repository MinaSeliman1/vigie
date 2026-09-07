using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationOnboardingProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationType",
                table: "Organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoolCount",
                table: "Organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryGoal",
                table: "Organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamSize",
                table: "Organizations",
                type: "character varying(48)",
                maxLength: 48,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationType",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PoolCount",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PrimaryGoal",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "Organizations");
        }
    }
}
