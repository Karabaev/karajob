using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Karajob.Application.Files;
using Karajob.Application.Transfer;
using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Goals;
using Karajob.Domain.MasterProfiles;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Files;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Transfer;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Transfer;

public class DatabaseTransferTests : IAsyncLifetime
{
  private const string DataFile = "database.json";

  private readonly string sourceRoot = NewTempDirectory();
  private readonly string targetRoot = NewTempDirectory();
  private SqliteTestDatabase source = null!;
  private SqliteTestDatabase target = null!;
  private LocalFileStorage sourceStorage = null!;
  private LocalFileStorage targetStorage = null!;
  private Guid secondUserId;

  private static DateTime Moment => PersistenceFixtures.Moment;

  [Fact]
  public async Task Roundtrip_TwoProfiles_ReproducesIdenticalDatabaseJson()
  {
    await SeedBothProfilesAsync();
    var exported = await ExportAsync(source, sourceStorage);

    await CreateService(target, targetStorage).ImportAsync(new MemoryStream(exported));

    var reExported = await ExportAsync(target, targetStorage);
    Assert.Equal(NormalizedJson(exported), NormalizedJson(reExported));
  }

  [Fact]
  public async Task Roundtrip_CvFiles_AreRestoredByteForByte()
  {
    await SeedBothProfilesAsync();
    var exported = await ExportAsync(source, sourceStorage);

    await CreateService(target, targetStorage).ImportAsync(new MemoryStream(exported));

    Assert.Equal(CvBytes("owner"), ReadFile(targetStorage, "cv/owner/cv.pdf"));
    Assert.Equal(CvBytes("second"), ReadFile(targetStorage, "cv/second/cv.pdf"));
  }

  [Fact]
  public async Task Export_Always_ContainsDataFileAndCvFilesInArchive()
  {
    await SeedBothProfilesAsync();

    var exported = await ExportAsync(source, sourceStorage);

    var entries = EntryNames(exported);
    Assert.Contains(DataFile, entries);
    Assert.Contains("files/cv/owner/cv.pdf", entries);
    Assert.Contains("files/cv/second/cv.pdf", entries);
  }

  [Fact]
  public async Task Export_Always_WritesHeaderWithFormatVersionAndLastMigration()
  {
    var exported = await ExportAsync(source, sourceStorage);

    var root = ReadJson(exported);
    Assert.Equal(1, (int)root["formatVersion"]!);
    Assert.False(string.IsNullOrWhiteSpace((string?)root["exportedAtUtc"]));
    await using var context = source.CreateContext(SeedData.OwnerId);
    var lastMigration = (await context.Database.GetAppliedMigrationsAsync()).Last();
    Assert.Equal(lastMigration, (string?)root["schemaMigration"]);
  }

  [Fact]
  public async Task Export_NoCurrentProfile_IncludesEveryProfilesRows()
  {
    await SeedBothProfilesAsync();

    var exported = await ExportAsync(source, sourceStorage);

    var tables = ReadJson(exported)["tables"]!.AsObject();
    var trackOwners = tables["Track"]!.AsArray().Select(r => (Guid)Property(r!, "UserId")!).ToList();
    Assert.Contains(SeedData.OwnerId, trackOwners);
    Assert.Contains(secondUserId, trackOwners);
    var vacancyOwners = tables["Vacancy"]!.AsArray().Select(r => (Guid)Property(r!, "UserId")!).ToList();
    Assert.Contains(SeedData.OwnerId, vacancyOwners);
    Assert.Contains(secondUserId, vacancyOwners);
  }

  [Fact]
  public async Task Export_EveryEntityTypeOfTheModel_HasATableEntry()
  {
    var exported = await ExportAsync(source, sourceStorage);

    var tables = ReadJson(exported)["tables"]!.AsObject();
    await using var context = source.CreateContext(SeedData.OwnerId);
    var missing = context.Model.GetEntityTypes()
      .Where(t => !t.IsOwned())
      .Select(t => t.ClrType.Name)
      .Where(name => !tables.ContainsKey(name))
      .ToList();
    Assert.Empty(missing);
  }

