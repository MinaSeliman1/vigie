using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeSessionVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "Employees");
        }
    }
}
