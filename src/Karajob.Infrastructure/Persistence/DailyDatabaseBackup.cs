using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Karajob.Infrastructure.Persistence;

public class DailyDatabaseBackup(
  DatabaseOptions databaseOptions,
  BackupOptions backupOptions,
  TimeProvider timeProvider,
  ILogger<DailyDatabaseBackup> logger)
{
  public const string DailyFolderName = "daily";

  private const string TimestampFormat = "yyyyMMdd-HHmmss";
  private const string Extension = ".db";
  private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

  public string? RunIfDue()
  {
    var databasePath = Path.GetFullPath(new SqliteConnectionStringBuilder(databaseOptions.ConnectionString).DataSource);
    var dailyDirectory = Path.Combine(
      Path.GetDirectoryName(databasePath)!, DatabaseInitializer.BackupsFolderName, DailyFolderName);
    Directory.CreateDirectory(dailyDirectory);

    var now = timeProvider.GetUtcNow().UtcDateTime;
    var snapshots = ListSnapshots(dailyDirectory);
    if (snapshots.Count > 0 && now - snapshots[0].TakenAt < Interval)
    {
      return null;
    }

    var snapshotPath = Path.Combine(dailyDirectory, now.ToString(TimestampFormat, CultureInfo.InvariantCulture) + Extension);
    SqliteBackup.Copy(databaseOptions.ConnectionString!, snapshotPath);
    logger.LogInformation("Saved daily database snapshot to {SnapshotPath}", snapshotPath);

    foreach (var expired in ListSnapshots(dailyDirectory).Skip(backupOptions.KeepDaily))
    {
      File.Delete(expired.Path);
      logger.LogInformation("Deleted expired daily snapshot {SnapshotPath}", expired.Path);
    }

    return snapshotPath;
  }

  private static List<(string Path, DateTime TakenAt)> ListSnapshots(string directory)
  {
    var snapshots = new List<(string Path, DateTime TakenAt)>();
    foreach (var path in Directory.EnumerateFiles(directory, "*" + Extension))
    {
      var name = Path.GetFileNameWithoutExtension(path);
      if (!DateTime.TryParseExact(name, TimestampFormat, CultureInfo.InvariantCulture,
        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var takenAt)) continue;

      snapshots.Add((path, takenAt));
    }

    return snapshots.OrderByDescending(snapshot => snapshot.TakenAt).ToList();
  }
}
