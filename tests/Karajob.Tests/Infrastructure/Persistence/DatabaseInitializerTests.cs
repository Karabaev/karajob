using Karajob.Application.Users;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karajob.Tests.Infrastructure.Persistence;

public class DatabaseInitializerTests : IAsyncLifetime
{
  private static readonly DateTimeOffset Now = new(2026, 11, 13, 10, 15, 0, TimeSpan.Zero);

  private readonly string directory = Path.Combine(Path.GetTempPath(), "karajob-tests-" + Guid.NewGuid().ToString("N"));
  private readonly CapturingLoggerProvider logs = new();

  private string DatabasePath => Path.Combine(directory, "karajob.db");
  private string BackupsDirectory => Path.Combine(directory, DatabaseInitializer.BackupsFolderName);

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
  public async Task NewDatabaseFile_MigratesWithoutBackup()
  {
    await using var provider = BuildFileProvider();

    await provider.InitializeKarajobDatabaseAsync();

    Assert.True(File.Exists(DatabasePath));
    Assert.Empty(await GetPendingMigrationsAsync(provider));
    Assert.False(Directory.Exists(BackupsDirectory));
  }

  [Fact]
  public async Task UpToDateDatabase_DoesNotCreateBackup()
  {
    await using var provider = BuildFileProvider();
    await provider.InitializeKarajobDatabaseAsync();

    await provider.InitializeKarajobDatabaseAsync();

    Assert.False(Directory.Exists(BackupsDirectory));
  }

