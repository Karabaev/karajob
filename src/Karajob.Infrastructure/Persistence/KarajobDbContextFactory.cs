using Karajob.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Infrastructure.Persistence;

public class KarajobDbContextFactory(IDbContextFactory<KarajobDbContext> innerFactory) : IKarajobDbContextFactory
{
  public Task<IKarajobDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
