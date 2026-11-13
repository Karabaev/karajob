using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karajob.Infrastructure.Persistence;

public class DatabaseInitializer(
  IDbContextFactory<KarajobDbContext> contextFactory,
  DatabaseOptions options,
  TimeProvider timeProvider,
  ILogger<DatabaseInitializer> logger)
{
  public const string BackupsFolderName = "backups";

  public async Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    var databasePath = options.Provider == DatabaseOptions.SqliteProvider
      ? Path.GetFullPath(new SqliteConnectionStringBuilder(options.ConnectionString).DataSource)
      : null;
    // Checked before the first connection: opening one creates an empty file.
    var databaseExisted = databasePath is not null && File.Exists(databasePath);
    if (databasePath is not null)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
    }

    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
    if (pending.Count == 0)
    {
      logger.LogInformation("Database is up to date, no pending migrations");
      return;
    }

    var backupPath = databaseExisted ? Backup(databasePath!, pending[^1]) : null;
    logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
    try
    {
      await context.Database.MigrateAsync(cancellationToken);
    }
    catch (Exception exception)
    {
      var message = backupPath is null
        ? "Database migration failed. No backup was taken because the database is new."
        : $"Database migration failed. Restore the backup from '{backupPath}'.";
      logger.LogCritical(exception, "{Message}", message);
      throw new DatabaseMigrationException(message, backupPath, exception);
    }

    if (options.Provider == DatabaseOptions.SqliteInMemoryProvider)
    {
      await DevelopmentSeed.SeedAsync(context, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
      logger.LogInformation("Seeded in-memory database with development data");
    }
  }

  private string Backup(string databasePath, string targetMigration)
  {
    var backupsDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, BackupsFolderName);
    Directory.CreateDirectory(backupsDirectory);
    var timestamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss");
    var backupPath = Path.Combine(backupsDirectory, $"{timestamp}-before-{targetMigration}.db");

    using var source = new SqliteConnection(options.ConnectionString);
    // Unpooled, so the backup file is not kept open after the copy.
    using var destination = new SqliteConnection(
      new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString());
    source.Open();
    destination.Open();
    source.BackupDatabase(destination);

    logger.LogInformation("Backed up database to {BackupPath}", backupPath);
    return backupPath;
  }
}
