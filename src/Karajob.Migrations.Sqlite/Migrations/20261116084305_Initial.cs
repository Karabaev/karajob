using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconPath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                    table.UniqueConstraint("AK_Currencies_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Engines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Engines", x => x.Id);
                });

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
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false, defaultValue: "#7e57c2"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAliases_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyDomains_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Genre = table.Column<string>(type: "TEXT", nullable: true),
                    EngineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Engines_EngineId",
                        column: x => x.EngineId,
                        principalTable: "Engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CvDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvDocuments_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CvDocuments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                        name: "FK_SearchGoals_Currencies_IncomeCurrency",
                        column: x => x.IncomeCurrency,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
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
                name: "UserCompanyNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    IsBlacklisted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanyNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompanyNotes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCompanyNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vacancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    WorkFormat = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscoveredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    DismissReason = table.Column<int>(type: "INTEGER", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Salary_Currency = table.Column<string>(type: "TEXT", nullable: true),
                    Salary_Max = table.Column<long>(type: "INTEGER", nullable: true),
                    Salary_Min = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacancies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CvVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CvDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvVersions_CvDocuments_CvDocumentId",
                        column: x => x.CvDocumentId,
                        principalTable: "CvDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CvVersions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Hypotheses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Industries = table.Column<string>(type: "TEXT", nullable: false),
                    WorkFormat = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Positioning = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryCvDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hypotheses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hypotheses_CvDocuments_PrimaryCvDocumentId",
                        column: x => x.PrimaryCvDocumentId,
                        principalTable: "CvDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Hypotheses_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Hypotheses_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Hypotheses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SearchGoalEngines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SearchGoalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EngineId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchGoalEngines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchGoalEngines_Engines_EngineId",
                        column: x => x.EngineId,
                        principalTable: "Engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SearchGoalEngines_SearchGoals_SearchGoalId",
                        column: x => x.SearchGoalId,
                        principalTable: "SearchGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchGoalEngines_Users_UserId",
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
                name: "ApplicationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VacancyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FromStage = table.Column<int>(type: "INTEGER", nullable: true),
                    ToStage = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationEvents_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyEngines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VacancyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EngineId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyEngines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyEngines_Engines_EngineId",
                        column: x => x.EngineId,
                        principalTable: "Engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyEngines_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyEngines_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "HypothesisEngines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HypothesisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EngineId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypothesisEngines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypothesisEngines_Engines_EngineId",
                        column: x => x.EngineId,
                        principalTable: "Engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HypothesisEngines_Hypotheses_HypothesisId",
                        column: x => x.HypothesisId,
                        principalTable: "Hypotheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HypothesisEngines_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VacancyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HypothesisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CvVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CoverLetter = table.Column<string>(type: "TEXT", nullable: true),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplications_CvVersions_CvVersionId",
                        column: x => x.CvVersionId,
                        principalTable: "CvVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplications_Hypotheses_HypothesisId",
                        column: x => x.HypothesisId,
                        principalTable: "Hypotheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplications_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VacancyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HypothesisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: true),
                    Rationale = table.Column<string>(type: "TEXT", nullable: true),
                    MatchedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyMatches_Hypotheses_HypothesisId",
                        column: x => x.HypothesisId,
                        principalTable: "Hypotheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyMatches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyMatches_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("019a7c1e-0000-7000-8000-000000000601"), "RUB", "Российский рубль", "российский рубль" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000602"), "USD", "Доллар США", "доллар сша" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000603"), "EUR", "Евро", "евро" }
                });

            migrationBuilder.InsertData(
                table: "Engines",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("019a7c1e-0000-7000-8000-000000000701"), "Unity", "unity" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000702"), "Unreal Engine", "unreal engine" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000703"), "Godot", "godot" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000704"), "Defold", "defold" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000705"), "Cocos", "cocos" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000706"), "Собственный", "собственный" }
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

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("019a7c1e-0000-7000-8000-000000000101"), "Unity Developer", "unity developer" },
                    { new Guid("019a7c1e-0000-7000-8000-000000000102"), "Unreal Developer", "unreal developer" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Color", "CreatedAt", "Name" },
                values: new object[] { new Guid("019a7c1e-0000-7000-8000-000000000001"), "#7e57c2", new DateTime(2026, 11, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Owner" });

            migrationBuilder.InsertData(
                table: "SearchGoals",
                columns: new[] { "Id", "ConversionThresholdPermille", "Deadline", "DesiredMonthlyIncome", "IncomeCurrency", "Industries", "MarketThresholdPerWeek", "MaturationDays", "MinSampleSize", "MustHave", "RoleId", "UserId", "WeeklyApplicationTarget" },
                values: new object[] { new Guid("019a7c1e-0000-7000-8000-000000000401"), 30, null, null, null, null, 10, 14, 30, null, null, new Guid("019a7c1e-0000-7000-8000-000000000001"), 10 });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationEvents_UserId",
                table: "ApplicationEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationEvents_VacancyId",
                table: "ApplicationEvents",
                column: "VacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Slug",
                table: "Companies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAliases_CompanyId",
                table: "CompanyAliases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAliases_Value",
                table: "CompanyAliases",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDomains_CompanyId",
                table: "CompanyDomains",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDomains_Value",
                table: "CompanyDomains",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_NormalizedName",
                table: "Currencies",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvDocuments_LanguageId",
                table: "CvDocuments",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CvDocuments_UserId",
                table: "CvDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CvVersions_CvDocumentId",
                table: "CvVersions",
                column: "CvDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CvVersions_UserId",
                table: "CvVersions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Engines_NormalizedName",
                table: "Engines",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hypotheses_LanguageId",
                table: "Hypotheses",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Hypotheses_PrimaryCvDocumentId",
                table: "Hypotheses",
                column: "PrimaryCvDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Hypotheses_RoleId",
                table: "Hypotheses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Hypotheses_UserId",
                table: "Hypotheses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisEngines_EngineId",
                table: "HypothesisEngines",
                column: "EngineId");

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisEngines_HypothesisId_EngineId",
                table: "HypothesisEngines",
                columns: new[] { "HypothesisId", "EngineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HypothesisEngines_UserId",
                table: "HypothesisEngines",
                column: "UserId");

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
                name: "IX_JobApplications_CvVersionId",
                table: "JobApplications",
                column: "CvVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_HypothesisId",
                table: "JobApplications",
                column: "HypothesisId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_UserId",
                table: "JobApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_VacancyId",
                table: "JobApplications",
                column: "VacancyId",
                unique: true);

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
                name: "IX_Platforms_NormalizedName",
                table: "Platforms",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_EngineId",
                table: "Projects",
                column: "EngineId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalEngines_EngineId",
                table: "SearchGoalEngines",
                column: "EngineId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalEngines_SearchGoalId_EngineId",
                table: "SearchGoalEngines",
                columns: new[] { "SearchGoalId", "EngineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoalEngines_UserId",
                table: "SearchGoalEngines",
                column: "UserId");

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
                name: "IX_SearchGoals_IncomeCurrency",
                table: "SearchGoals",
                column: "IncomeCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoals_RoleId",
                table: "SearchGoals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchGoals_UserId",
                table: "SearchGoals",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyNotes_CompanyId",
                table: "UserCompanyNotes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyNotes_UserId_CompanyId",
                table: "UserCompanyNotes",
                columns: new[] { "UserId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_CompanyId",
                table: "Vacancies",
                column: "CompanyId");

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
                name: "IX_Vacancies_UserId_Url",
                table: "Vacancies",
                columns: new[] { "UserId", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacancyEngines_EngineId",
                table: "VacancyEngines",
                column: "EngineId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyEngines_UserId",
                table: "VacancyEngines",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyEngines_VacancyId_EngineId",
                table: "VacancyEngines",
                columns: new[] { "VacancyId", "EngineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacancyMatches_HypothesisId",
                table: "VacancyMatches",
                column: "HypothesisId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyMatches_UserId",
                table: "VacancyMatches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyMatches_VacancyId_HypothesisId",
                table: "VacancyMatches",
                columns: new[] { "VacancyId", "HypothesisId" },
                unique: true);

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
                name: "ApplicationEvents");

            migrationBuilder.DropTable(
                name: "CompanyAliases");

            migrationBuilder.DropTable(
                name: "CompanyDomains");

            migrationBuilder.DropTable(
                name: "HypothesisEngines");

            migrationBuilder.DropTable(
                name: "HypothesisLocations");

            migrationBuilder.DropTable(
                name: "HypothesisPlatforms");

            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "SearchGoalEngines");

            migrationBuilder.DropTable(
                name: "SearchGoalLocations");

            migrationBuilder.DropTable(
                name: "SearchGoalPlatforms");

            migrationBuilder.DropTable(
                name: "UserCompanyNotes");

            migrationBuilder.DropTable(
                name: "VacancyEngines");

            migrationBuilder.DropTable(
                name: "VacancyMatches");

            migrationBuilder.DropTable(
                name: "VacancyPlatforms");

            migrationBuilder.DropTable(
                name: "CvVersions");

            migrationBuilder.DropTable(
                name: "SearchGoals");

            migrationBuilder.DropTable(
                name: "Engines");

            migrationBuilder.DropTable(
                name: "Hypotheses");

            migrationBuilder.DropTable(
                name: "Platforms");

            migrationBuilder.DropTable(
                name: "Vacancies");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "CvDocuments");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
