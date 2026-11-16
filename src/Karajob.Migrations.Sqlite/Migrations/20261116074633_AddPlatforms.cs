using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatforms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HypothesisPlatforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HypothesisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypothesisPlatforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypothesisPlatforms_Hypotheses_HypothesisId",
                        column: x => x.HypothesisId,
                        principalTable: "Hypotheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HypothesisPlatforms_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HypothesisPlatforms_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SearchGoalPlatforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SearchGoalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchGoalPlatforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchGoalPlatforms_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SearchGoalPlatforms_SearchGoals_SearchGoalId",
                        column: x => x.SearchGoalId,
                        principalTable: "SearchGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchGoalPlatforms_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VacancyPlatforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VacancyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyPlatforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyPlatforms_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyPlatforms_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyPlatforms_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Platforms",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("019a7c1e-0000-7000-8000-000000000501"), "PC", "pc" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000502"), "Consoles", "consoles" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000503"), "Mobile", "mobile" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000504"), "VR", "vr" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000505"), "Web", "web" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisPlatforms_HypothesisId_PlatformId",
                table: "HypothesisPlatforms",
                columns: new[] { "HypothesisId", "PlatformId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisPlatforms_PlatformId",
                table: "HypothesisPlatforms",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisPlatforms_UserId",
                table: "HypothesisPlatforms",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_NormalizedName",
                table: "Platforms",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalPlatforms_PlatformId",
                table: "SearchGoalPlatforms",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalPlatforms_SearchGoalId_PlatformId",
                table: "SearchGoalPlatforms",
                columns: new[] { "SearchGoalId", "PlatformId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalPlatforms_UserId",
                table: "SearchGoalPlatforms",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyPlatforms_PlatformId",
                table: "VacancyPlatforms",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyPlatforms_UserId",
                table: "VacancyPlatforms",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyPlatforms_VacancyId_PlatformId",
                table: "VacancyPlatforms",
                columns: new[] { "VacancyId", "PlatformId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HypothesisPlatforms");

            migrationBuilder.DropTable(
                name: "SearchGoalPlatforms");

            migrationBuilder.DropTable(
                name: "VacancyPlatforms");

            migrationBuilder.DropTable(
                name: "Platforms");
        }
    }
}
