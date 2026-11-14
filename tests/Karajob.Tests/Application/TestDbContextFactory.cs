using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Hypotheses;
using Karajob.Application.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application;

internal class TestDbContextFactory(SqliteTestDatabase database, Guid userId) : IKarajobDbContextFactory
{
  public static CompanyService CreateCompanyService(SqliteTestDatabase database, Guid userId)
  {
    return new CompanyService(new TestDbContextFactory(database, userId), new FixedCurrentUser(userId));
  }

  public static CatalogService CreateCatalogService(SqliteTestDatabase database, Guid userId)
  {
    return new CatalogService(new TestDbContextFactory(database, userId));
  }

  public static HypothesisService CreateHypothesisService(SqliteTestDatabase database, Guid userId, TimeProvider timeProvider)
  {
    return new HypothesisService(new TestDbContextFactory(database, userId), new FixedCurrentUser(userId), timeProvider);
  }

  public Task<IKarajobDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IKarajobDbContext>(database.CreateContext(userId));
  }
}
