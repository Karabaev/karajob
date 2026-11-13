using Karajob.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Infrastructure.Persistence;

public class KarajobDbContextFactory(IDbContextFactory<KarajobDbContext> innerFactory) : IKarajobDbContextFactory
{
  public async Task<IKarajobDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
  {
    return await innerFactory.CreateDbContextAsync(cancellationToken);
  }
}
