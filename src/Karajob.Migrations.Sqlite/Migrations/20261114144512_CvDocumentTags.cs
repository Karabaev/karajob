using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class CvDocumentTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "CvDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "CvDocuments");
        }
    }
}
