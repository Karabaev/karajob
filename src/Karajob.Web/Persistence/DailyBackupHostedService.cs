using Karajob.Infrastructure.Persistence;

namespace Karajob.Web.Persistence;

// Checks hourly rather than sleeping 24 h, so container restarts never postpone or skip a snapshot.
public class DailyBackupHostedService(
  DailyDatabaseBackup backup,
  TimeProvider timeProvider,
  ILogger<DailyBackupHostedService> logger) : BackgroundService
{
  private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(CheckInterval, timeProvider);
    do
    {
      try
      {
        backup.RunIfDue();
      }
      catch (Exception exception)
      {
        // A failed snapshot must not take the tracker down; it is retried on the next check.
        logger.LogError(exception, "Daily database snapshot failed");
      }
    }
    while (await timer.WaitForNextTickAsync(stoppingToken));
  }
}
