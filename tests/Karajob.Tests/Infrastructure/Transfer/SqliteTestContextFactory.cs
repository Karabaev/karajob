using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Transfer;

internal class SqliteTestContextFactory(SqliteTestDatabase database, Guid userId) : IDbContextFactory<KarajobDbContext>
{
  public KarajobDbContext CreateDbContext()
  {
    return database.CreateContext(userId);
  }
}
