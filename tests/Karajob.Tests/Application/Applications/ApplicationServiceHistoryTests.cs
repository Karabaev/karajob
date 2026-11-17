using Karajob.Application.Applications;
using Karajob.Application.Companies;
using Karajob.Domain.Applications;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application.Vacancies;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Applications;

public class ApplicationServiceHistoryTests : VacancyServiceFixture
{
  private static readonly ApplicationHistoryFilter NoFilter = new(null, null, null);

  private ApplicationService HistoryFor(Guid userId)
  {
    return TestDbContextFactory.CreateApplicationService(Database, userId);
  }

  private Task<IReadOnlyList<ApplicationHistoryItem>> ListAsync(ApplicationHistoryFilter? filter = null)
  {
    return HistoryFor(SeedData.OwnerId).ListHistoryAsync(filter ?? NoFilter);
  }

  private async Task<Guid> CreateAtAsync(
    VacancyStage stage,
    Guid hypothesisId,
    string url,
    DateTime createdAt,
    ApplicationOutcome outcome = ApplicationOutcome.Rejected)
  {
    Clock.UtcNow = createdAt;
    return await CreateOnBoardAsync(stage, hypothesisId, url, outcome);
  }

  private async Task<Guid> CreateCompanyNamedAsync(string slug, string name)
  {
    var company = await Companies.CreateAsync(new CompanyInput(slug, name, null, null, [], [], []));
    return company.Id;
  }

  [Fact]
  public async Task ListHistoryAsync_NoVacancies_ReturnsEmpty()
  {
    Assert.Empty(await ListAsync());
  }

