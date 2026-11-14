using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Catalogs;

public class CatalogServiceTests : CatalogServiceFixture
{
  [Fact]
  public async Task ListAsync_Roles_ReturnsSeededRolesWithoutCode()
  {
    var items = await Service.ListAsync(CatalogKind.Role);

    Assert.Equal(
      [
        new CatalogListItem(SeedData.UnityDeveloperRoleId, "Unity Developer", null),
        new CatalogListItem(SeedData.UnrealDeveloperRoleId, "Unreal Developer", null),
      ],
      items);
  }

  [Fact]
  public async Task ListAsync_Locations_ReturnsSeededLocationsWithoutCode()
  {
    var items = await Service.ListAsync(CatalogKind.Location);

    Assert.Equal(["EU", "Serbia", "USA"], items.Select(i => i.Name));
    Assert.All(items, i => Assert.Null(i.Code));
  }

  [Fact]
  public async Task ListAsync_Languages_ReturnsSeededLanguagesWithCode()
  {
    var items = await Service.ListAsync(CatalogKind.Language);

    Assert.Equal(
      [
        new CatalogListItem(SeedData.EnglishLanguageId, "English", "en"),
        new CatalogListItem(SeedData.RussianLanguageId, "Русский", "ru"),
      ],
      items);
  }

  [Fact]
  public async Task ListAsync_SortsByNameCaseInsensitively()
  {
    await Service.CreateAsync(CatalogKind.Role, new CatalogInput("alpha role"));
    await Service.CreateAsync(CatalogKind.Role, new CatalogInput("Zeta role"));

    var items = await Service.ListAsync(CatalogKind.Role);

    Assert.Equal(["alpha role", "Unity Developer", "Unreal Developer", "Zeta role"], items.Select(i => i.Name));
  }

  [Fact]
  public async Task CreateAsync_Role_ReturnsAndPersistsItem()
  {
    var created = await Service.CreateAsync(CatalogKind.Role, new CatalogInput("  Godot Developer "));

    Assert.NotEqual(Guid.Empty, created.Id);
    Assert.Equal("Godot Developer", created.Name);
    Assert.Null(created.Code);
    Assert.Contains(await Service.ListAsync(CatalogKind.Role), i => i.Id == created.Id && i.Name == "Godot Developer");
  }

  [Fact]
  public async Task CreateAsync_Location_ReturnsAndPersistsItem()
  {
    var created = await Service.CreateAsync(CatalogKind.Location, new CatalogInput("Germany"));

    Assert.Equal("Germany", created.Name);
    Assert.Contains(await Service.ListAsync(CatalogKind.Location), i => i.Id == created.Id);
  }

  [Fact]
  public async Task CreateAsync_Language_NormalizesCodeAndPersists()
  {
    var created = await Service.CreateAsync(CatalogKind.Language, new CatalogInput("Deutsch", " DE "));

    Assert.Equal("Deutsch", created.Name);
    Assert.Equal("de", created.Code);
    Assert.Contains(await Service.ListAsync(CatalogKind.Language), i => i.Id == created.Id && i.Code == "de");
  }

  [Theory]
  [InlineData(CatalogKind.Role)]
  [InlineData(CatalogKind.Location)]
  public async Task CreateAsync_CodeForNonLanguage_IsIgnored(CatalogKind kind)
  {
    var created = await Service.CreateAsync(kind, new CatalogInput("Something new", "xx"));

    Assert.Null(created.Code);
  }

  [Theory]
  [InlineData(CatalogKind.Role, "")]
  [InlineData(CatalogKind.Role, "   ")]
  [InlineData(CatalogKind.Location, "")]
  [InlineData(CatalogKind.Language, "")]
  public async Task CreateAsync_BlankName_ThrowsValidationWithNameKey(CatalogKind kind, string name)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(kind, new CatalogInput(name, "de")));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(CatalogKind.Role, "unity  developer")]
  [InlineData(CatalogKind.Role, "UNITY DEVELOPER")]
  [InlineData(CatalogKind.Location, " usa ")]
  [InlineData(CatalogKind.Language, "ENGLISH")]
  public async Task CreateAsync_DuplicateName_ThrowsValidationWithNameKey(CatalogKind kind, string name)
  {
    var before = (await Service.ListAsync(kind)).Count;

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(kind, new CatalogInput(name, "de")));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
    Assert.Equal(before, (await Service.ListAsync(kind)).Count);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  [InlineData("r")]
  [InlineData("russ")]
  [InlineData("r1")]
  public async Task CreateAsync_LanguageWithBlankOrInvalidCode_ThrowsValidationWithCodeKey(string? code)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Language, new CatalogInput("Deutsch", code)));

    Assert.Contains(nameof(CatalogInput.Code), exception.Errors.Keys);
    Assert.DoesNotContain(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_LanguageWithDuplicateCode_ThrowsValidationWithCodeKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Language, new CatalogInput("Another Russian", "RU")));

    Assert.Contains(nameof(CatalogInput.Code), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_NameUsedInAnotherCatalog_Succeeds()
  {
    var created = await Service.CreateAsync(CatalogKind.Role, new CatalogInput("Serbia"));

    Assert.Equal("Serbia", created.Name);
  }

  [Fact]
  public async Task RenameAsync_Role_ReturnsAndPersistsNewName()
  {
    var renamed = await Service.RenameAsync(CatalogKind.Role, SeedData.UnrealDeveloperRoleId, "  Unreal Engine Developer ");

    Assert.Equal(SeedData.UnrealDeveloperRoleId, renamed.Id);
    Assert.Equal("Unreal Engine Developer", renamed.Name);
    Assert.Contains(await Service.ListAsync(CatalogKind.Role), i => i.Id == renamed.Id && i.Name == "Unreal Engine Developer");
  }

  [Fact]
  public async Task RenameAsync_ToOwnNameWithDifferentCase_IsAllowed()
  {
    var renamed = await Service.RenameAsync(CatalogKind.Location, SeedData.UsaLocationId, "Usa");

    Assert.Equal("Usa", renamed.Name);
  }

  [Fact]
  public async Task RenameAsync_Language_KeepsCode()
  {
    var renamed = await Service.RenameAsync(CatalogKind.Language, SeedData.RussianLanguageId, "Russian");

    Assert.Equal("Russian", renamed.Name);
    Assert.Equal("ru", renamed.Code);
  }

  [Fact]
  public async Task RenameAsync_ToAnotherEntrysName_ThrowsValidationWithNameKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.RenameAsync(CatalogKind.Role, SeedData.UnrealDeveloperRoleId, "unity  developer"));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
    Assert.Contains(await Service.ListAsync(CatalogKind.Role), i => i.Name == "Unreal Developer");
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task RenameAsync_BlankName_ThrowsValidationWithNameKey(string name)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.RenameAsync(CatalogKind.Location, SeedData.EuLocationId, name));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Theory]
  [InlineData(CatalogKind.Role)]
  [InlineData(CatalogKind.Location)]
  [InlineData(CatalogKind.Language)]
  public async Task RenameAsync_UnknownId_ThrowsKeyNotFound(CatalogKind kind)
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RenameAsync(kind, Guid.NewGuid(), "Whatever"));
  }

  [Fact]
  public async Task RenameAsync_IdOfAnotherCatalogKind_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(
      () => Service.RenameAsync(CatalogKind.Location, SeedData.UnityDeveloperRoleId, "Whatever"));
  }
}
