using Karajob.Application.Files;
using Karajob.Application.Transfer;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Infrastructure.Transfer;

public class DatabaseTransferService : IDatabaseTransfer
{
  public Task ExportAsync(Stream destination, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<ImportSummary> ImportAsync(Stream source, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public DatabaseTransferService(
    IDbContextFactory<KarajobDbContext> contextFactory,
    IFileStorage fileStorage,
    DatabaseOptions databaseOptions,
    TimeProvider timeProvider)
  {
  }
}
