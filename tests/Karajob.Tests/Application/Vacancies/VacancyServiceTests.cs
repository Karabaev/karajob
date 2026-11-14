using Karajob.Application.Common;
using Karajob.Application.Companies;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Normalization;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceTests : VacancyServiceFixture
{
  [Fact]
  public async Task CreateAsync_Valid_CreatesInboxManualVacancyDiscoveredNow()
  {
    var details = await Service.CreateAsync(Input());

    Assert.NotEqual(Guid.Empty, details.Id);
    Assert.Equal(VacancyStage.Inbox, details.Stage);
    Assert.Equal(VacancySource.Manual, details.Source);
    Assert.Equal(Now, details.DiscoveredAt);
    Assert.Null(details.DismissReason);
    Assert.Null(details.ApplicationHypothesisId);
    Assert.Empty(details.Timeline);
    Assert.Empty(details.Similar);
  }

  [Fact]
  public async Task CreateAsync_UrlWithTrackingAndUpperCase_StoresNormalizedUrl()
  {
    const string raw = "https://WWW.Example.com/jobs/1/?utm_source=x#top";

    var details = await Service.CreateAsync(Input(raw));

    Assert.Equal(Normalize.VacancyUrl(raw), details.Url);
  }

  [Fact]
  public async Task CreateAsync_BlankTextFields_StoredAsNull()
  {
    var details = await Service.CreateAsync(Input(description: "   ", notes: ""));

    Assert.Null(details.Description);
    Assert.Null(details.Notes);
  }

  [Fact]
  public async Task CreateAsync_TextFields_AreTrimmed()
  {
    var details = await Service.CreateAsync(Input(description: "  desc  ", notes: "\tnotes\n"));

    Assert.Equal("desc", details.Description);
    Assert.Equal("notes", details.Notes);
  }

  [Fact]
  public async Task CreateAsync_WithCompany_ReturnsCompanyName()
  {
    var company = await Companies.CreateAsync(new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], []));

    var details = await Service.CreateAsync(Input(companyId: company.Id));

    Assert.Equal(company.Id, details.CompanyId);
    Assert.Equal("Pixel Forge", details.CompanyName);
  }

  [Fact]
  public async Task FindAsync_AfterCreateWithAllFields_RoundTripsEverything()
  {
    var companyId = await CreateCompanyAsync();
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(
      companyId: companyId,
      roleId: SeedData.UnityDeveloperRoleId,
      locationId: SeedData.EuLocationId,
      languageId: SeedData.EnglishLanguageId,
      workFormat: WorkFormat.Remote | WorkFormat.Hybrid,
      salaryMin: 3000,
      salaryMax: 5000,
      salaryCurrency: "usd",
      hypothesisIds: [hypothesisId],
      description: "desc",
      notes: "notes"));

    var found = await Service.FindAsync(created.Id);

    Assert.NotNull(found);
    Assert.Equal(created.Id, found.Id);
    Assert.Equal(created.Url, found.Url);
    Assert.Equal(companyId, found.CompanyId);
    Assert.Equal(SeedData.UnityDeveloperRoleId, found.RoleId);
    Assert.Equal(SeedData.EuLocationId, found.LocationId);
    Assert.Equal(SeedData.EnglishLanguageId, found.LanguageId);
    Assert.Equal(WorkFormat.Remote | WorkFormat.Hybrid, found.WorkFormat);
    Assert.Equal(3000, found.SalaryMin);
    Assert.Equal(5000, found.SalaryMax);
    Assert.Equal("USD", found.SalaryCurrency);
    Assert.Equal([hypothesisId], found.HypothesisIds);
    Assert.Equal("desc", found.Description);
    Assert.Equal("notes", found.Notes);
  }

  [Fact]
  public async Task FindAsync_UnknownId_ReturnsNull()
  {
    Assert.Null(await Service.FindAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task FindAsync_AnotherUsersVacancy_ReturnsNull()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var created = await ServiceFor(other.Id).CreateAsync(Input());

    Assert.Null(await Service.FindAsync(created.Id));
  }

  [Fact]
  public async Task CreateAsync_WorkFormatNone_StoredAsNull()
  {
    var created = await Service.CreateAsync(Input(workFormat: WorkFormat.None));

    var found = await Service.FindAsync(created.Id);

    Assert.Null(found!.WorkFormat);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not a url")]
  public async Task CreateAsync_EmptyOrInvalidUrl_FailsWithUrlError(string url)
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(url)));

    Assert.Contains("Url", ex.Errors.Keys);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task CreateAsync_DuplicateUrlAfterNormalization_FailsWithUrlError()
  {
    await Service.CreateAsync(Input("https://example.com/jobs/1"));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.CreateAsync(Input("https://www.EXAMPLE.com/jobs/1/?utm_source=x")));

    Assert.Contains("Url", ex.Errors.Keys);
    Assert.Equal(1, await CountVacanciesAsync());
  }

  [Fact]
  public async Task CreateAsync_SameUrlAsAnotherUser_Succeeds()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(other.Id).CreateAsync(Input());

    var details = await Service.CreateAsync(Input());

    Assert.Equal(Normalize.VacancyUrl(DefaultUrl), details.Url);
  }

  [Fact]
  public async Task CreateAsync_UnknownCompany_FailsWithCompanyIdError()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(companyId: Guid.NewGuid())));

    Assert.Contains("CompanyId", ex.Errors.Keys);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task CreateAsync_UnknownRole_FailsWithRoleIdError()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(roleId: Guid.NewGuid())));

    Assert.Contains("RoleId", ex.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_UnknownLocation_FailsWithLocationIdError()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(locationId: Guid.NewGuid())));

    Assert.Contains("LocationId", ex.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_UnknownLanguage_FailsWithLanguageIdError()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(languageId: Guid.NewGuid())));

    Assert.Contains("LanguageId", ex.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_UnknownHypothesis_FailsWithHypothesisIdsError()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(hypothesisIds: [Guid.NewGuid()])));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_AnotherUsersHypothesis_FailsWithHypothesisIdsError()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await CreateHypothesisAsync("Foreign", other.Id);

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(hypothesisIds: [foreign])));

    Assert.Contains("HypothesisIds", ex.Errors.Keys);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task CreateAsync_NoSalaryValuesButCurrency_SalaryIsNull()
  {
    var details = await Service.CreateAsync(Input(salaryCurrency: "USD"));

    Assert.Null(details.SalaryMin);
    Assert.Null(details.SalaryMax);
    Assert.Null(details.SalaryCurrency);
  }

  [Fact]
  public async Task CreateAsync_OnlySalaryMin_Succeeds()
  {
    var details = await Service.CreateAsync(Input(salaryMin: 3000, salaryCurrency: "EUR"));

    Assert.Equal(3000, details.SalaryMin);
    Assert.Null(details.SalaryMax);
    Assert.Equal("EUR", details.SalaryCurrency);
  }

  [Fact]
  public async Task CreateAsync_OnlySalaryMax_Succeeds()
  {
    var details = await Service.CreateAsync(Input(salaryMax: 6000, salaryCurrency: "EUR"));

    Assert.Null(details.SalaryMin);
    Assert.Equal(6000, details.SalaryMax);
  }

  [Fact]
  public async Task CreateAsync_SalaryMinGreaterThanMax_FailsWithSalaryError()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.CreateAsync(Input(salaryMin: 5000, salaryMax: 3000, salaryCurrency: "USD")));

    Assert.Contains("Salary", ex.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_NegativeSalary_FailsWithSalaryError()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.CreateAsync(Input(salaryMin: -1, salaryCurrency: "USD")));

    Assert.Contains("Salary", ex.Errors.Keys);
  }

  [Theory]
  [InlineData("RU")]
  [InlineData("12$")]
  [InlineData("USDD")]
  [InlineData("")]
  [InlineData(null)]
  public async Task CreateAsync_SalaryWithBadOrMissingCurrency_FailsWithSalaryError(string? currency)
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.CreateAsync(Input(salaryMin: 1000, salaryCurrency: currency)));

    Assert.Contains("Salary", ex.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_LowerCaseCurrency_StoredUpperCase()
  {
    var details = await Service.CreateAsync(Input(salaryMin: 1000, salaryCurrency: "usd"));

    Assert.Equal("USD", details.SalaryCurrency);
  }

  [Fact]
  public async Task CreateAsync_SeveralInvalidFields_CollectsAllErrors()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.CreateAsync(Input("not a url", companyId: Guid.NewGuid(), roleId: Guid.NewGuid(), salaryMin: -5, salaryCurrency: "USD")));

    Assert.Contains("Url", ex.Errors.Keys);
    Assert.Contains("CompanyId", ex.Errors.Keys);
    Assert.Contains("RoleId", ex.Errors.Keys);
    Assert.Contains("Salary", ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_Valid_ChangesFields()
  {
    var created = await Service.CreateAsync(Input(notes: "old"));
    var companyId = await CreateCompanyAsync();

    var updated = await Service.UpdateAsync(created.Id, Input(
      "https://example.com/jobs/2",
      companyId: companyId,
      roleId: SeedData.UnrealDeveloperRoleId,
      salaryMin: 1000,
      salaryMax: 2000,
      salaryCurrency: "EUR",
      notes: "new"));

    var found = await Service.FindAsync(created.Id);
    Assert.Equal(updated.Url, found!.Url);
    Assert.Equal("https://example.com/jobs/2", found.Url);
    Assert.Equal(companyId, found.CompanyId);
    Assert.Equal(SeedData.UnrealDeveloperRoleId, found.RoleId);
    Assert.Equal(1000, found.SalaryMin);
    Assert.Equal("new", found.Notes);
    Assert.Equal(VacancyStage.Inbox, found.Stage);
    Assert.Equal(Now, found.DiscoveredAt);
  }

  [Fact]
  public async Task UpdateAsync_KeepingOwnUrl_IsNotDuplicate()
  {
    var created = await Service.CreateAsync(Input());

    var updated = await Service.UpdateAsync(created.Id, Input(notes: "changed"));

    Assert.Equal("changed", updated.Notes);
  }

  [Fact]
  public async Task UpdateAsync_UrlOfAnotherVacancy_FailsWithUrlError()
  {
    await Service.CreateAsync(Input("https://example.com/jobs/1"));
    var second = await Service.CreateAsync(Input("https://example.com/jobs/2"));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(second.Id, Input("https://example.com/jobs/1")));

    Assert.Contains("Url", ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_UnknownId_Throws()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(Guid.NewGuid(), Input()));
  }

  [Fact]
  public async Task UpdateAsync_AnotherUsersVacancy_Throws()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var foreign = await ServiceFor(other.Id).CreateAsync(Input());

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(foreign.Id, Input(notes: "hack")));
  }

  [Fact]
  public async Task UpdateAsync_Invalid_ChangesNothing()
  {
    var created = await Service.CreateAsync(Input(notes: "keep"));

    await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(created.Id, Input(notes: "lost", roleId: Guid.NewGuid())));

    var found = await Service.FindAsync(created.Id);
    Assert.Equal("keep", found!.Notes);
  }

  [Fact]
  public async Task ListAsync_ReturnsOnlyRequestedStageNewestFirstWithNames()
  {
    var company = await Companies.CreateAsync(new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], []));
    var old = await Service.CreateAsync(Input("https://example.com/jobs/old", companyId: company.Id, roleId: SeedData.UnityDeveloperRoleId));
    Clock.UtcNow = Now.AddDays(1);
    var recent = await Service.CreateAsync(Input("https://example.com/jobs/recent"));
    Clock.UtcNow = Now.AddDays(2);
    var targeted = await Service.CreateAsync(Input("https://example.com/jobs/targeted", hypothesisIds: [await CreateHypothesisAsync()]));
    await MutateAsync(targeted.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddDays(2)));

    var items = await Service.ListAsync(VacancyStage.Inbox);

    Assert.Equal([recent.Id, old.Id], items.Select(i => i.Id));
    Assert.All(items, i => Assert.Equal(VacancyStage.Inbox, i.Stage));
    var oldItem = items.Single(i => i.Id == old.Id);
    Assert.Equal("Pixel Forge", oldItem.CompanyName);
    Assert.NotNull(oldItem.RoleName);
    Assert.Equal(Now, oldItem.DiscoveredAt);
    Assert.Equal(Normalize.VacancyUrl("https://example.com/jobs/old"), oldItem.Url);
    var recentItem = items.Single(i => i.Id == recent.Id);
    Assert.Null(recentItem.CompanyName);
    Assert.Null(recentItem.RoleName);
  }

  [Fact]
  public async Task ListAsync_OtherStage_ReturnsThatStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var created = await Service.CreateAsync(Input(hypothesisIds: [hypothesisId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now));

    var items = await Service.ListAsync(VacancyStage.Targeted);

    Assert.Equal(created.Id, Assert.Single(items).Id);
  }

  [Fact]
  public async Task ListAsync_AnotherUsersVacancies_AreNotReturned()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(other.Id).CreateAsync(Input());

    Assert.Empty(await Service.ListAsync(VacancyStage.Inbox));
  }
}
