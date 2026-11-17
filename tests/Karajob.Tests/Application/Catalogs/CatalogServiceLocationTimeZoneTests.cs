using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Catalogs;

public class CatalogServiceLocationTimeZoneTests : CatalogServiceFixture
{
  [Fact]
  public async Task ListAsync_Locations_ExposesSeededTimeZones()
  {
    var items = await Service.ListAsync(CatalogKind.Location);

    Assert.Equal("Europe/Belgrade", items.Single(i => i.Id == SeedData.SerbiaLocationId).TimeZoneId);
    Assert.Null(items.Single(i => i.Id == SeedData.EuLocationId).TimeZoneId);
    Assert.Null(items.Single(i => i.Id == SeedData.UsaLocationId).TimeZoneId);
  }

  [Fact]
  public async Task ListAsync_Roles_HaveNoTimeZone()
  {
    var items = await Service.ListAsync(CatalogKind.Role);

    Assert.All(items, i => Assert.Null(i.TimeZoneId));
  }

  [Fact]
  public async Task UpdateLocationAsync_NameAndZone_ReturnsUpdatedItemAndPersists()
  {
    var updated = await Service.UpdateLocationAsync(SeedData.EuLocationId, "  Europe ", "Europe/Berlin");

    Assert.Equal(SeedData.EuLocationId, updated.Id);
    Assert.Equal("Europe", updated.Name);
    Assert.Equal("Europe/Berlin", updated.TimeZoneId);
    Assert.Contains(
      await Service.ListAsync(CatalogKind.Location),
      i => i.Id == SeedData.EuLocationId && i.Name == "Europe" && i.TimeZoneId == "Europe/Berlin");
  }

  [Fact]
  public async Task UpdateLocationAsync_WindowsZoneId_StoresIanaId()
  {
    var updated = await Service.UpdateLocationAsync(SeedData.EuLocationId, "EU", "W. Europe Standard Time");

    Assert.Equal("Europe/Berlin", updated.TimeZoneId);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task UpdateLocationAsync_NullOrBlankZone_ClearsExistingZone(string? zone)
  {
    var updated = await Service.UpdateLocationAsync(SeedData.SerbiaLocationId, "Serbia", zone);

    Assert.Null(updated.TimeZoneId);
    Assert.Null((await Service.ListAsync(CatalogKind.Location)).Single(i => i.Id == SeedData.SerbiaLocationId).TimeZoneId);
  }

  [Fact]
  public async Task UpdateLocationAsync_InvalidZone_ThrowsValidationWithTimeZoneKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateLocationAsync(SeedData.EuLocationId, "EU", "Mars/Olympus_Mons"));

    Assert.Contains("TimeZoneId", exception.Errors.Keys);
    Assert.DoesNotContain(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task UpdateLocationAsync_InvalidZone_SavesNothingNotEvenTheName()
  {
    await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateLocationAsync(SeedData.SerbiaLocationId, "Renamed", "Mars/Olympus_Mons"));

    var item = (await Service.ListAsync(CatalogKind.Location)).Single(i => i.Id == SeedData.SerbiaLocationId);
    Assert.Equal("Serbia", item.Name);
    Assert.Equal("Europe/Belgrade", item.TimeZoneId);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task UpdateLocationAsync_BlankName_ThrowsValidationWithNameKey(string name)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateLocationAsync(SeedData.EuLocationId, name, "Europe/Berlin"));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task UpdateLocationAsync_DuplicateName_ThrowsValidationWithNameKeyAndSavesNothing()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateLocationAsync(SeedData.EuLocationId, "serbia", "Europe/Berlin"));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
    Assert.Null((await Service.ListAsync(CatalogKind.Location)).Single(i => i.Id == SeedData.EuLocationId).TimeZoneId);
  }

  [Fact]
  public async Task UpdateLocationAsync_KeepingOwnName_Succeeds()
  {
    var updated = await Service.UpdateLocationAsync(SeedData.SerbiaLocationId, "Serbia", "Europe/Berlin");

    Assert.Equal("Serbia", updated.Name);
    Assert.Equal("Europe/Berlin", updated.TimeZoneId);
  }

  [Fact]
  public async Task UpdateLocationAsync_UnknownId_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateLocationAsync(Guid.NewGuid(), "Anywhere", "Europe/Berlin"));
  }

  [Fact]
  public async Task UpdateLocationAsync_IdOfNonLocationEntry_ThrowsKeyNotFound()
  {
    var role = (await Service.ListAsync(CatalogKind.Role)).First();

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateLocationAsync(role.Id, "Anywhere", "Europe/Berlin"));
  }
}
