using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddVacancyTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Vacancies",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Vacancies");
        }
    }
}
