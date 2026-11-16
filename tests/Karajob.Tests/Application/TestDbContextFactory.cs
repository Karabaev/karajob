using Karajob.Application.Applications;
using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Cvs;
using Karajob.Application.Files;
using Karajob.Application.Goals;
using Karajob.Application.Hypotheses;
using Karajob.Application.MasterProfiles;
using Karajob.Application.Metrics;
using Karajob.Application.Persistence;
using Karajob.Application.Resources;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Infrastructure.Users;
using Karajob.Tests.Application.Vacancies;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karajob.Tests.Application;

internal class TestDbContextFactory(SqliteTestDatabase database, Guid userId) : IKarajobDbContextFactory
{
  public static CompanyService CreateCompanyService(SqliteTestDatabase database, Guid userId)
  {
    return new CompanyService(new TestDbContextFactory(database, userId), new FixedCurrentUser(userId), TestLocalizer.For<CompanyMessages>());
  }

  public static CatalogService CreateCatalogService(SqliteTestDatabase database, Guid userId)
  {
    return new CatalogService(new TestDbContextFactory(database, userId), TestLocalizer.For<CatalogMessages>());
  }

  public static HypothesisService CreateHypothesisService(SqliteTestDatabase database, Guid userId, TimeProvider timeProvider)
  {
    return new HypothesisService(new TestDbContextFactory(database, userId), new FixedCurrentUser(userId), timeProvider, TestLocalizer.For<HypothesisMessages>());
  }

  public static CvService CreateCvService(
    SqliteTestDatabase database,
    Guid userId,
    IFileStorage fileStorage,
    TimeProvider timeProvider,
    CvOptions options)
  {
    return new CvService(new TestDbContextFactory(database, userId), new FixedCurrentUser(userId), fileStorage, timeProvider, options, NullLogger<CvService>.Instance, TestLocalizer.For<CvMessages>());
  }

  public static MasterProfileService CreateMasterProfileService(SqliteTestDatabase database, Guid userId, TimeProvider timeProvider)
  {
    return new MasterProfileService(new TestDbContextFactory(database, userId), new FixedCurrentUser(userId), timeProvider, TestLocalizer.For<MasterProfileMessages>());
  }

  public static VacancyService CreateVacancyService(
    SqliteTestDatabase database,
    Guid userId,
    TimeProvider timeProvider,
    IPageTitleReader? titleReader = null,
    TimeZoneInfo? timeZone = null)
  {
    return new VacancyService(
      new TestDbContextFactory(database, userId),
      new FixedCurrentUser(userId, timeZone),
      timeProvider,
      titleReader ?? new StubPageTitleReader(),
      TestLocalizer.For<VacancyMessages>());
  }

  public static ApplicationService CreateApplicationService(SqliteTestDatabase database, Guid userId)
  {
    return new ApplicationService(new TestDbContextFactory(database, userId));
  }

  public static UserService CreateUserService(SqliteTestDatabase database, Guid userId, TimeProvider timeProvider)
  {
    return new UserService(new TestDbContextFactory(database, userId), timeProvider, TestLocalizer.For<UserMessages>());
  }

  public static SearchGoalService CreateSearchGoalService(SqliteTestDatabase database, Guid userId)
  {
    return new SearchGoalService(new TestDbContextFactory(database, userId), TestLocalizer.For<GoalMessages>());
  }

  public static HypothesisMetricsService CreateHypothesisMetricsService(SqliteTestDatabase database, Guid userId, TimeProvider timeProvider, TimeZoneInfo? timeZone = null)
  {
    return new HypothesisMetricsService(new TestDbContextFactory(database, userId), new FixedCurrentUser(userId, timeZone), timeProvider);
  }

  public Task<IKarajobDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IKarajobDbContext>(database.CreateContext(userId));
  }
}
