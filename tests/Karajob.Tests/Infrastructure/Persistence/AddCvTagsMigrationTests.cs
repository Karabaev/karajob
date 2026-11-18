using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Karajob.Tests.Infrastructure.Persistence;

public class AddCvTagsMigrationTests : IAsyncLifetime
{
  private const string LastMigrationBeforeTags = "20261117165203_RenameHypothesisToTrack";
  private const string SecondUserId = "CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC";
  private const string ThirdUserId = "DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD";
  private const string OwnerSql = $"(SELECT Id FROM Users WHERE Id NOT IN ('{SecondUserId}', '{ThirdUserId}') LIMIT 1)";

  private static readonly string LongTag = new('a', 60);

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

    await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTags);
    await SeedOldSchemaAsync();
    await context.Database.MigrateAsync();
  }

  public async Task DisposeAsync()
  {
    await context.DisposeAsync();
    await connection.DisposeAsync();
  }

  [Fact]
  public async Task Migrate_LegacyJsonTags_CreatesLowercaseTrimmedDedupedTagsPerUser()
  {
    var owner = await QueryStringsAsync($"SELECT Name FROM Tags WHERE UserId = {OwnerSql} ORDER BY Name");
    var second = await QueryStringsAsync($"SELECT Name FROM Tags WHERE UserId = '{SecondUserId}' ORDER BY Name");

    Assert.Equal(["c#", "games", "unity"], owner);
    Assert.Equal([new string('a', 50), "unity"], second);
  }

  [Fact]
  public async Task Migrate_CyrillicTagsDifferingOnlyByCase_AreLowercasedAndMerged()
  {
    var names = await QueryStringsAsync($"SELECT Name FROM Tags WHERE UserId = '{ThirdUserId}' ORDER BY Name");

    Assert.Equal(["бэкенд", "ёж"], names);
  }

  [Fact]
  public async Task Migrate_LegacyJsonTags_LinksEachCvToItsDedupedTags()
  {
    var links = await QueryStringsAsync(
      """
      SELECT d.Name || ':' || t.Name
      FROM CvDocumentTags l
      JOIN CvDocuments d ON d.Id = l.CvDocumentId
      JOIN Tags t ON t.Id = l.TagId
      ORDER BY d.Name, t.Name
      """);

    Assert.Equal(
      ["cyrillic:бэкенд", "cyrillic:ёж", "first:c#", "first:unity", "second:games", "second:unity", "theirs:" + new string('a', 50), "theirs:unity"],
      links);
  }

  [Fact]
  public async Task Migrate_SameTagNameOnTwoCvsOfOneUser_SharesSingleTagRow()
  {
    var unity = await QueryStringsAsync($"SELECT Id FROM Tags WHERE Name = 'unity' AND UserId = {OwnerSql}");
    var linkedIds = await QueryStringsAsync(
      $"SELECT DISTINCT TagId FROM CvDocumentTags l JOIN Tags t ON t.Id = l.TagId WHERE t.Name = 'unity' AND t.UserId = {OwnerSql}");

    Assert.Single(unity);
    Assert.Equal(unity, linkedIds);
  }

  [Fact]
  public async Task Migrate_SameTagNameForTwoUsers_CreatesSeparateTagRows()
  {
    var ids = await QueryStringsAsync("SELECT Id FROM Tags WHERE Name = 'unity'");

    Assert.Equal(2, ids.Count);
    Assert.Equal(2, ids.Distinct().Count());
  }

  [Fact]
  public async Task Migrate_TagsAndLinks_BelongToTheCvOwner()
  {
    var mismatched = await QueryStringsAsync(
      """
      SELECT l.Id FROM CvDocumentTags l
      JOIN CvDocuments d ON d.Id = l.CvDocumentId
      JOIN Tags t ON t.Id = l.TagId
      WHERE l.UserId <> d.UserId OR t.UserId <> d.UserId
      """);

    Assert.Empty(mismatched);
  }

  [Fact]
  public async Task Migrate_BlankEmptyAndInvalidJson_CreateNoTagsAndKeepCvs()
  {
    var documents = await QueryStringsAsync("SELECT Name FROM CvDocuments ORDER BY Name");
    var linkedDocuments = await QueryStringsAsync(
      "SELECT DISTINCT d.Name FROM CvDocumentTags l JOIN CvDocuments d ON d.Id = l.CvDocumentId ORDER BY d.Name");

    Assert.Equal(["cyrillic", "empty", "first", "garbage", "none", "second", "theirs"], documents);
    Assert.Equal(["cyrillic", "first", "second", "theirs"], linkedDocuments);
  }

  [Fact]
  public async Task Migrate_NewRows_HaveUpperCaseGuidIdsAndNoColor()
  {
    var tagIds = await QueryStringsAsync("SELECT Id FROM Tags");
    var linkIds = await QueryStringsAsync("SELECT Id FROM CvDocumentTags");
    var colored = await QueryStringsAsync("SELECT COUNT(*) FROM Tags WHERE Color IS NOT NULL");

    Assert.All(tagIds.Concat(linkIds), id =>
    {
      Assert.True(Guid.TryParse(id, out _), id);
      Assert.Equal(id.ToUpperInvariant(), id);
    });
    Assert.Equal(["0"], colored);
  }

  [Fact]
  public async Task Migrate_LegacyTags_AreReadableThroughTheModel()
  {
    var first = await context.CvDocuments.SingleAsync(d => d.Name == "first");
    var tagIds = first.Tags.Select(l => l.TagId).ToList();
    var tagNames = await context.Tags
      .Where(t => tagIds.Contains(t.Id))
      .Select(t => t.Name)
      .OrderBy(n => n)
      .ToListAsync();

    Assert.Equal(["c#", "unity"], tagNames);
  }

  [Fact]
  public async Task Migrate_LegacyTags_DropsTagsColumnFromCvDocuments()
  {
    var columns = await QueryStringsAsync("SELECT name FROM pragma_table_info('CvDocuments')");

    Assert.DoesNotContain("Tags", columns);
  }

  [Fact]
  public async Task Migrate_LegacyTags_KeepsForeignKeysIntact()
  {
    var violations = await QueryStringsAsync("SELECT \"table\" FROM pragma_foreign_key_check");

    Assert.Empty(violations);
  }

  [Fact]
  public async Task Migrate_LegacyTags_CreatesUniqueIndexOnUserAndName()
  {
    var indexes = await QueryStringsAsync("SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = 'Tags'");

    Assert.Contains("IX_Tags_UserId_Name", indexes);
  }

  [Fact]
  public async Task Rollback_ToBeforeAddCvTags_RestoresTagsColumnAndKeepsCvs()
  {
    await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTags);

    var columns = await QueryStringsAsync("SELECT name FROM pragma_table_info('CvDocuments')");
    var tables = await QueryStringsAsync("SELECT name FROM sqlite_master WHERE type = 'table'");
    var documents = await QueryStringsAsync("SELECT Name FROM CvDocuments ORDER BY Name");

    Assert.Contains("Tags", columns);
    Assert.DoesNotContain("CvDocumentTags", tables);
    Assert.Equal(7, documents.Count);
  }

  private async Task SeedOldSchemaAsync()
  {
    string[] statements =
    [
      $"INSERT INTO Users (Id, Name, Color, CreatedAt, TimeZoneId) VALUES ('{SecondUserId}', 'Second', '#26a69a', '2026-11-01 00:00:00', 'UTC')",
      CvInsert("11111111-1111-1111-1111-111111111111", OwnerSql, "first", """["Unity","C#","unity"]"""),
      CvInsert("22222222-2222-2222-2222-222222222222", OwnerSql, "second", """["  UNITY  ","Games",""," "]"""),
      CvInsert("33333333-3333-3333-3333-333333333333", OwnerSql, "empty", ""),
      CvInsert("44444444-4444-4444-4444-444444444444", OwnerSql, "garbage", "not json"),
      CvInsert("55555555-5555-5555-5555-555555555555", OwnerSql, "none", "[]"),
      $"INSERT INTO Users (Id, Name, Color, CreatedAt, TimeZoneId) VALUES ('{ThirdUserId}', 'Third', '#26a69a', '2026-11-01 00:00:00', 'UTC')",
      CvInsert("77777777-7777-7777-7777-777777777777", $"'{ThirdUserId}'", "cyrillic", "[\"Бэкенд\",\"бэкенд\",\"Ёж\"]"),
      CvInsert("66666666-6666-6666-6666-666666666666", $"'{SecondUserId}'", "theirs", $"[\"Unity\",\"{LongTag}\"]"),
    ];

    foreach (var sql in statements)
    {
      await using var command = connection.CreateCommand();
      command.CommandText = sql;
      await command.ExecuteNonQueryAsync();
    }
  }

  private static string CvInsert(string id, string userSql, string name, string tagsJson)
  {
    return $"""
      INSERT INTO CvDocuments (Id, UserId, Name, LanguageId, Tags, CreatedAt)
      VALUES ('{id}', {userSql}, '{name}', (SELECT Id FROM Languages LIMIT 1), '{tagsJson}', '2026-11-01 00:00:00')
      """;
  }

  private async Task<List<string>> QueryStringsAsync(string sql)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await using var reader = await command.ExecuteReaderAsync();
    var result = new List<string>();
    while (await reader.ReadAsync())
    {
      result.Add(reader.GetValue(0).ToString()!);
    }

    return result;
  }
}
