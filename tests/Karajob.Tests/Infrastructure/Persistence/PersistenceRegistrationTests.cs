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
  public void AddKarajobPersistence_SqliteProvider_ResolvesContextFactory()
  {
    var options = new DatabaseOptions { Provider = DatabaseOptions.SqliteProvider, ConnectionString = "DataSource=:memory:" };
    var services = new ServiceCollection();
    services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
    services.AddKarajobPersistence(options);
    using var provider = services.BuildServiceProvider();

    var factory = provider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    using var context = factory.CreateDbContext();

    Assert.NotNull(context);
  }
}
