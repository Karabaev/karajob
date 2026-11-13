using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Catalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Hypotheses");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Hypotheses");

            migrationBuilder.DropColumn(
                name: "Locations",
                table: "Hypotheses");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "CvDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Hypotheses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                table: "Hypotheses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                table: "CvDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HypothesisLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HypothesisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypothesisLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypothesisLocations_Hypotheses_HypothesisId",
                        column: x => x.HypothesisId,
                        principalTable: "Hypotheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HypothesisLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HypothesisLocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "Code", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("019a7c1e-0000-7000-8000-000000000301"), "ru", "Русский", "русский" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000302"), "en", "English", "english" }
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("019a7c1e-0000-7000-8000-000000000201"), "EU", "eu" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000202"), "USA", "usa" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000203"), "Serbia", "serbia" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("019a7c1e-0000-7000-8000-000000000101"), "Unity Developer", "unity developer" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000102"), "Unreal Developer", "unreal developer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_LanguageId",
                table: "Vacancies",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_LocationId",
                table: "Vacancies",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_RoleId",
                table: "Vacancies",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Hypotheses_LanguageId",
                table: "Hypotheses",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Hypotheses_RoleId",
                table: "Hypotheses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CvDocuments_LanguageId",
                table: "CvDocuments",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisLocations_HypothesisId_LocationId",
                table: "HypothesisLocations",
                columns: new[] { "HypothesisId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisLocations_LocationId",
                table: "HypothesisLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisLocations_UserId",
                table: "HypothesisLocations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Code",
                table: "Languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Languages_NormalizedName",
                table: "Languages",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_NormalizedName",
                table: "Locations",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CvDocuments_Languages_LanguageId",
                table: "CvDocuments",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Hypotheses_Languages_LanguageId",
                table: "Hypotheses",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Hypotheses_Roles_RoleId",
                table: "Hypotheses",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_Languages_LanguageId",
                table: "Vacancies",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_Locations_LocationId",
                table: "Vacancies",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_Roles_RoleId",
                table: "Vacancies",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CvDocuments_Languages_LanguageId",
                table: "CvDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_Hypotheses_Languages_LanguageId",
                table: "Hypotheses");

            migrationBuilder.DropForeignKey(
                name: "FK_Hypotheses_Roles_RoleId",
                table: "Hypotheses");

            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_Languages_LanguageId",
                table: "Vacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_Locations_LocationId",
                table: "Vacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_Roles_RoleId",
                table: "Vacancies");

            migrationBuilder.DropTable(
                name: "HypothesisLocations");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_LanguageId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_LocationId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_RoleId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Hypotheses_LanguageId",
                table: "Hypotheses");

            migrationBuilder.DropIndex(
                name: "IX_Hypotheses_RoleId",
                table: "Hypotheses");

            migrationBuilder.DropIndex(
                name: "IX_CvDocuments_LanguageId",
                table: "CvDocuments");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Hypotheses");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Hypotheses");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "CvDocuments");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Hypotheses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Hypotheses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Locations",
                table: "Hypotheses",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Vacancies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "CvDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
