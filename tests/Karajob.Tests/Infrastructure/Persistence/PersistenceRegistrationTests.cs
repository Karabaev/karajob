using Karajob.Application.Persistence;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Infrastructure.Persistence;

public class PersistenceRegistrationTests
{
  [Fact]
  public void AddKarajobPersistence_UnknownProvider_ThrowsInvalidOperationException()
  {
    var options = new DatabaseOptions { Provider = "Postgres", ConnectionString = "Host=localhost" };

    Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddKarajobPersistence(options));
  }

  [Fact]
  public void AddKarajobPersistence_BlankConnectionString_ThrowsArgumentException()
  {
    var options = new DatabaseOptions { Provider = DatabaseOptions.SqliteProvider, ConnectionString = " " };

    Assert.Throws<ArgumentException>(() => new ServiceCollection().AddKarajobPersistence(options));
  }

  [Fact]
  public void AddKarajobPersistence_InMemoryWithoutConnectionString_ResolvesContextFactory()
  {
    var options = new DatabaseOptions { Provider = DatabaseOptions.SqliteInMemoryProvider };
    var services = new ServiceCollection();
    services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    services.AddKarajobPersistence(options);
    using var provider = services.BuildServiceProvider();

    var factory = provider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    using var context = factory.CreateDbContext();

    Assert.Contains(context.Database.GetMigrations(), m => m.EndsWith("_Initial", StringComparison.Ordinal));
  }

  [Fact]
  public void AddKarajobPersistence_SqliteProvider_ResolvesContextFactoryWithMigrations()
  {
    var options = new DatabaseOptions { Provider = DatabaseOptions.SqliteProvider, ConnectionString = "DataSource=:memory:" };
    var services = new ServiceCollection();
    services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    services.AddKarajobPersistence(options);
    using var provider = services.BuildServiceProvider();

    var factory = provider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    using var context = factory.CreateDbContext();

    Assert.Contains(context.Database.GetMigrations(), m => m.EndsWith("_Initial", StringComparison.Ordinal));
  }

  [Fact]
  public async Task AddKarajobPersistence_SqliteInMemory_ResolvesApplicationContextFactoryWhoseContextIsUsable()
  {
    var options = new DatabaseOptions { Provider = DatabaseOptions.SqliteInMemoryProvider };
    var services = new ServiceCollection();
    services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    services.AddKarajobPersistence(options);
    await using var provider = services.BuildServiceProvider();

    var factory = provider.GetRequiredService<IKarajobDbContextFactory>();
    await using var context = await factory.CreateDbContextAsync();

    var concrete = Assert.IsType<KarajobDbContext>(context);
    await concrete.Database.MigrateAsync();
    Assert.Empty(await context.Companies.ToListAsync());
  }
}
