using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddUserColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Users",
                type: "TEXT",
                maxLength: 7,
                nullable: false,
                defaultValue: "#7e57c2");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000001"),
                column: "Color",
                value: "#7e57c2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Users");
        }
    }
}
