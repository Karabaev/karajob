using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Karajob.Tests.Infrastructure.Persistence;

public class RenameHypothesisToTrackMigrationTests : IAsyncLifetime
{
  private const string LastMigrationBeforeRename = "20261117162539_AddCompanyLocations";
  private const string HypothesisId = "11111111-1111-1111-1111-111111111111";

  private static readonly string[] OldTables =
  [
    "Hypotheses",
    "HypothesisCheckIns",
    "HypothesisDecisions",
    "HypothesisEngines",
    "HypothesisLocations",
    "HypothesisPlatforms",
  ];

  private static readonly string[] NewTables =
  [
    "Tracks",
    "TrackCheckIns",
    "TrackDecisions",
    "TrackEngines",
    "TrackLocations",
    "TrackPlatforms",
  ];

  private SqliteConnection connection = null!;
  private KarajobDbContext context = null!;

  public async Task InitializeAsync()
  {
    connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<KarajobDbContext>()
      .UseSqlite(connection, b => b.MigrationsAssembly(DatabaseOptions.SqliteMigrationsAssembly))
      .Options;
    context = new KarajobDbContext(options, new FixedCurrentUser(SeedData.OwnerId));

    var migrator = context.GetService<IMigrator>();
    await migrator.MigrateAsync(LastMigrationBeforeRename);
    await SeedOldSchemaAsync();
    await context.Database.MigrateAsync();
  }

  public async Task DisposeAsync()
  {
    await context.DisposeAsync();
    await connection.DisposeAsync();
  }

  [Fact]
  public async Task Migrate_FromAddCompanyLocations_RemovesOldTables()
  {
    var existing = await QueryStringsAsync("SELECT name FROM sqlite_master WHERE type = 'table'");

    Assert.Empty(existing.Intersect(OldTables));
  }

  [Fact]
  public async Task Migrate_FromAddCompanyLocations_CreatesNewTables()
  {
    var existing = await QueryStringsAsync("SELECT name FROM sqlite_master WHERE type = 'table'");

    Assert.Empty(NewTables.Except(existing));
  }

  [Fact]
  public async Task Migrate_FromAddCompanyLocations_KeepsHypothesisRowInTracks()
  {
    var ids = await QueryStringsAsync("SELECT Id FROM Tracks");
    var names = await QueryStringsAsync("SELECT Name FROM Tracks");

    Assert.Equal([HypothesisId], ids, StringComparer.OrdinalIgnoreCase);
    Assert.Equal(["Legacy hypothesis"], names);
  }

  [Theory]
  [InlineData("TrackCheckIns")]
  [InlineData("TrackDecisions")]
  [InlineData("TrackEngines")]
  [InlineData("TrackLocations")]
  [InlineData("TrackPlatforms")]
  [InlineData("JobApplications")]
  [InlineData("VacancyMatches")]
  public async Task Migrate_FromAddCompanyLocations_KeepsDependentRowWithTrackId(string table)
  {
    var trackIds = await QueryStringsAsync($"SELECT TrackId FROM {table}");

    Assert.Equal([HypothesisId], trackIds, StringComparer.OrdinalIgnoreCase);
  }

  [Theory]
  [InlineData("TrackCheckIns")]
  [InlineData("TrackDecisions")]
  [InlineData("TrackEngines")]
  [InlineData("TrackLocations")]
  [InlineData("TrackPlatforms")]
  [InlineData("JobApplications")]
  [InlineData("VacancyMatches")]
  public async Task Migrate_FromAddCompanyLocations_DropsHypothesisIdColumn(string table)
  {
    var columns = await QueryStringsAsync($"SELECT name FROM pragma_table_info('{table}')");

    Assert.Contains("TrackId", columns);
    Assert.DoesNotContain("HypothesisId", columns);
  }

  [Fact]
  public async Task Migrate_FromAddCompanyLocations_KeepsForeignKeysIntact()
  {
    var violations = await QueryStringsAsync("SELECT \"table\" FROM pragma_foreign_key_check");

    Assert.Empty(violations);
  }

  [Fact]
  public async Task Migrate_FromAddCompanyLocations_RenamesIndexes()
  {
    var indexes = await QueryStringsAsync("SELECT name FROM sqlite_master WHERE type = 'index'");

    Assert.DoesNotContain(indexes, name => name.Contains("Hypothes"));
    Assert.Contains("IX_TrackEngines_TrackId_EngineId", indexes);
    Assert.Contains("IX_VacancyMatches_VacancyId_TrackId", indexes);
    Assert.Contains("IX_TrackCheckIns_TrackId_Week", indexes);
  }

  [Fact]
  public async Task Rollback_ToAddCompanyLocations_RestoresOldNamesAndData()
  {
    await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeRename);

    var tables = await QueryStringsAsync("SELECT name FROM sqlite_master WHERE type = 'table'");
    var ids = await QueryStringsAsync("SELECT Id FROM Hypotheses");
    var matchColumns = await QueryStringsAsync("SELECT name FROM pragma_table_info('VacancyMatches')");