  [Fact]
  public async Task ListHistoryAsync_MixedStages_ReturnsOnlyVacanciesWithApplication()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await Service.CreateAsync(Input("https://example.com/jobs/inbox"));
    await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId, "https://example.com/jobs/targeted");
    await CreateOnBoardAsync(VacancyStage.Preparing, hypothesisId, "https://example.com/jobs/preparing");
    var dismissed = await CreateOnBoardAsync(VacancyStage.Targeted, hypothesisId, "https://example.com/jobs/dismissed");
    await Service.DismissAsync(dismissed, new Karajob.Application.Vacancies.DismissInput(DismissReason.Other, null));
    var applied = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/applied");
    var interviewing = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/interviewing");
    var closed = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/closed");

    var items = await ListAsync();

    Assert.Equal(
      new[] { applied, interviewing, closed }.Order(),
      items.Select(i => i.VacancyId).Order());
  }

  [Fact]
  public async Task ListHistoryAsync_Vacancies_AreOrderedByAppliedAtDescending()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var oldest = await CreateAtAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1", Now);
    var newest = await CreateAtAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/2", Now.AddDays(5));
    var middle = await CreateAtAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/3", Now.AddDays(2));

    var items = await ListAsync();

    Assert.Equal([newest, middle, oldest], items.Select(i => i.VacancyId));
  }

  [Fact]
  public async Task ListHistoryAsync_AnotherUsersApplications_AreNotReturned()
  {
    var otherUserId = (await PersistenceFixtures.AddUserAsync(Database)).Id;
    var otherHypothesis = await CreateHypothesisAsync("Theirs", otherUserId);
    var otherCv = await CreateCvAsync("Their CV", "their.pdf", otherUserId);
    var theirs = await ServiceFor(otherUserId).CreateAsync(Input("https://example.com/jobs/theirs", hypothesisIds: [otherHypothesis]));
    await MutateAsync(theirs.Id, v =>
    {
      v.MoveTo(VacancyStage.Targeted, Now);
      v.Apply(otherHypothesis, otherCv.VersionId, ApplicationChannel.Website, Now, null);
    }, otherUserId);
    var hypothesisId = await CreateHypothesisAsync();
    var mine = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/mine");

    var items = await ListAsync();

    Assert.Equal(mine, Assert.Single(items).VacancyId);
    Assert.Equal(theirs.Id, Assert.Single(await HistoryFor(otherUserId).ListHistoryAsync(NoFilter)).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_Item_CarriesApplicationDetails()
  {
    var hypothesisId = await CreateHypothesisAsync("Unity Developer RU");
    var companyId = await CreateCompanyNamedAsync("pixel-forge", "Pixel Forge");
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    await MutateAsync(id, v =>
    {
      v.CompanyId = companyId;
      v.RoleId = SeedData.UnityDeveloperRoleId;
      v.Application!.CoverLetter = "Hello there";
    });
    var vacancy = await LoadAsync(id);

    var item = Assert.Single(await ListAsync());

    Assert.Equal(id, item.VacancyId);
    Assert.Equal(vacancy.Url, item.Url);
    Assert.Equal("Pixel Forge", item.CompanyName);
    Assert.Equal("Unity Developer", item.RoleName);
    Assert.Equal(VacancyStage.Applied, item.Stage);
    Assert.Null(item.Outcome);
    Assert.Equal(hypothesisId, item.HypothesisId);
    Assert.Equal("Unity Developer RU", item.HypothesisName);
    Assert.StartsWith("CV for", item.CvName);
    Assert.Equal("board.pdf", item.CvFileName);
    Assert.Equal(ApplicationChannel.Website, item.Channel);
    Assert.Equal(vacancy.Application!.AppliedAt, item.AppliedAt);
    Assert.Equal("Hello there", item.CoverLetter);
  }

  [Fact]
  public async Task ListHistoryAsync_VacancyWithoutCompanyAndRole_HasNullNames()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");

    var item = Assert.Single(await ListAsync());

    Assert.Null(item.CompanyName);
    Assert.Null(item.RoleName);
    Assert.Null(item.CoverLetter);
  }

  [Fact]
  public async Task ListHistoryAsync_ClosedVacancy_CarriesOutcomeClosedAtAndClosedFromStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/1");
    var closedAt = Now.AddDays(10);
    await MutateAsync(id, v => v.Close(ApplicationOutcome.Offer, closedAt));

    var item = Assert.Single(await ListAsync());

    Assert.Equal(VacancyStage.Closed, item.Stage);
    Assert.Equal(ApplicationOutcome.Offer, item.Outcome);
    Assert.Equal(closedAt, item.ClosedAt);
    Assert.Equal(VacancyStage.Interviewing, item.ClosedFromStage);
  }

  [Theory]
  [InlineData(VacancyStage.Applied)]
  [InlineData(VacancyStage.Interviewing)]
  public async Task ListHistoryAsync_OpenVacancy_HasNoClosedAtAndClosedFromStage(VacancyStage stage)
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(stage, hypothesisId, "https://example.com/jobs/1");

    var item = Assert.Single(await ListAsync());

    Assert.Null(item.ClosedAt);
    Assert.Null(item.ClosedFromStage);
    Assert.Null(item.Outcome);
  }

  [Fact]
  public async Task ListHistoryAsync_ReopenedAndClosedAgain_UsesLatestCloseEvent()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    await MutateAsync(id, v =>
    {
      v.Close(ApplicationOutcome.Rejected, Now.AddDays(3));
      v.Reopen(VacancyStage.Interviewing, Now.AddDays(4));
      v.Close(ApplicationOutcome.Offer, Now.AddDays(8));
    });

    var item = Assert.Single(await ListAsync());

    Assert.Equal(Now.AddDays(8), item.ClosedAt);
    Assert.Equal(VacancyStage.Interviewing, item.ClosedFromStage);
    Assert.Equal(ApplicationOutcome.Offer, item.Outcome);
  }

  [Fact]
  public async Task ListHistoryAsync_StageFilter_ReturnsOnlyThatStage()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var interviewing = await CreateOnBoardAsync(VacancyStage.Interviewing, hypothesisId, "https://example.com/jobs/2");
    await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/3");

    var items = await ListAsync(new ApplicationHistoryFilter(VacancyStage.Interviewing, null, null));

    Assert.Equal(interviewing, Assert.Single(items).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_OutcomeFilter_ReturnsOnlyThatOutcome()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var offer = await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/2", ApplicationOutcome.Offer);
    await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/3", ApplicationOutcome.Rejected);

    var items = await ListAsync(new ApplicationHistoryFilter(null, ApplicationOutcome.Offer, null));

    Assert.Equal(offer, Assert.Single(items).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_StageAndOutcomeFilters_AreCombined()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/1", ApplicationOutcome.Rejected);

    var items = await ListAsync(new ApplicationHistoryFilter(VacancyStage.Applied, ApplicationOutcome.Rejected, null));

    Assert.Empty(items);
  }

  [Fact]
  public async Task ListHistoryAsync_SearchByCompanyName_IsCaseInsensitiveSubstring()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var pixel = await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/1", "pixel-forge", "Pixel Forge");
    await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/2", "acme", "Acme Corp");

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, "xEL fo"));

    Assert.Equal(pixel, Assert.Single(items).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_SearchByCompanySlug_FindsVacancy()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var vacancy = await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/1", "zeta-games", "Totally Different");
    await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/2", "acme", "Acme Corp");

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, "ZETA-gam"));

    Assert.Equal(vacancy, Assert.Single(items).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_SearchByRoleName_FindsVacancy()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var unity = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    var unreal = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/2");
    await MutateAsync(unity, v => v.RoleId = SeedData.UnityDeveloperRoleId);
    await MutateAsync(unreal, v => v.RoleId = SeedData.UnrealDeveloperRoleId);

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, "UNREAL"));

    Assert.Equal(unreal, Assert.Single(items).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_SearchWithCyrillic_MatchesIgnoringCase()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var pixel = await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/1", "piksel", "Пиксель");
    await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/2", "acme", "Acme Corp");

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, "пикс"));

    Assert.Equal(pixel, Assert.Single(items).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_SearchIsTrimmed()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var pixel = await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/1", "pixel-forge", "Pixel Forge");
    await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/2", "acme", "Acme Corp");

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, "  pixel  "));

    Assert.Equal(pixel, Assert.Single(items).VacancyId);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task ListHistoryAsync_BlankSearch_DoesNotFilter(string? search)
  {
    var hypothesisId = await CreateHypothesisAsync();
    await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/1");
    await CreateOnBoardAsync(VacancyStage.Closed, hypothesisId, "https://example.com/jobs/2");

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, search));

    Assert.Equal(2, items.Count);
  }

  [Fact]
  public async Task ListHistoryAsync_SearchByUrl_FindsVacancyWithoutCompany()
  {
    var hypothesisId = await CreateHypothesisAsync();
    var hh = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://hh.ru/vacancy/1");
    await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, "https://example.com/jobs/2");

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, "HH.RU"));

    Assert.Equal(hh, Assert.Single(items).VacancyId);
  }

  [Fact]
  public async Task ListHistoryAsync_SearchWithNoMatch_ReturnsEmpty()
  {
    var hypothesisId = await CreateHypothesisAsync();
    await AppliedWithCompanyAsync(hypothesisId, "https://example.com/jobs/1", "pixel-forge", "Pixel Forge");

    var items = await ListAsync(new ApplicationHistoryFilter(null, null, "nonexistent"));

    Assert.Empty(items);
  }

  private async Task<Guid> AppliedWithCompanyAsync(Guid hypothesisId, string url, string slug, string name)
  {
    var companyId = await CreateCompanyNamedAsync(slug, name);
    var id = await CreateOnBoardAsync(VacancyStage.Applied, hypothesisId, url);
    await MutateAsync(id, v => v.CompanyId = companyId);
    return id;
  }
}
