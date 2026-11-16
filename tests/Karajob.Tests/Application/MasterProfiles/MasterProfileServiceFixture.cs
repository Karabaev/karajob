using Karajob.Application.MasterProfiles;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.MasterProfiles;

public abstract class MasterProfileServiceFixture : IAsyncLifetime
{
  protected static readonly DateTime Now = new(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

  protected SqliteTestDatabase Database { get; private set; } = null!;
  internal FixedTimeProvider Clock { get; } = new(Now);
  protected MasterProfileService Service { get; private set; } = null!;

  protected MasterProfileService ServiceFor(Guid userId)
  {
    return TestDbContextFactory.CreateMasterProfileService(Database, userId, Clock);
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = ServiceFor(SeedData.OwnerId);
  }

  public async Task DisposeAsync()
  {
    await Database.DisposeAsync();
  }
}
