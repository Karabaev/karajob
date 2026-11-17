using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddHypothesisCheckInsAndDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WellBeingThreshold",
                table: "SearchGoals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStartedAt",
                table: "Hypotheses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HypothesisCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HypothesisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Week = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Motivation = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypothesisCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypothesisCheckIns_Hypotheses_HypothesisId",
                        column: x => x.HypothesisId,
                        principalTable: "Hypotheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HypothesisCheckIns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HypothesisDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HypothesisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Changes = table.Column<string>(type: "TEXT", nullable: true),
                    PeriodStartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MarketVacancies = table.Column<int>(type: "INTEGER", nullable: false),
                    MarketThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    PositiveApplications = table.Column<int>(type: "INTEGER", nullable: false),
                    MaturedApplications = table.Column<int>(type: "INTEGER", nullable: false),
                    ConversionThresholdPermille = table.Column<int>(type: "INTEGER", nullable: false),
                    WellBeingTenths = table.Column<int>(type: "INTEGER", nullable: true),
                    Hint = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypothesisDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypothesisDecisions_Hypotheses_HypothesisId",
                        column: x => x.HypothesisId,
                        principalTable: "Hypotheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HypothesisDecisions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "SearchGoals",
                keyColumn: "Id",
                keyValue: new Guid("019a7c1e-0000-7000-8000-000000000401"),
                column: "WellBeingThreshold",
                value: 2);

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisCheckIns_HypothesisId_Week",
                table: "HypothesisCheckIns",
                columns: new[] { "HypothesisId", "Week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisCheckIns_UserId",
                table: "HypothesisCheckIns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisDecisions_HypothesisId_DecidedAt",
                table: "HypothesisDecisions",
                columns: new[] { "HypothesisId", "DecidedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisDecisions_UserId",
                table: "HypothesisDecisions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HypothesisCheckIns");

            migrationBuilder.DropTable(
                name: "HypothesisDecisions");

            migrationBuilder.DropColumn(
                name: "WellBeingThreshold",
                table: "SearchGoals");

            migrationBuilder.DropColumn(
                name: "PeriodStartedAt",
                table: "Hypotheses");
        }
    }
}
