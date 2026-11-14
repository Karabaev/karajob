using Karajob.Application.Catalogs;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Catalogs;

public abstract class CatalogServiceFixture : IAsyncLifetime
{
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected CatalogService Service { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
  }

  public async Task DisposeAsync()
  {
    await Database.DisposeAsync();
  }
}
