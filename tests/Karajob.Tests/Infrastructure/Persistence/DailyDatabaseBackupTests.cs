using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application.Tracks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Infrastructure.Persistence;

public class DailyDatabaseBackupTests : IAsyncLifetime
{
  private static readonly DateTime Now = new(2026, 11, 13, 10, 15, 0, DateTimeKind.Utc);

  private readonly string directory = Path.Combine(Path.GetTempPath(), "karajob-tests-" + Guid.NewGuid().ToString("N"));

  private string DatabasePath => Path.Combine(directory, "karajob.db");
  private string BackupsDirectory => Path.Combine(directory, DatabaseInitializer.BackupsFolderName);
  private string DailyDirectory => Path.Combine(BackupsDirectory, DailyDatabaseBackup.DailyFolderName);

  public Task InitializeAsync()
  {
    Directory.CreateDirectory(directory);
    return Task.CompletedTask;
  }

  public Task DisposeAsync()
  {
    SqliteConnection.ClearAllPools();
    Directory.Delete(directory, true);
    return Task.CompletedTask;
  }

  [Fact]
  public async Task RunIfDue_NoSnapshots_CreatesConsistentSnapshot()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    var path = backup.RunIfDue();

    var expected = Path.Combine(DailyDirectory, "20261113-101500.db");
    Assert.Equal(expected, path);
    Assert.True(File.Exists(expected));
    Assert.Equal(1, ScalarCount(expected, "SELECT COUNT(*) FROM sqlite_master WHERE name = '__EFMigrationsHistory'"));
    var sourceUsers = ScalarCount(DatabasePath, "SELECT COUNT(*) FROM Users");
    Assert.True(sourceUsers > 0);
    Assert.Equal(sourceUsers, ScalarCount(expected, "SELECT COUNT(*) FROM Users"));
    Assert.Equal(
      ScalarCount(DatabasePath, "SELECT COUNT(*) FROM __EFMigrationsHistory"),
      ScalarCount(expected, "SELECT COUNT(*) FROM __EFMigrationsHistory"));
  }

  [Fact]
  public async Task RunIfDue_SnapshotYoungerThan24Hours_ReturnsNullAndCreatesNothing()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    CreateSnapshotFile(Now.AddHours(-23));
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    var path = backup.RunIfDue();

    Assert.Null(path);
    Assert.Single(Directory.GetFiles(DailyDirectory));
  }

  [Fact]
  public async Task RunIfDue_NewestSnapshotOlderThan24Hours_CreatesNewSnapshot()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    CreateSnapshotFile(Now.AddHours(-25));
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    var path = backup.RunIfDue();

    Assert.Equal(Path.Combine(DailyDirectory, "20261113-101500.db"), path);
    Assert.Equal(2, Directory.GetFiles(DailyDirectory).Length);
  }

  [Fact]
  public async Task RunIfDue_SnapshotExactly24HoursOld_CreatesNewSnapshot()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    CreateSnapshotFile(Now.AddHours(-24));
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    var path = backup.RunIfDue();

    Assert.NotNull(path);
    Assert.Equal(2, Directory.GetFiles(DailyDirectory).Length);
  }

  [Fact]
  public async Task RunIfDue_NewestIsYoungButOlderOnesExist_ReturnsNull()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    CreateSnapshotFile(Now.AddDays(-5));
    CreateSnapshotFile(Now.AddHours(-2));
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    var path = backup.RunIfDue();

    Assert.Null(path);
    Assert.Equal(2, Directory.GetFiles(DailyDirectory).Length);
  }

  [Fact]
  public async Task RunIfDue_MoreSnapshotsThanKeepDaily_KeepsOnlyNewest()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 3);
    Directory.CreateDirectory(BackupsDirectory);
    var preMigration = Path.Combine(BackupsDirectory, "20261101-000000-before-X.db");
    File.WriteAllText(preMigration, "x");
    Directory.CreateDirectory(DailyDirectory);
    var notes = Path.Combine(DailyDirectory, "notes.txt");
    File.WriteAllText(notes, "x");
    var unparsable = Path.Combine(DailyDirectory, "notes.db");
    File.WriteAllText(unparsable, "x");
    for (var days = 4; days >= 1; days--)
    {
      CreateSnapshotFile(Now.AddDays(-days - 1));
    }
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    backup.RunIfDue();

    var snapshots = Directory.GetFiles(DailyDirectory, "2*.db").Select(Path.GetFileName).Order().ToList();
    Assert.Equal(
      [
        SnapshotName(Now.AddDays(-3)),
        SnapshotName(Now.AddDays(-2)),
        SnapshotName(Now),
      ],
      snapshots);
    Assert.True(File.Exists(preMigration));
    Assert.True(File.Exists(notes));
    Assert.True(File.Exists(unparsable));
  }

  [Fact]
  public async Task RunIfDue_OnlyUnparsableDbFile_CreatesSnapshot()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    Directory.CreateDirectory(DailyDirectory);
    File.WriteAllText(Path.Combine(DailyDirectory, "notes.db"), "x");
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    Assert.NotNull(backup.RunIfDue());
  }

  [Fact]
  public async Task RunIfDue_SnapshotFromTheFuture_CreatesSnapshot()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    CreateSnapshotFile(Now.AddDays(2));
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    Assert.Equal(Path.Combine(DailyDirectory, SnapshotName(Now)), backup.RunIfDue());
  }

  [Fact]
  public async Task RunIfDue_NotDueButTooManySnapshots_DeletesExpired()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 2);
    CreateSnapshotFile(Now.AddDays(-3));
    CreateSnapshotFile(Now.AddDays(-2));
    CreateSnapshotFile(Now.AddHours(-1));
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    Assert.Null(backup.RunIfDue());

    var snapshots = Directory.GetFiles(DailyDirectory).Select(Path.GetFileName).Order().ToList();
    Assert.Equal([SnapshotName(Now.AddDays(-2)), SnapshotName(Now.AddHours(-1))], snapshots);
  }

  [Fact]
  public void SqliteBackupCopy_FailingSource_LeavesNoTempFile()
  {
    var source = Path.Combine(directory, "broken.db");
    File.WriteAllText(source, "this is not a SQLite database, just text long enough to fill a header page");
    var destination = Path.Combine(directory, "copy.db");

    Assert.ThrowsAny<SqliteException>(() => SqliteBackup.Copy($"Data Source={source};Pooling=False", destination));

    Assert.False(File.Exists(destination));
    Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
  }

  [Fact]
  public async Task RunIfDue_AfterRun_LeavesNoTempFiles()
  {
    await using var provider = await BuildMigratedProviderAsync(keepDaily: 7);
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    backup.RunIfDue();

    Assert.Empty(Directory.GetFiles(DailyDirectory, "*.tmp"));
    Assert.Single(Directory.GetFiles(DailyDirectory));
  }

  [Fact]
  public void AddKarajobDailyBackup_KeepDailyZero_Throws()
  {
    var services = new ServiceCollection();

    Assert.ThrowsAny<ArgumentException>(() =>
      services.AddKarajobDailyBackup(FileOptions(), new BackupOptions { KeepDaily = 0 }));
  }

  [Fact]
  public void AddKarajobDailyBackup_InMemoryProvider_DoesNotRegisterBackup()
  {
    var options = new DatabaseOptions { Provider = DatabaseOptions.SqliteInMemoryProvider };
    var services = BuildServices(options);
    services.AddKarajobDailyBackup(options, new BackupOptions { KeepDaily = 7 });
    using var provider = services.BuildServiceProvider();

    Assert.Null(provider.GetService<DailyDatabaseBackup>());
  }

  [Fact]
  public void AddKarajobDailyBackup_SqliteProvider_RegistersBackup()
  {
    var options = FileOptions();
    var services = BuildServices(options);
    services.AddKarajobDailyBackup(options, new BackupOptions { KeepDaily = 7 });
    using var provider = services.BuildServiceProvider();

    Assert.NotNull(provider.GetService<DailyDatabaseBackup>());
  }

  private static string SnapshotName(DateTime time)
  {
    return time.ToString("yyyyMMdd-HHmmss") + ".db";
  }

  private DatabaseOptions FileOptions()
  {
    return new DatabaseOptions
    {
      Provider = DatabaseOptions.SqliteProvider,
      ConnectionString = $"Data Source={DatabasePath}",
    };
  }

  private void CreateSnapshotFile(DateTime time)
  {
    Directory.CreateDirectory(DailyDirectory);
    File.WriteAllText(Path.Combine(DailyDirectory, SnapshotName(time)), "old");
  }

  private static ServiceCollection BuildServices(DatabaseOptions options)
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
    services.AddKarajobPersistence(options);
    return services;
  }

  private async Task<ServiceProvider> BuildMigratedProviderAsync(int keepDaily)
  {
    var options = FileOptions();
    var services = BuildServices(options);
    services.AddKarajobDailyBackup(options, new BackupOptions { KeepDaily = keepDaily });
    var provider = services.BuildServiceProvider();
    await provider.InitializeKarajobDatabaseAsync();
    return provider;
  }

  private static int ScalarCount(string path, string sql)
  {
    using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    return Convert.ToInt32(command.ExecuteScalar());
  }
}
