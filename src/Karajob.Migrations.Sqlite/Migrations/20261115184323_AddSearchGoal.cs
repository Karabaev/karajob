using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Industries = table.Column<string>(type: "TEXT", nullable: true),
                    MustHave = table.Column<string>(type: "TEXT", nullable: true),
                    WeeklyApplicationTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    Deadline = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DesiredMonthlyIncome = table.Column<long>(type: "INTEGER", nullable: true),
                    IncomeCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    MarketThresholdPerWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    ConversionThresholdPermille = table.Column<int>(type: "INTEGER", nullable: false),
                    MinSampleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    MaturationDays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchGoals_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SearchGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SearchGoalLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SearchGoalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchGoalLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchGoalLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SearchGoalLocations_SearchGoals_SearchGoalId",
                        column: x => x.SearchGoalId,
                        principalTable: "SearchGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchGoalLocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SearchGoals",
                columns: new[] { "Id", "ConversionThresholdPermille", "Deadline", "DesiredMonthlyIncome", "IncomeCurrency", "Industries", "MarketThresholdPerWeek", "MaturationDays", "MinSampleSize", "MustHave", "RoleId", "UserId", "WeeklyApplicationTarget" },
                values: new object[] { new Guid("019a7c1e-0000-7000-8000-000000000401"), 30, null, null, null, null, 10, 14, 30, null, null, new Guid("019a7c1e-0000-7000-8000-000000000001"), 10 });

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalLocations_LocationId",
                table: "SearchGoalLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalLocations_SearchGoalId_LocationId",
                table: "SearchGoalLocations",
                columns: new[] { "SearchGoalId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalLocations_UserId",
                table: "SearchGoalLocations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoals_RoleId",
                table: "SearchGoals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoals_UserId",
                table: "SearchGoals",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchGoalLocations");

            migrationBuilder.DropTable(
                name: "SearchGoals");
        }
    }
}
