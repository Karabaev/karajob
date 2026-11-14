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
    var snapshotPath = IsDue(ListSnapshots(dailyDirectory), now) ? TakeSnapshot(dailyDirectory, now) : null;
    // On every check, not only after a snapshot, so a failed deletion is retried within the hour.
    DeleteExpired(dailyDirectory);
    return snapshotPath;
  }

  private static bool IsDue(List<(string Path, DateTime TakenAt)> snapshots, DateTime now)
  {
    if (snapshots.Count == 0) return true;

    var age = now - snapshots[0].TakenAt;
    // A negative age means the clock was moved back: waiting for it to catch up could skip days.
    return age >= Interval || age < TimeSpan.Zero;
  }

  private string TakeSnapshot(string dailyDirectory, DateTime now)
  {
    var snapshotPath = Path.Combine(dailyDirectory, now.ToString(TimestampFormat, CultureInfo.InvariantCulture) + Extension);
    SqliteBackup.Copy(databaseOptions.ConnectionString!, snapshotPath);
    logger.LogInformation("Saved daily database snapshot to {SnapshotPath}", snapshotPath);
    return snapshotPath;
  }

  private void DeleteExpired(string dailyDirectory)
  {
    foreach (var expired in ListSnapshots(dailyDirectory).Skip(backupOptions.KeepDaily))
    {
      File.Delete(expired.Path);
      logger.LogInformation("Deleted expired daily snapshot {SnapshotPath}", expired.Path);
    }
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
