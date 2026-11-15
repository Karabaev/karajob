using Karajob.Application.Goals;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Goals;

public abstract class SearchGoalServiceFixture : IAsyncLifetime
{
  protected static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected SearchGoalService Service { get; private set; } = null!;

  protected static SearchGoalInput Input(
    Guid? roleId = null,
    IReadOnlyList<Guid>? locationIds = null,
    string? industries = null,
    string? mustHave = null,
    int weeklyApplicationTarget = 10,
    DateOnly? deadline = null,
    long? desiredMonthlyIncome = null,
    string? incomeCurrency = null,
    int marketThresholdPerWeek = 10,
    int conversionThresholdPermille = 30,
    int minSampleSize = 30,
    int maturationDays = 14)
  {
    return new SearchGoalInput(
      roleId,
      locationIds ?? [],
      industries,
      mustHave,
      weeklyApplicationTarget,
      deadline,
      desiredMonthlyIncome,
      incomeCurrency,
      marketThresholdPerWeek,
      conversionThresholdPermille,
      minSampleSize,
      maturationDays);
  }

  protected SearchGoalService ServiceFor(Guid userId)
  {
    return TestDbContextFactory.CreateSearchGoalService(Database, userId);
  }

  protected async Task<Guid> CreateProfileAsync()
  {
    var users = TestDbContextFactory.CreateUserService(Database, SeedData.OwnerId, new FixedTimeProvider(Now));
    var profile = await users.CreateAsync(new UserInput("Second", "#26a69a"));
    return profile.Id;
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