    Assert.Empty(OldTables.Except(tables));
    Assert.Empty(NewTables.Intersect(tables));
    Assert.Equal([HypothesisId], ids, StringComparer.OrdinalIgnoreCase);
    Assert.Contains("HypothesisId", matchColumns);
  }

  private async Task SeedOldSchemaAsync()
  {
    string[] statements =
    [
      $"""
      INSERT INTO Hypotheses (Id, UserId, Name, RoleId, Industries, WorkFormat, LanguageId, Status, CreatedAt)
      VALUES ('{HypothesisId}', (SELECT Id FROM Users LIMIT 1), 'Legacy hypothesis',
        (SELECT Id FROM Roles LIMIT 1), '', 0, (SELECT Id FROM Languages LIMIT 1), 0, '2026-11-01 00:00:00')
      """,
      $"""
      INSERT INTO HypothesisCheckIns (Id, UserId, HypothesisId, Week, Motivation, Confidence, CreatedAt, UpdatedAt)
      VALUES ('22222222-2222-2222-2222-222222222222', (SELECT Id FROM Users LIMIT 1), '{HypothesisId}',
        '2026-11-02', 3, 3, '2026-11-02 00:00:00', '2026-11-02 00:00:00')
      """,
      $"""
      INSERT INTO HypothesisDecisions (Id, UserId, HypothesisId, DecidedAt, Kind, PeriodStartedAt, MarketVacancies,
        MarketThreshold, PositiveApplications, MaturedApplications, ConversionThresholdPermille, Hint)
      VALUES ('33333333-3333-3333-3333-333333333333', (SELECT Id FROM Users LIMIT 1), '{HypothesisId}',
        '2026-11-03 00:00:00', 0, '2026-11-01 00:00:00', 0, 0, 0, 0, 0, 0)
      """,
      $"""
      INSERT INTO HypothesisEngines (Id, UserId, HypothesisId, EngineId)
      VALUES ('44444444-4444-4444-4444-444444444444', (SELECT Id FROM Users LIMIT 1), '{HypothesisId}',
        (SELECT Id FROM Engines LIMIT 1))
      """,
      $"""
      INSERT INTO HypothesisLocations (Id, UserId, HypothesisId, LocationId)
      VALUES ('55555555-5555-5555-5555-555555555555', (SELECT Id FROM Users LIMIT 1), '{HypothesisId}',
        (SELECT Id FROM Locations LIMIT 1))
      """,
      $"""
      INSERT INTO HypothesisPlatforms (Id, UserId, HypothesisId, PlatformId)
      VALUES ('66666666-6666-6666-6666-666666666666', (SELECT Id FROM Users LIMIT 1), '{HypothesisId}',
        (SELECT Id FROM Platforms LIMIT 1))
      """,
      """
      INSERT INTO Vacancies (Id, UserId, Url, Source, DiscoveredAt, Stage)
      VALUES ('77777777-7777-7777-7777-777777777777', (SELECT Id FROM Users LIMIT 1),
        'https://example.com/job', 0, '2026-11-01 00:00:00', 0)
      """,
      """
      INSERT INTO CvDocuments (Id, UserId, Name, LanguageId, Tags, CreatedAt)
      VALUES ('88888888-8888-8888-8888-888888888888', (SELECT Id FROM Users LIMIT 1), 'CV',
        (SELECT Id FROM Languages LIMIT 1), '', '2026-11-01 00:00:00')
      """,
      """
      INSERT INTO CvVersions (Id, UserId, CvDocumentId, FileName, StoragePath, ContentType, SizeBytes, CreatedAt)
      VALUES ('99999999-9999-9999-9999-999999999999', (SELECT Id FROM Users LIMIT 1),
        '88888888-8888-8888-8888-888888888888', 'cv.pdf', 'cv/cv.pdf', 'application/pdf', 1, '2026-11-01 00:00:00')
      """,
      $"""
      INSERT INTO JobApplications (Id, UserId, VacancyId, HypothesisId, CvVersionId, Channel, AppliedAt)
      VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', (SELECT Id FROM Users LIMIT 1),
        '77777777-7777-7777-7777-777777777777', '{HypothesisId}', '99999999-9999-9999-9999-999999999999',
        0, '2026-11-04 00:00:00')
      """,
      $"""
      INSERT INTO VacancyMatches (Id, UserId, VacancyId, HypothesisId, MatchedBy)
      VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', (SELECT Id FROM Users LIMIT 1),
        '77777777-7777-7777-7777-777777777777', '{HypothesisId}', 0)
      """,
    ];

    foreach (var sql in statements)
    {
      await using var command = connection.CreateCommand();
      command.CommandText = sql;
      await command.ExecuteNonQueryAsync();
    }
  }

  private async Task<List<string>> QueryStringsAsync(string sql)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await using var reader = await command.ExecuteReaderAsync();
    var result = new List<string>();
    while (await reader.ReadAsync())
    {
      result.Add(reader.GetString(0));
    }

    return result;
  }
}
