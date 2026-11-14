using Microsoft.Extensions.Logging;

namespace Karajob.Infrastructure.Persistence;

public class DailyDatabaseBackup(
  DatabaseOptions databaseOptions,
  BackupOptions backupOptions,
  TimeProvider timeProvider,
  ILogger<DailyDatabaseBackup> logger)
{
  public const string DailyFolderName = "daily";

  public string? RunIfDue()
  {
    throw new NotImplementedException();
  }
}
