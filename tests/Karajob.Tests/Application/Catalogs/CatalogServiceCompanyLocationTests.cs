using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Application.Companies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Catalogs;

public class CatalogServiceCompanyLocationTests : CatalogServiceFixture
{
  [Fact]
  public async Task DeleteAsync_LocationUsedByCompany_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddCompanyWithLocationAsync(SeedData.OwnerId, SeedData.EuLocationId);

    await AssertDeleteBlockedAsync(SeedData.EuLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedByCompanyCreatedByAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddCompanyWithLocationAsync(other.Id, SeedData.SerbiaLocationId);

    await AssertDeleteBlockedAsync(SeedData.SerbiaLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LocationNoLongerUsedByCompany_RemovesIt()
  {
    var companies = TestDbContextFactory.CreateCompanyService(Database, SeedData.OwnerId);
    var company = await companies.CreateAsync(Input(SeedData.UsaLocationId));
    await companies.DeleteAsync(company.Id);

    await Service.DeleteAsync(CatalogKind.Location, SeedData.UsaLocationId);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Location), i => i.Id == SeedData.UsaLocationId);
  }

  private static CompanyInput Input(Guid locationId)
  {
    return new CompanyInput("pixel-forge", "Pixel Forge", null, null, [], [], [locationId]);
  }

  private async Task AddCompanyWithLocationAsync(Guid userId, Guid locationId)
  {
    var companies = TestDbContextFactory.CreateCompanyService(Database, userId);
    await companies.CreateAsync(Input(locationId));
  }

  private async Task AssertDeleteBlockedAsync(Guid id)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(CatalogKind.Location, id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Contains(await Service.ListAsync(CatalogKind.Location), i => i.Id == id);
  }
}
