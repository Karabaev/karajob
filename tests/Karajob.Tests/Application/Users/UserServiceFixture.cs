using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Users;

public abstract class UserServiceFixture : IAsyncLifetime
{
  protected static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

  protected SqliteTestDatabase Database { get; private set; } = null!;
  internal FixedTimeProvider Clock { get; } = new(Now);
  protected UserService Service { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = TestDbContextFactory.CreateUserService(Database, SeedData.OwnerId, Clock);
  }

  public async Task DisposeAsync()
  {
    await Database.DisposeAsync();
  }
}
