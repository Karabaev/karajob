using Karajob.Application.Common;
using Karajob.Application.Companies;
using Karajob.Domain.Companies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Companies;

public class CompanyServiceLocationTests : CompanyServiceFixture
{
  [Fact]
  public async Task CreateAsync_WithLocations_ReturnsAndPersistsLocationIds()
  {
    var created = await Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId, SeedData.SerbiaLocationId] });

    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.SerbiaLocationId }.Order(), created.LocationIds.Order());
    var found = await Service.FindBySlugAsync("pixel-forge");
    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.SerbiaLocationId }.Order(), found!.LocationIds.Order());
  }

  [Fact]
  public async Task CreateAsync_NullLocationIds_CreatesCompanyWithoutLocations()
  {
    var created = await Service.CreateAsync(Input());

    Assert.Empty(created.LocationIds);
  }

  [Fact]
  public async Task CreateAsync_DuplicateLocationIds_StoresEachOnce()
  {
    var created = await Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId, SeedData.EuLocationId] });

    Assert.Equal(SeedData.EuLocationId, Assert.Single(created.LocationIds));
  }

  [Fact]
  public async Task CreateAsync_UnknownLocationId_ReportsLocationIdsErrorAndSavesNothing()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId, Guid.NewGuid()] }));

    Assert.Contains(nameof(CompanyInput.LocationIds), exception.Errors.Keys);
    Assert.Null(await Service.FindBySlugAsync("pixel-forge"));
  }

  [Fact]
  public async Task UpdateAsync_ChangedLocations_ReplacesSet()
  {
    var created = await Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId, SeedData.SerbiaLocationId] });

    var updated = await Service.UpdateAsync(created.Id, Input() with { LocationIds = [SeedData.SerbiaLocationId, SeedData.UsaLocationId] });

    Assert.Equal(new[] { SeedData.SerbiaLocationId, SeedData.UsaLocationId }.Order(), updated.LocationIds.Order());
    var found = await Service.FindBySlugAsync("pixel-forge");
    Assert.Equal(new[] { SeedData.SerbiaLocationId, SeedData.UsaLocationId }.Order(), found!.LocationIds.Order());
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task UpdateAsync_EmptyOrNullLocations_ClearsThem(bool useNull)
  {
    var created = await Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId] });

    var updated = await Service.UpdateAsync(created.Id, Input() with { LocationIds = useNull ? null : [] });

    Assert.Empty(updated.LocationIds);
    Assert.Empty((await Service.FindBySlugAsync("pixel-forge"))!.LocationIds);
  }

  [Fact]
  public async Task UpdateAsync_UnknownLocationId_ReportsLocationIdsErrorAndKeepsOldLocations()
  {
    var created = await Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId] });

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input() with { LocationIds = [Guid.NewGuid()] }));

    Assert.Contains(nameof(CompanyInput.LocationIds), exception.Errors.Keys);
    Assert.Equal(SeedData.EuLocationId, Assert.Single((await Service.FindBySlugAsync("pixel-forge"))!.LocationIds));
  }

  [Fact]
  public async Task Locations_AreSharedAcrossUsers()
  {
    await Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId] });
    var other = await PersistenceFixtures.AddUserAsync(Database);

    var found = await ServiceFor(other.Id).FindBySlugAsync("pixel-forge");

    Assert.Equal(SeedData.EuLocationId, Assert.Single(found!.LocationIds));
  }

  [Fact]
  public async Task SearchAsync_CompanyWithLocations_ReturnsNamesSortedIgnoringCase()
  {
    await Service.CreateAsync(Input() with { LocationIds = [SeedData.UsaLocationId, SeedData.EuLocationId, SeedData.SerbiaLocationId] });

    var item = Assert.Single(await Service.SearchAsync(null));

    Assert.Equal(["EU", "Serbia", "USA"], item.LocationNames);
  }

  [Fact]
  public async Task SearchAsync_CompanyWithoutLocations_ReturnsEmptyNames()
  {
    await Service.CreateAsync(Input());

    var item = Assert.Single(await Service.SearchAsync(null));

    Assert.Empty(item.LocationNames);
  }

  [Fact]
  public async Task DeleteAsync_CompanyWithLocations_RemovesItsLinks()
  {
    var created = await Service.CreateAsync(Input() with { LocationIds = [SeedData.EuLocationId, SeedData.UsaLocationId] });

    await Service.DeleteAsync(created.Id);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await context.Set<CompanyLocation>().ToListAsync());
  }
}