  [Fact]
  public async Task DatabaseWithPendingMigrations_CreatesBackupBeforeMigrating()
  {
    CreateLegacyDatabase("CREATE TABLE Marker (Id INTEGER PRIMARY KEY);");
    await using var provider = BuildFileProvider();
    var initialId = await GetLastMigrationIdAsync(provider);

    await provider.InitializeKarajobDatabaseAsync();

    var backup = Assert.Single(Directory.GetFiles(BackupsDirectory));
    Assert.Equal($"20261113-101500-before-{initialId}.db", Path.GetFileName(backup));
    Assert.Equal(1, ScalarCount(backup, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'Marker'"));
    Assert.Equal(0, ScalarCount(backup, "SELECT COUNT(*) FROM sqlite_master WHERE name = '__EFMigrationsHistory'"));
    Assert.Empty(await GetPendingMigrationsAsync(provider));
  }

  [Fact]
  public async Task FailingMigration_ThrowsWithBackupPath()
  {
    CreateLegacyDatabase("CREATE TABLE Users (Id INTEGER PRIMARY KEY);");
    await using var provider = BuildFileProvider();

    var exception = await Assert.ThrowsAsync<DatabaseMigrationException>(() => provider.InitializeKarajobDatabaseAsync());

    Assert.NotNull(exception.BackupPath);
    Assert.True(File.Exists(exception.BackupPath));
    Assert.Contains(exception.BackupPath, exception.Message);
    Assert.Contains(logs.Entries, e => e.Level == LogLevel.Critical && e.Message.Contains(exception.BackupPath));
  }

  [Fact]
  public async Task InMemoryProvider_MigratesAndSeeds()
  {
    await using var provider = BuildInMemoryProvider();

    await provider.InitializeKarajobDatabaseAsync();

    var factory = provider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await factory.CreateDbContextAsync();
    var vacancies = await context.Vacancies
      .Include(v => v.Application)
      .Include(v => v.Events)
      .Include(v => v.Matches)
      .ToListAsync();
    var stageCounts = vacancies.GroupBy(v => v.Stage).ToDictionary(g => g.Key, g => g.Count());

    Assert.Equal(1, await context.Users.CountAsync());
    Assert.Equal(3, await context.Hypotheses.CountAsync());
    Assert.Equal(10, vacancies.Count);
    Assert.Equal(2, stageCounts[VacancyStage.Inbox]);
    Assert.Equal(2, stageCounts[VacancyStage.Targeted]);
    Assert.Equal(1, stageCounts[VacancyStage.Preparing]);
    Assert.Equal(2, stageCounts[VacancyStage.Applied]);
    Assert.Equal(1, stageCounts[VacancyStage.Interviewing]);
    Assert.Equal(1, stageCounts[VacancyStage.Closed]);
    Assert.Equal(1, stageCounts[VacancyStage.Dismissed]);
    Assert.All(
      vacancies.Where(v => v.Stage is VacancyStage.Applied or VacancyStage.Interviewing or VacancyStage.Closed),
      v => Assert.NotNull(v.Application));
    Assert.All(vacancies.Where(v => v.Stage != VacancyStage.Inbox), v =>
    {
      Assert.NotEmpty(v.Events);
      Assert.NotEmpty(v.Matches);
    });
    Assert.NotEmpty(await context.Companies.ToListAsync());
    Assert.NotEmpty(await context.CvVersions.ToListAsync());
  }

  [Fact]
  public async Task InMemoryProvider_SeparateRegistrations_AreIsolated()
  {
    await using var first = BuildInMemoryProvider();
    await using var second = BuildInMemoryProvider();
    await first.InitializeKarajobDatabaseAsync();
    await second.InitializeKarajobDatabaseAsync();

    var firstFactory = first.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using (var context = await firstFactory.CreateDbContextAsync())
    {
      context.Hypotheses.Add(new Hypothesis(SeedData.OwnerId, "Extra", SeedData.UnityDeveloperRoleId, SeedData.EnglishLanguageId, Now.UtcDateTime));
      await context.SaveChangesAsync();
    }

    var secondFactory = second.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var secondContext = await secondFactory.CreateDbContextAsync();
    await using var firstContext = await firstFactory.CreateDbContextAsync();
    Assert.Equal(3, await secondContext.Hypotheses.CountAsync());
    Assert.Equal(4, await firstContext.Hypotheses.CountAsync());
  }

  private ServiceProvider BuildFileProvider()
  {
    return Build(
      new DatabaseOptions
      {
        Provider = DatabaseOptions.SqliteProvider,
        ConnectionString = $"Data Source={DatabasePath}",
      },
      logs);
  }

  private static ServiceProvider BuildInMemoryProvider()
  {
    return Build(new DatabaseOptions { Provider = DatabaseOptions.SqliteInMemoryProvider }, null);
  }

  private static ServiceProvider Build(DatabaseOptions options, ILoggerProvider? loggerProvider)
  {
    var services = new ServiceCollection();
    services.AddLogging(logging =>
    {
      if (loggerProvider is not null)
      {
        logging.AddProvider(loggerProvider);
      }
    });
    services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
    services.AddKarajobPersistence(options);
    return services.BuildServiceProvider();
  }

  private void CreateLegacyDatabase(string sql)
  {
    using var connection = new SqliteConnection($"Data Source={DatabasePath}");
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.ExecuteNonQuery();
  }

  private static int ScalarCount(string path, string sql)
  {
    using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    return Convert.ToInt32(command.ExecuteScalar());
  }

  private static async Task<string> GetLastMigrationIdAsync(ServiceProvider provider)
  {
    var factory = provider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await factory.CreateDbContextAsync();
    return context.Database.GetMigrations().Last();
  }

  private static async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(ServiceProvider provider)
  {
    var factory = provider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await factory.CreateDbContextAsync();
    return (await context.Database.GetPendingMigrationsAsync()).ToList();
  }

  private class CapturingLoggerProvider : ILoggerProvider, ILogger
  {
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName)
    {
      return this;
    }

    public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull
    {
      return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return true;
    }

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      lock (Entries)
      {
        Entries.Add((logLevel, formatter(state, exception)));
      }
    }

    public void Dispose()
    {
    }
  }

  private class FixedTimeProvider(DateTimeOffset now) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow()
    {
      return now;
    }
  }
}
