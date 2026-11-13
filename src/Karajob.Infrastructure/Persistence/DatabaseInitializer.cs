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

  public Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
