using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceSimilarTests : VacancyServiceFixture
{
  private async Task<Guid> CreateAtAsync(
    DateTime discoveredAt,
    string path,
    Guid? companyId,
    Guid? roleId,
    VacancyService? service = null)
  {
    Clock.UtcNow = discoveredAt;
    var created = await (service ?? Service).CreateAsync(Input($"https://example.com/jobs/{path}", companyId: companyId, roleId: roleId));
    return created.Id;
  }

  [Fact]
  public async Task FindSimilarAsync_SameCompanyAndRoleWithinWindow_ReturnsVacancy()
  {
    var companyId = await CreateCompanyAsync();
    var existing = await CreateAtAsync(Now.AddDays(-10), "a", companyId, SeedData.UnityDeveloperRoleId);
    Clock.UtcNow = Now;

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, null);

    var item = Assert.Single(similar);
    Assert.Equal(existing, item.Id);
    Assert.Equal(VacancyStage.Inbox, item.Stage);
    Assert.Equal(Now.AddDays(-10), item.DiscoveredAt);
    Assert.Equal(Karajob.Domain.Normalization.Normalize.VacancyUrl("https://example.com/jobs/a"), item.Url);
  }

  [Fact]
  public async Task FindSimilarAsync_ExactlyThirtyDaysBeforeClockNow_IsIncluded()
  {
    var companyId = await CreateCompanyAsync();
    var existing = await CreateAtAsync(Now.AddDays(-30), "a", companyId, SeedData.UnityDeveloperRoleId);
    Clock.UtcNow = Now;

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, null);

    Assert.Equal(existing, Assert.Single(similar).Id);
  }

  [Fact]
  public async Task FindSimilarAsync_ThirtyOneDaysBeforeClockNow_IsExcluded()
  {
    var companyId = await CreateCompanyAsync();
    await CreateAtAsync(Now.AddDays(-31), "a", companyId, SeedData.UnityDeveloperRoleId);
    Clock.UtcNow = Now;

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, null);

    Assert.Empty(similar);
  }

  [Fact]
  public async Task FindSimilarAsync_WithExcludeId_UsesExcludedVacancyDiscoveredAtAsReference()
  {
    var companyId = await CreateCompanyAsync();
    var reference = await CreateAtAsync(Now.AddDays(-100), "ref", companyId, SeedData.UnityDeveloperRoleId);
    var near = await CreateAtAsync(Now.AddDays(-70), "near", companyId, SeedData.UnityDeveloperRoleId);
    await CreateAtAsync(Now.AddDays(-69), "far", companyId, SeedData.UnityDeveloperRoleId);
    Clock.UtcNow = Now;

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, reference);

    Assert.Equal(near, Assert.Single(similar).Id);
  }

  [Fact]
  public async Task FindSimilarAsync_ThirtyOneDaysAfterReference_IsExcluded()
  {
    var companyId = await CreateCompanyAsync();
    var reference = await CreateAtAsync(Now.AddDays(-60), "ref", companyId, SeedData.UnityDeveloperRoleId);
    await CreateAtAsync(Now.AddDays(-29), "after", companyId, SeedData.UnityDeveloperRoleId);
    var inside = await CreateAtAsync(Now.AddDays(-30), "inside", companyId, SeedData.UnityDeveloperRoleId);

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, reference);

    Assert.Equal(inside, Assert.Single(similar).Id);
  }

  [Fact]
  public async Task FindSimilarAsync_ThirtyOneDaysBeforeReference_IsExcluded()
  {
    var companyId = await CreateCompanyAsync();
    await CreateAtAsync(Now.AddDays(-61), "before", companyId, SeedData.UnityDeveloperRoleId);
    var reference = await CreateAtAsync(Now.AddDays(-30), "ref", companyId, SeedData.UnityDeveloperRoleId);

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, reference);

    Assert.Empty(similar);
  }

  [Fact]
  public async Task FindSimilarAsync_ExcludesTheVacancyItself()
  {
    var companyId = await CreateCompanyAsync();
    var reference = await CreateAtAsync(Now, "ref", companyId, SeedData.UnityDeveloperRoleId);

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, reference);

    Assert.Empty(similar);
  }

  [Fact]
  public async Task FindSimilarAsync_OtherCompanyOrOtherRole_AreNotIncluded()
  {
    var companyId = await CreateCompanyAsync("pixel-forge");
    var otherCompanyId = await CreateCompanyAsync("other-studio");
    await CreateAtAsync(Now, "other-company", otherCompanyId, SeedData.UnityDeveloperRoleId);
    await CreateAtAsync(Now, "other-role", companyId, SeedData.UnrealDeveloperRoleId);
    await CreateAtAsync(Now, "no-role", companyId, null);

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, null);

    Assert.Empty(similar);
  }

  [Fact]
  public async Task FindSimilarAsync_AnotherUsersVacancy_IsNotIncluded()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var companyId = await CreateCompanyAsync();
    await CreateAtAsync(Now, "foreign", companyId, SeedData.UnityDeveloperRoleId, ServiceFor(other.Id));

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, null);

    Assert.Empty(similar);
  }

  [Fact]
  public async Task FindSimilarAsync_DismissedVacancy_StillCounts()
  {
    var companyId = await CreateCompanyAsync();
    var dismissed = await CreateAtAsync(Now, "dismissed", companyId, SeedData.UnityDeveloperRoleId);
    await MutateAsync(dismissed, v => v.Dismiss(DismissReason.WrongStack, null, Now));

    var similar = await Service.FindSimilarAsync(companyId, SeedData.UnityDeveloperRoleId, null);

    var item = Assert.Single(similar);
    Assert.Equal(VacancyStage.Dismissed, item.Stage);
  }

  [Fact]
  public async Task FindAsync_VacancyWithCompanyAndRole_ListsSimilarOnes()
  {
    var companyId = await CreateCompanyAsync();
    var first = await CreateAtAsync(Now.AddDays(-5), "first", companyId, SeedData.UnityDeveloperRoleId);
    var second = await CreateAtAsync(Now, "second", companyId, SeedData.UnityDeveloperRoleId);

    var details = await Service.FindAsync(second);

    Assert.Equal(first, Assert.Single(details!.Similar).Id);
  }

  [Fact]
  public async Task FindAsync_VacancyWithoutCompanyOrRole_HasNoSimilar()
  {
    var companyId = await CreateCompanyAsync();
    await CreateAtAsync(Now, "first", companyId, SeedData.UnityDeveloperRoleId);
    var noRole = await CreateAtAsync(Now, "no-role", companyId, null);
    var noCompany = await CreateAtAsync(Now, "no-company", null, SeedData.UnityDeveloperRoleId);

    Assert.Empty((await Service.FindAsync(noRole))!.Similar);
    Assert.Empty((await Service.FindAsync(noCompany))!.Similar);
  }
}
