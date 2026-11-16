using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class LocalizeSeedNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000601"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Russian ruble", "russian ruble" });

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000602"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "US dollar", "us dollar" });

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000603"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Euro", "euro" });

            migrationBuilder.UpdateData(
                table: "Engines",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000706"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Proprietary", "proprietary" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000301"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Russian", "russian" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000601"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Российский рубль", "российский рубль" });

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000602"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Доллар США", "доллар сша" });

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000603"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Евро", "евро" });

            migrationBuilder.UpdateData(
                table: "Engines",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000706"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Собственный", "собственный" });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000301"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Русский", "русский" });
        }
    }
}
