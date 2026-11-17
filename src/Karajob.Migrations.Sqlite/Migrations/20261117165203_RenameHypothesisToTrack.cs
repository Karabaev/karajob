using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class RenameHypothesisToTrack : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.RenameTable(
      name: "Hypotheses",
      newName: "Tracks");

    migrationBuilder.RenameTable(
      name: "HypothesisCheckIns",
      newName: "TrackCheckIns");

    migrationBuilder.RenameTable(
      name: "HypothesisDecisions",
      newName: "TrackDecisions");

    migrationBuilder.RenameTable(
      name: "HypothesisEngines",
      newName: "TrackEngines");

    migrationBuilder.RenameTable(
      name: "HypothesisLocations",
      newName: "TrackLocations");

    migrationBuilder.RenameTable(
      name: "HypothesisPlatforms",
      newName: "TrackPlatforms");

    migrationBuilder.RenameColumn(
      name: "HypothesisId",
      table: "TrackCheckIns",
      newName: "TrackId");

    migrationBuilder.RenameColumn(
      name: "HypothesisId",
      table: "TrackDecisions",
      newName: "TrackId");

    migrationBuilder.RenameColumn(
      name: "HypothesisId",
      table: "TrackEngines",
      newName: "TrackId");

    migrationBuilder.RenameColumn(
      name: "HypothesisId",
      table: "TrackLocations",
      newName: "TrackId");

    migrationBuilder.RenameColumn(
      name: "HypothesisId",
      table: "TrackPlatforms",
      newName: "TrackId");

    migrationBuilder.RenameColumn(
      name: "HypothesisId",
      table: "JobApplications",
      newName: "TrackId");

    migrationBuilder.RenameColumn(
      name: "HypothesisId",
      table: "VacancyMatches",
      newName: "TrackId");

    migrationBuilder.RenameIndex(
      name: "IX_Hypotheses_LanguageId",
      table: "Tracks",
      newName: "IX_Tracks_LanguageId");

    migrationBuilder.RenameIndex(
      name: "IX_Hypotheses_PrimaryCvDocumentId",
      table: "Tracks",
      newName: "IX_Tracks_PrimaryCvDocumentId");

    migrationBuilder.RenameIndex(
      name: "IX_Hypotheses_RoleId",
      table: "Tracks",
      newName: "IX_Tracks_RoleId");

    migrationBuilder.RenameIndex(
      name: "IX_Hypotheses_UserId",
      table: "Tracks",
      newName: "IX_Tracks_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisCheckIns_HypothesisId_Week",
      table: "TrackCheckIns",
      newName: "IX_TrackCheckIns_TrackId_Week");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisCheckIns_UserId",
      table: "TrackCheckIns",
      newName: "IX_TrackCheckIns_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisDecisions_HypothesisId_DecidedAt",
      table: "TrackDecisions",
      newName: "IX_TrackDecisions_TrackId_DecidedAt");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisDecisions_UserId",
      table: "TrackDecisions",
      newName: "IX_TrackDecisions_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisEngines_EngineId",
      table: "TrackEngines",
      newName: "IX_TrackEngines_EngineId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisEngines_HypothesisId_EngineId",
      table: "TrackEngines",
      newName: "IX_TrackEngines_TrackId_EngineId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisEngines_UserId",
      table: "TrackEngines",
      newName: "IX_TrackEngines_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisLocations_HypothesisId_LocationId",
      table: "TrackLocations",
      newName: "IX_TrackLocations_TrackId_LocationId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisLocations_LocationId",
      table: "TrackLocations",
      newName: "IX_TrackLocations_LocationId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisLocations_UserId",
      table: "TrackLocations",
      newName: "IX_TrackLocations_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisPlatforms_HypothesisId_PlatformId",
      table: "TrackPlatforms",
      newName: "IX_TrackPlatforms_TrackId_PlatformId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisPlatforms_PlatformId",
      table: "TrackPlatforms",
      newName: "IX_TrackPlatforms_PlatformId");

    migrationBuilder.RenameIndex(
      name: "IX_HypothesisPlatforms_UserId",
      table: "TrackPlatforms",
      newName: "IX_TrackPlatforms_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_JobApplications_HypothesisId",
      table: "JobApplications",
      newName: "IX_JobApplications_TrackId");

    migrationBuilder.RenameIndex(
      name: "IX_VacancyMatches_HypothesisId",
      table: "VacancyMatches",
      newName: "IX_VacancyMatches_TrackId");

    migrationBuilder.RenameIndex(
      name: "IX_VacancyMatches_VacancyId_HypothesisId",
      table: "VacancyMatches",
      newName: "IX_VacancyMatches_VacancyId_TrackId");

  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.RenameTable(
      name: "TrackPlatforms",
      newName: "HypothesisPlatforms");

    migrationBuilder.RenameTable(
      name: "TrackLocations",
      newName: "HypothesisLocations");

    migrationBuilder.RenameTable(
      name: "TrackEngines",
      newName: "HypothesisEngines");

    migrationBuilder.RenameTable(
      name: "TrackDecisions",
      newName: "HypothesisDecisions");

    migrationBuilder.RenameTable(
      name: "TrackCheckIns",
      newName: "HypothesisCheckIns");

    migrationBuilder.RenameTable(
      name: "Tracks",
      newName: "Hypotheses");

    migrationBuilder.RenameColumn(
      name: "TrackId",
      table: "HypothesisPlatforms",
      newName: "HypothesisId");

    migrationBuilder.RenameColumn(
      name: "TrackId",
      table: "HypothesisLocations",
      newName: "HypothesisId");

    migrationBuilder.RenameColumn(
      name: "TrackId",
      table: "HypothesisEngines",
      newName: "HypothesisId");

    migrationBuilder.RenameColumn(
      name: "TrackId",
      table: "HypothesisDecisions",
      newName: "HypothesisId");

    migrationBuilder.RenameColumn(
      name: "TrackId",
      table: "HypothesisCheckIns",
      newName: "HypothesisId");

    migrationBuilder.RenameColumn(
      name: "TrackId",
      table: "VacancyMatches",
      newName: "HypothesisId");

    migrationBuilder.RenameColumn(
      name: "TrackId",
      table: "JobApplications",
      newName: "HypothesisId");

    migrationBuilder.RenameIndex(
      name: "IX_VacancyMatches_VacancyId_TrackId",
      table: "VacancyMatches",
      newName: "IX_VacancyMatches_VacancyId_HypothesisId");

    migrationBuilder.RenameIndex(
      name: "IX_VacancyMatches_TrackId",
      table: "VacancyMatches",
      newName: "IX_VacancyMatches_HypothesisId");

    migrationBuilder.RenameIndex(
      name: "IX_JobApplications_TrackId",
      table: "JobApplications",
      newName: "IX_JobApplications_HypothesisId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackPlatforms_UserId",
      table: "HypothesisPlatforms",
      newName: "IX_HypothesisPlatforms_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackPlatforms_PlatformId",
      table: "HypothesisPlatforms",
      newName: "IX_HypothesisPlatforms_PlatformId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackPlatforms_TrackId_PlatformId",
      table: "HypothesisPlatforms",
      newName: "IX_HypothesisPlatforms_HypothesisId_PlatformId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackLocations_UserId",
      table: "HypothesisLocations",
      newName: "IX_HypothesisLocations_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackLocations_LocationId",
      table: "HypothesisLocations",
      newName: "IX_HypothesisLocations_LocationId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackLocations_TrackId_LocationId",
      table: "HypothesisLocations",
      newName: "IX_HypothesisLocations_HypothesisId_LocationId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackEngines_UserId",
      table: "HypothesisEngines",
      newName: "IX_HypothesisEngines_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackEngines_TrackId_EngineId",
      table: "HypothesisEngines",
      newName: "IX_HypothesisEngines_HypothesisId_EngineId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackEngines_EngineId",
      table: "HypothesisEngines",
      newName: "IX_HypothesisEngines_EngineId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackDecisions_UserId",
      table: "HypothesisDecisions",
      newName: "IX_HypothesisDecisions_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackDecisions_TrackId_DecidedAt",
      table: "HypothesisDecisions",
      newName: "IX_HypothesisDecisions_HypothesisId_DecidedAt");

    migrationBuilder.RenameIndex(
      name: "IX_TrackCheckIns_UserId",
      table: "HypothesisCheckIns",
      newName: "IX_HypothesisCheckIns_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_TrackCheckIns_TrackId_Week",
      table: "HypothesisCheckIns",
      newName: "IX_HypothesisCheckIns_HypothesisId_Week");

    migrationBuilder.RenameIndex(
      name: "IX_Tracks_UserId",
      table: "Hypotheses",
      newName: "IX_Hypotheses_UserId");

    migrationBuilder.RenameIndex(
      name: "IX_Tracks_RoleId",
      table: "Hypotheses",
      newName: "IX_Hypotheses_RoleId");

    migrationBuilder.RenameIndex(
      name: "IX_Tracks_PrimaryCvDocumentId",
      table: "Hypotheses",
      newName: "IX_Hypotheses_PrimaryCvDocumentId");

    migrationBuilder.RenameIndex(
      name: "IX_Tracks_LanguageId",
      table: "Hypotheses",
      newName: "IX_Hypotheses_LanguageId");
  }
}
