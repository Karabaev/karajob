using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application.Hypotheses;
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
    for (var days = 4; days >= 1; days--)
    {
      CreateSnapshotFile(Now.AddDays(-days - 1));
    }
    var backup = provider.GetRequiredService<DailyDatabaseBackup>();

    backup.RunIfDue();

    var snapshots = Directory.GetFiles(DailyDirectory, "*.db").Select(Path.GetFileName).Order().ToList();
    Assert.Equal(
      [
        SnapshotName(Now.AddDays(-3)),
        SnapshotName(Now.AddDays(-2)),
        SnapshotName(Now),
      ],
      snapshots);
    Assert.True(File.Exists(preMigration));
    Assert.True(File.Exists(notes));
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
