using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PublishShiftWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicationStatus",
                table: "Shifts",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Draft");

            // Les quarts déjà opérationnels appartiennent à la version publiée
            // avant l'introduction du workflow brouillon/publication.
            migrationBuilder.Sql("UPDATE \"Shifts\" SET \"PublicationStatus\" = 'Published'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicationStatus",
                table: "Shifts");
        }
    }
}
