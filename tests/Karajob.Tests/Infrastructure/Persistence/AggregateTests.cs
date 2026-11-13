using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class AggregateTests
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Fact]
  public async Task Vacancy_LoadedWithoutInclude_HasApplicationLoaded()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var (vacancy, _, _) = await PersistenceFixtures.AddAppliedVacancyAsync(database, SeedData.OwnerId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.NotNull(loaded.Application);
    Assert.Equal(vacancy.Application!.Id, loaded.Application.Id);
  }

  [Fact]
  public async Task Vacancy_LoadedWithoutInclude_HasMatchesLoaded()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    var vacancy = await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, hypothesis.Id);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    var match = Assert.Single(loaded.Matches);
    Assert.Equal(hypothesis.Id, match.HypothesisId);
  }

  [Fact]
  public async Task Close_LoadedAppliedVacancy_PersistsClosedStageAndOutcome()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var (vacancy, _, _) = await PersistenceFixtures.AddAppliedVacancyAsync(database, SeedData.OwnerId);
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Vacancies.SingleAsync(v => v.Id == vacancy.Id);
      loaded.Close(ApplicationOutcome.Rejected, Moment.AddDays(3));
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var reloaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Equal(VacancyStage.Closed, reloaded.Stage);
    Assert.Equal(ApplicationOutcome.Rejected, reloaded.Application!.Outcome);
  }

  [Fact]
  public async Task MoveTo_ReopeningLoadedClosedVacancy_ClearsPersistedOutcome()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var (vacancy, _, _) = await PersistenceFixtures.AddAppliedVacancyAsync(database, SeedData.OwnerId);
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Vacancies.SingleAsync(v => v.Id == vacancy.Id);
      loaded.Close(ApplicationOutcome.Ghosted, Moment.AddDays(3));
      await context.SaveChangesAsync();
    }

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var closed = await context.Vacancies.SingleAsync(v => v.Id == vacancy.Id);
      closed.MoveTo(VacancyStage.Applied, Moment.AddDays(4));
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var reloaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Equal(VacancyStage.Applied, reloaded.Stage);
    Assert.Null(reloaded.Application!.Outcome);
  }

  [Fact]
  public async Task MoveTo_LoadedVacancy_PersistsNewApplicationEvent()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    var vacancy = await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, hypothesis.Id);
    await using var context = database.CreateContext(SeedData.OwnerId);
    var before = await context.ApplicationEvents.CountAsync(e => e.VacancyId == vacancy.Id);
    var loaded = await context.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    var added = loaded.MoveTo(VacancyStage.Targeted, Moment.AddDays(1));
    await context.SaveChangesAsync();

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var after = await fresh.ApplicationEvents.CountAsync(e => e.VacancyId == vacancy.Id);
    Assert.Equal(before + 1, after);
    Assert.True(await fresh.ApplicationEvents.AnyAsync(e => e.Id == added.Id));
  }

  [Fact]
  public async Task Company_LoadedWithoutInclude_HasDomainsAndAliasesLoaded()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = new Karajob.Domain.Companies.Company("agg-co", "Agg Co");
    company.AddDomain("agg.example.com");
    company.AddAlias("Agg");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Companies.Add(company);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Companies.SingleAsync(c => c.Id == company.Id);

    Assert.Single(loaded.Domains);
    Assert.Single(loaded.Aliases);
  }

  [Fact]
  public async Task AddDomain_ExistingDomainOnLoadedCompany_DoesNotCreateDuplicate()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = new Karajob.Domain.Companies.Company("dup-co", "Dup Co");
    company.AddDomain("dup.example.com");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Companies.Add(company);
      await context.SaveChangesAsync();
    }

    await using var second = database.CreateContext(SeedData.OwnerId);
    var loaded = await second.Companies.SingleAsync(c => c.Id == company.Id);
    loaded.AddDomain("dup.example.com");
    await second.SaveChangesAsync();

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    Assert.Equal(1, await fresh.CompanyDomains.CountAsync(d => d.CompanyId == company.Id));
  }

  [Fact]
  public async Task Apply_LoadedVacancy_PersistsNewApplication()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    var (_, cvVersion) = await PersistenceFixtures.AddCvAsync(database, SeedData.OwnerId);
    var vacancy = await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, hypothesis.Id);
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Vacancies.SingleAsync(v => v.Id == vacancy.Id);
      loaded.MoveTo(VacancyStage.Targeted, Moment);
      loaded.Apply(hypothesis.Id, cvVersion.Id, ApplicationChannel.Referral, Moment.AddDays(1), null);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var reloaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Equal(VacancyStage.Applied, reloaded.Stage);
    Assert.Equal(cvVersion.Id, reloaded.Application!.CvVersionId);
  }

  [Fact]
  public async Task SetMatch_NewHypothesisOnLoadedVacancy_PersistsMatch()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var first = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId, "First");
    var second = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId, "Second");
    var vacancy = await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, first.Id);
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Vacancies.SingleAsync(v => v.Id == vacancy.Id);
      loaded.SetMatch(second.Id, 50, null, MatchAuthor.Ai);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var reloaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Equal(2, reloaded.Matches.Count);
  }

  [Fact]
  public async Task RemoveMatch_LoadedVacancy_DeletesMatchRow()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    var vacancy = await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, hypothesis.Id);
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Vacancies.SingleAsync(v => v.Id == vacancy.Id);
      loaded.RemoveMatch(hypothesis.Id);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);

    Assert.False(await fresh.VacancyMatches.AnyAsync(m => m.VacancyId == vacancy.Id));
  }

  [Fact]
  public async Task AddDomain_NewDomainOnLoadedCompany_PersistsDomain()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = await PersistenceFixtures.AddCompanyAsync(database, "grow-co");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Companies.SingleAsync(c => c.Id == company.Id);
      loaded.AddDomain("grow.example.com");
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var reloaded = await fresh.Companies.SingleAsync(c => c.Id == company.Id);

    Assert.Equal("grow.example.com", Assert.Single(reloaded.Domains).Value);
  }

  [Fact]
  public async Task RemoveDomainAndAlias_LoadedCompany_DeletesRows()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var company = new Karajob.Domain.Companies.Company("shrink-co", "Shrink Co");
    company.AddDomain("shrink.example.com");
    company.AddAlias("Shrink");
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Companies.Add(company);
      await context.SaveChangesAsync();
    }

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Companies.SingleAsync(c => c.Id == company.Id);
      loaded.RemoveDomain("shrink.example.com");
      loaded.RemoveAlias("Shrink");
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);

    Assert.False(await fresh.CompanyDomains.AnyAsync(d => d.CompanyId == company.Id));
    Assert.False(await fresh.CompanyAliases.AnyAsync(a => a.CompanyId == company.Id));
  }
}
