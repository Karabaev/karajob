using Karajob.Application.Companies;
using Karajob.Application.Hypotheses;
using Karajob.Application.Vacancies;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Hypotheses;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Vacancies;

public abstract class VacancyServiceFixture : IAsyncLifetime
{
  protected const string DefaultUrl = "https://example.com/jobs/1";

  protected static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  internal FixedTimeProvider Clock { get; } = new(Now);
  protected SqliteTestDatabase Database { get; private set; } = null!;
  protected VacancyService Service { get; private set; } = null!;
  protected CompanyService Companies { get; private set; } = null!;
  protected HypothesisService Hypotheses { get; private set; } = null!;

  protected static VacancyInput Input(
    string url = DefaultUrl,
    Guid? companyId = null,
    Guid? roleId = null,
    Guid? locationId = null,
    Guid? languageId = null,
    WorkFormat? workFormat = null,
    long? salaryMin = null,
    long? salaryMax = null,
    string? salaryCurrency = null,
    IReadOnlyList<Guid>? hypothesisIds = null,
    string? description = null,
    string? notes = null)
  {
    return new VacancyInput(
      url,
      companyId,
      roleId,
      locationId,
      languageId,
      workFormat,
      salaryMin,
      salaryMax,
      salaryCurrency,
      hypothesisIds ?? [],
      description,
      notes);
  }

  protected VacancyService ServiceFor(Guid userId)
  {
    return TestDbContextFactory.CreateVacancyService(Database, userId, Clock);
  }

  protected async Task<Guid> CreateCompanyAsync(string slug = "pixel-forge")
  {
    var company = await Companies.CreateAsync(new CompanyInput(slug, slug, null, null, [], []));
    return company.Id;
  }

  protected async Task<Guid> CreateHypothesisAsync(string name = "Unity Developer RU", Guid? userId = null)
  {
    var service = userId is null
      ? Hypotheses
      : TestDbContextFactory.CreateHypothesisService(Database, userId.Value, Clock);
    var hypothesis = await service.CreateAsync(new HypothesisInput(
      name,
      SeedData.UnityDeveloperRoleId,
      [],
      [],
      WorkFormat.Remote,
      SeedData.RussianLanguageId,
      null,
      null,
      HypothesisStatus.Draft));
    return hypothesis.Id;
  }

  protected async Task MutateAsync(Guid vacancyId, Action<Vacancy> mutate, Guid? userId = null)
  {
    await using var context = Database.CreateContext(userId ?? SeedData.OwnerId);
    var vacancy = await context.Vacancies
      .Include(v => v.Matches)
      .Include(v => v.Events)
      .Include(v => v.Application)
      .SingleAsync(v => v.Id == vacancyId);
    mutate(vacancy);
    await context.SaveChangesAsync();
  }

  protected async Task<Vacancy> LoadAsync(Guid vacancyId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.Vacancies
      .Include(v => v.Matches)
      .Include(v => v.Events)
      .Include(v => v.Application)
      .SingleAsync(v => v.Id == vacancyId);
  }

  protected async Task<int> CountVacanciesAsync()
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    return await context.Vacancies.CountAsync();
  }

  public async Task InitializeAsync()
  {
    Database = await SqliteTestDatabase.CreateAsync();
    Service = ServiceFor(SeedData.OwnerId);
    Companies = TestDbContextFactory.CreateCompanyService(Database, SeedData.OwnerId);
    Hypotheses = TestDbContextFactory.CreateHypothesisService(Database, SeedData.OwnerId, Clock);
  }

  public async Task DisposeAsync()
  {
    await Database.DisposeAsync();
  }
}