  [Fact]
  public async Task Export_StoredCvFileIsMissing_ThrowsCvFileMissing()
  {
    await SeedBothProfilesAsync();
    sourceStorage.Delete("cv/second/cv.pdf");

    var exception = await Assert.ThrowsAsync<DatabaseTransferException>(
      () => CreateService(source, sourceStorage).ExportAsync(new MemoryStream()));

    Assert.Equal(DatabaseTransferFailure.CvFileMissing, exception.Failure);
  }

  [Fact]
  public async Task Import_ExistingData_IsReplacedByArchiveContent()
  {
    await SeedBothProfilesAsync();
    var exported = await ExportAsync(source, sourceStorage);
    var other = await PersistenceFixtures.AddUserAsync(target, "Stranger");
    await PersistenceFixtures.AddTrackAsync(target, other.Id, "Stranger track");
    await PersistenceFixtures.AddVacancyAsync(target, other.Id, "https://stranger.example.com/job");
    await SaveFileAsync(targetStorage, "cv/old/stranger.pdf", [9, 9, 9]);
    await AddCvVersionRowAsync(target, other.Id, "cv/old/stranger.pdf");

    await CreateService(target, targetStorage).ImportAsync(new MemoryStream(exported));

    var sourceSnapshot = await SnapshotAsync(source);
    var targetSnapshot = await SnapshotAsync(target);
    Assert.Equal(sourceSnapshot, targetSnapshot);
    Assert.DoesNotContain(other.Id.ToString(), targetSnapshot);
  }

  [Fact]
  public async Task Import_ExistingCvFiles_AreRemovedFromStorage()
  {
    await SeedBothProfilesAsync();
    var exported = await ExportAsync(source, sourceStorage);
    var other = await PersistenceFixtures.AddUserAsync(target, "Stranger");
    await SaveFileAsync(targetStorage, "cv/old/stranger.pdf", [9, 9, 9]);
    await AddCvVersionRowAsync(target, other.Id, "cv/old/stranger.pdf");

    await CreateService(target, targetStorage).ImportAsync(new MemoryStream(exported));

    Assert.False(File.Exists(Path.Combine(targetRoot, "cv", "old", "stranger.pdf")));
  }

  [Fact]
  public async Task Import_ArchiveWithSamePathsAsExistingFiles_ReplacesFileContent()
  {
    await SeedBothProfilesAsync();
    var exported = await ExportAsync(source, sourceStorage);
    await SaveFileAsync(targetStorage, "cv/owner/cv.pdf", [1, 1, 1]);

    await CreateService(target, targetStorage).ImportAsync(new MemoryStream(exported));

    Assert.Equal(CvBytes("owner"), ReadFile(targetStorage, "cv/owner/cv.pdf"));
  }

  [Fact]
  public async Task Import_Valid_ReturnsSummaryWithRowAndFileCounts()
  {
    await SeedBothProfilesAsync();
    var exported = await ExportAsync(source, sourceStorage);

    var summary = await CreateService(target, targetStorage).ImportAsync(new MemoryStream(exported));

    Assert.Equal(2, summary.FileCount);
    Assert.Equal(2, summary.RowsByTable["Track"]);
    Assert.Equal(2, summary.RowsByTable["Vacancy"]);
    Assert.Equal(summary.RowsByTable.Values.Sum(), summary.TotalRows);
  }

  [Fact]
  public async Task Import_Valid_UserScopedContextSeesOnlyItsOwnRows()
  {
    await SeedBothProfilesAsync();
    var exported = await ExportAsync(source, sourceStorage);

    await CreateService(target, targetStorage).ImportAsync(new MemoryStream(exported));

    await using var second = target.CreateContext(secondUserId);
    Assert.Equal("Track second", (await second.Tracks.SingleAsync()).Name);
    Assert.Equal("https://second.example.com/job", (await second.Vacancies.SingleAsync()).Url);
    Assert.Single(await second.CvDocuments.ToListAsync());
    await using var owner = target.CreateContext(SeedData.OwnerId);
    Assert.Equal("Track owner", (await owner.Tracks.SingleAsync()).Name);
    Assert.Equal("https://owner.example.com/job", (await owner.Vacancies.SingleAsync()).Url);
  }

  [Fact]
  public async Task Import_NotAZipFile_ThrowsNotAnArchiveAndKeepsData()
  {
    var before = await PrepareTargetAsync();

    var exception = await Assert.ThrowsAsync<DatabaseTransferException>(
      () => CreateService(target, targetStorage).ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes("this is not a zip"))));

    Assert.Equal(DatabaseTransferFailure.NotAnArchive, exception.Failure);
    await AssertTargetUntouchedAsync(before);
  }

  [Fact]
  public async Task Import_ArchiveWithoutDataFile_ThrowsDataFileMissingAndKeepsData()
  {
    var before = await PrepareTargetAsync();
    var archive = Rebuild(await ExportSeededSourceAsync(), dropEntry: name => name == DataFile);

    var exception = await Assert.ThrowsAsync<DatabaseTransferException>(
      () => CreateService(target, targetStorage).ImportAsync(new MemoryStream(archive)));

    Assert.Equal(DatabaseTransferFailure.DataFileMissing, exception.Failure);
    await AssertTargetUntouchedAsync(before);
  }

  [Fact]
  public async Task Import_UnsupportedFormatVersion_ThrowsUnsupportedFormatVersionAndKeepsData()
  {
    var before = await PrepareTargetAsync();
    var archive = Rebuild(await ExportSeededSourceAsync(), editData: root => root["formatVersion"] = 999);

    var exception = await Assert.ThrowsAsync<DatabaseTransferException>(
      () => CreateService(target, targetStorage).ImportAsync(new MemoryStream(archive)));

    Assert.Equal(DatabaseTransferFailure.UnsupportedFormatVersion, exception.Failure);
    await AssertTargetUntouchedAsync(before);
  }

  [Fact]
  public async Task Import_DifferentSchemaMigration_ThrowsSchemaMismatchAndKeepsData()
  {
    var before = await PrepareTargetAsync();
    var archive = Rebuild(await ExportSeededSourceAsync(), editData: root => root["schemaMigration"] = "20200101000000_Older");

    var exception = await Assert.ThrowsAsync<DatabaseTransferException>(
      () => CreateService(target, targetStorage).ImportAsync(new MemoryStream(archive)));

    Assert.Equal(DatabaseTransferFailure.SchemaMismatch, exception.Failure);
    await AssertTargetUntouchedAsync(before);
  }

  [Fact]
  public async Task Import_CvFileMissingFromArchive_ThrowsCvFileMissingAndKeepsData()
  {
    var before = await PrepareTargetAsync();
    var archive = Rebuild(await ExportSeededSourceAsync(), dropEntry: name => name.StartsWith("files/", StringComparison.Ordinal));

    var exception = await Assert.ThrowsAsync<DatabaseTransferException>(
      () => CreateService(target, targetStorage).ImportAsync(new MemoryStream(archive)));

    Assert.Equal(DatabaseTransferFailure.CvFileMissing, exception.Failure);
    await AssertTargetUntouchedAsync(before);
  }

  public async Task InitializeAsync()
  {
    source = await SqliteTestDatabase.CreateAsync();
    target = await SqliteTestDatabase.CreateAsync();
    sourceStorage = new LocalFileStorage(new FileStorageOptions { RootDirectory = sourceRoot });
    targetStorage = new LocalFileStorage(new FileStorageOptions { RootDirectory = targetRoot });
  }

  public async Task DisposeAsync()
  {
    await source.DisposeAsync();
    await target.DisposeAsync();
    DeleteDirectory(sourceRoot);
    DeleteDirectory(targetRoot);
  }

  private async Task<byte[]> ExportSeededSourceAsync()
  {
    await SeedBothProfilesAsync();
    return await ExportAsync(source, sourceStorage);
  }

  private async Task<string> PrepareTargetAsync()
  {
    var other = await PersistenceFixtures.AddUserAsync(target, "Stranger");
    await PersistenceFixtures.AddTrackAsync(target, other.Id, "Stranger track");
    await SaveFileAsync(targetStorage, "cv/old/stranger.pdf", [9, 9, 9]);
    await AddCvVersionRowAsync(target, other.Id, "cv/old/stranger.pdf");
    return await SnapshotAsync(target);
  }

  private async Task AssertTargetUntouchedAsync(string before)
  {
    Assert.Equal(before, await SnapshotAsync(target));
    Assert.Equal<byte>([9, 9, 9], ReadFile(targetStorage, "cv/old/stranger.pdf"));
    Assert.False(File.Exists(Path.Combine(targetRoot, "cv", "owner", "cv.pdf")));
  }

  private static DatabaseTransferService CreateService(SqliteTestDatabase database, IFileStorage storage)
  {
    return new DatabaseTransferService(
      new SqliteTestContextFactory(database, Guid.NewGuid()),
      storage,
      new DatabaseOptions { Provider = DatabaseOptions.SqliteInMemoryProvider },
      TimeProvider.System);
  }

  private static async Task<byte[]> ExportAsync(SqliteTestDatabase database, IFileStorage storage)
  {
    using var stream = new MemoryStream();
    await CreateService(database, storage).ExportAsync(stream);
    return stream.ToArray();
  }

  private async Task SeedBothProfilesAsync()
  {
    var second = await PersistenceFixtures.AddUserAsync(source, "Second");
    secondUserId = second.Id;
    await SeedProfileAsync(SeedData.OwnerId, "owner");
    await SeedProfileAsync(second.Id, "second");
  }

  private async Task SeedProfileAsync(Guid userId, string key)
  {
    var company = new Company($"co-{key}", $"Company {key}")
    {
      Website = $"https://{key}.example.com",
      Description = $"About {key}",
    };
    company.AddDomain($"{key}.example.com");
    company.AddDomain($"{key}.io");
    company.AddAlias($"{key} Co");
    company.SetLocations([SeedData.EuLocationId]);
    var project = new Project(company.Id, $"Game {key}", ProjectStatus.InDevelopment)
    {
      Genre = "Strategy",
      EngineId = SeedData.UnityEngineId,
      Url = $"https://game.{key}.example.com",
    };
    var note = new UserCompanyNote(userId, company.Id) { Note = $"Note {key}", IsBlacklisted = true };

    var tag = new Tag(userId, $"tag-{key}");
    tag.SetColor("#26a69a");
    var document = new CvDocument(userId, $"CV {key}", SeedData.EnglishLanguageId, Moment);
    var bytes = CvBytes(key);
    var version = document.AddVersion("cv.pdf", $"cv/{key}/cv.pdf", "application/pdf", bytes.Length, "first", Moment);
    document.SetTags([tag.Id]);
    await SaveFileAsync(sourceStorage, version.StoragePath, bytes);

    var track = new Track(userId, $"Track {key}", SeedData.UnityDeveloperRoleId, SeedData.EnglishLanguageId, Moment);
    track.SetLocations([SeedData.EuLocationId, SeedData.SerbiaLocationId]);
    track.SetPlatforms([SeedData.PcPlatformId, SeedData.MobilePlatformId]);
    track.SetEngines([SeedData.UnityEngineId, SeedData.GodotEngineId]);
    track.Industries.AddRange(["Games", "Fintech"]);
    track.WorkFormat = WorkFormat.Remote | WorkFormat.Relocation;
    track.Positioning = "Senior";
    track.PrimaryCvDocumentId = document.Id;
    track.ChangeStatus(TrackStatus.Active, Moment);
    var checkIn = new TrackCheckIn(userId, track.Id, new DateOnly(2026, 3, 2), 4, 3, "ok", Moment);
    var snapshot = new DecisionSnapshot(Moment, 10, 5, 2, 4, 300, 35, DecisionHint.Continue);
    var decision = new TrackDecision(userId, track.Id, Moment, TrackDecisionKind.Continue, "keep going", null, snapshot);

    var vacancy = new Vacancy(userId, $"https://{key}.example.com/job", VacancySource.AiScan, Moment)
    {
      CompanyId = company.Id,
      RoleId = SeedData.UnrealDeveloperRoleId,
      LocationId = SeedData.UsaLocationId,
      LanguageId = SeedData.RussianLanguageId,
      WorkFormat = WorkFormat.Hybrid | WorkFormat.OnSite,
      Salary = new SalaryRange(3000, null, "eur"),
      Description = "desc",
      Notes = "notes",
      PublishedAt = Moment.AddDays(-2),
    };
    vacancy.SetPlatforms([SeedData.PcPlatformId, SeedData.WebPlatformId]);
    vacancy.SetEngines([SeedData.UnrealEngineId]);
    vacancy.SetMatch(track.Id, 80, "fits", MatchAuthor.Ai);
    vacancy.MoveTo(VacancyStage.Targeted, Moment);
    vacancy.MoveTo(VacancyStage.Preparing, Moment);
    vacancy.Apply(track.Id, version.Id, ApplicationChannel.Website, Moment, "letter");
    vacancy.AddEvent(ApplicationEventType.Note, Moment.AddHours(1), "called", TimeZoneInfo.Utc);

    var profile = new MasterProfile(userId, "# First\n", Moment);
    profile.AddVersion("# Second", "tweak", Moment.AddHours(1), profile.CurrentVersion.Id);

    await using var context = source.CreateContext(userId);
    var goal = await context.SearchGoals.SingleOrDefaultAsync();
    if (goal is null)
    {
      goal = new SearchGoal(userId);
      context.SearchGoals.Add(goal);
    }

    goal.RoleId = SeedData.UnityDeveloperRoleId;
    goal.Industries = "Games";
    goal.MustHave = "Remote";
    goal.WeeklyApplicationTarget = 7;
    goal.Deadline = new DateOnly(2026, 9, 1);
    goal.SetDesiredIncome(5000, "EUR");
    goal.SetLocations([SeedData.EuLocationId]);
    goal.SetPlatforms([SeedData.PcPlatformId]);
    goal.SetEngines([SeedData.UnityEngineId]);

    context.Companies.Add(company);
    context.Projects.Add(project);
    context.UserCompanyNotes.Add(note);
    context.Tags.Add(tag);
    context.CvDocuments.Add(document);
    context.Tracks.Add(track);
    await context.SaveChangesAsync();
    context.TrackCheckIns.Add(checkIn);
    context.TrackDecisions.Add(decision);
    context.Vacancies.Add(vacancy);
    context.MasterProfiles.Add(profile);
    await context.SaveChangesAsync();
  }

  private static async Task AddCvVersionRowAsync(SqliteTestDatabase database, Guid userId, string storagePath)
  {
    var document = new CvDocument(userId, "Old CV", SeedData.EnglishLanguageId, Moment);
    document.AddVersion("stranger.pdf", storagePath, "application/pdf", 3, null, Moment);
    await using var context = database.CreateContext(userId);
    context.CvDocuments.Add(document);
    await context.SaveChangesAsync();
  }

  private static async Task<string> SnapshotAsync(SqliteTestDatabase database)
  {
    await using var context = database.CreateContext(Guid.NewGuid());
    var builder = new StringBuilder();
    builder.AppendJoin(',', (await context.Users.IgnoreQueryFilters().Select(x => x.Id).ToListAsync()).Order()).AppendLine();
    builder.AppendJoin(',', (await context.Companies.Select(x => x.Id).ToListAsync()).Order()).AppendLine();
    builder.AppendJoin(',', (await context.Tracks.IgnoreQueryFilters().Select(x => x.Id).ToListAsync()).Order()).AppendLine();
    builder.AppendJoin(',', (await context.Vacancies.IgnoreQueryFilters().Select(x => x.Id).ToListAsync()).Order()).AppendLine();
    builder.AppendJoin(',', (await context.CvVersions.IgnoreQueryFilters().Select(x => x.StoragePath).ToListAsync()).Order()).AppendLine();
    builder.AppendJoin(',', (await context.SearchGoals.IgnoreQueryFilters().Select(x => x.Id).ToListAsync()).Order()).AppendLine();
    builder.AppendJoin(',', (await context.MasterProfiles.IgnoreQueryFilters().Select(x => x.Id).ToListAsync()).Order());
    return builder.ToString();
  }

  private static byte[] CvBytes(string key)
  {
    return Encoding.UTF8.GetBytes($"%PDF-1.7 content of {key} \u0001\u0002ÿ");
  }

  private static async Task SaveFileAsync(IFileStorage storage, string path, byte[] bytes)
  {
    storage.Delete(path);
    await storage.SaveAsync(path, new MemoryStream(bytes));
  }

  private static byte[] ReadFile(IFileStorage storage, string path)
  {
    using var stream = storage.OpenRead(path);
    using var copy = new MemoryStream();
    stream.CopyTo(copy);
    return copy.ToArray();
  }

  private static List<string> EntryNames(byte[] archive)
  {
    using var zip = new ZipArchive(new MemoryStream(archive));
    return zip.Entries.Select(e => e.FullName).ToList();
  }

  private static JsonObject ReadJson(byte[] archive)
  {
    using var zip = new ZipArchive(new MemoryStream(archive));
    using var reader = new StreamReader(zip.GetEntry(DataFile)!.Open(), Encoding.UTF8);
    return JsonNode.Parse(reader.ReadToEnd())!.AsObject();
  }

  private static JsonNode? Property(JsonNode row, string name)
  {
    return row.AsObject().Single(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
  }

  private static string NormalizedJson(byte[] archive)
  {
    var root = ReadJson(archive);
    root.Remove("exportedAtUtc");
    var tables = root["tables"]!.AsObject();
    foreach (var name in tables.Select(t => t.Key).ToList())
    {
      var rows = tables[name]!.AsArray().Select(r => r!.ToJsonString()).Order(StringComparer.Ordinal).ToList();
      tables[name] = new JsonArray(rows.Select(r => JsonNode.Parse(r)).ToArray());
    }

    return root.ToJsonString();
  }

  private static byte[] Rebuild(byte[] archive, Action<JsonObject>? editData = null, Func<string, bool>? dropEntry = null)
  {
    using var input = new ZipArchive(new MemoryStream(archive));
    using var output = new MemoryStream();
    using (var result = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
    {
      foreach (var entry in input.Entries)
      {
        if (dropEntry?.Invoke(entry.FullName) == true) continue;

        using var content = new MemoryStream();
        using (var entryStream = entry.Open())
        {
          entryStream.CopyTo(content);
        }

        var bytes = content.ToArray();
        if (entry.FullName == DataFile && editData is not null)
        {
          var root = JsonNode.Parse(bytes)!.AsObject();
          editData(root);
          bytes = Encoding.UTF8.GetBytes(root.ToJsonString());
        }

        var created = result.CreateEntry(entry.FullName);
        using var createdStream = created.Open();
        createdStream.Write(bytes);
      }
    }

    return output.ToArray();
  }

  private static string NewTempDirectory()
  {
    return Path.Combine(Path.GetTempPath(), "karajob-transfer-" + Guid.NewGuid().ToString("N"));
  }

  private static void DeleteDirectory(string path)
  {
    if (Directory.Exists(path))
    {
      Directory.Delete(path, true);
    }
  }
}
