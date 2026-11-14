using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Catalogs;

public class CatalogServiceDeleteTests : CatalogServiceFixture
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Theory]
  [InlineData(CatalogKind.Role, "Godot Developer", null)]
  [InlineData(CatalogKind.Location, "Germany", null)]
  [InlineData(CatalogKind.Language, "Deutsch", "de")]
  public async Task DeleteAsync_UnusedEntry_RemovesIt(CatalogKind kind, string name, string? code)
  {
    var created = await Service.CreateAsync(kind, new CatalogInput(name, code));

    await Service.DeleteAsync(kind, created.Id);

    Assert.DoesNotContain(await Service.ListAsync(kind), i => i.Id == created.Id);
  }

  [Theory]
  [InlineData(CatalogKind.Role)]
  [InlineData(CatalogKind.Location)]
  [InlineData(CatalogKind.Language)]
  public async Task DeleteAsync_UnknownId_ThrowsKeyNotFound(CatalogKind kind)
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(kind, Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteAsync_SeededEntryNotReferenced_RemovesIt()
  {
    await Service.DeleteAsync(CatalogKind.Role, SeedData.UnrealDeveloperRoleId);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Role), i => i.Id == SeedData.UnrealDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_RoleUsedByHypothesis_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await PersistenceFixtures.AddHypothesisAsync(Database, SeedData.OwnerId);

    await AssertDeleteBlockedAsync(CatalogKind.Role, SeedData.UnityDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_RoleUsedByHypothesisOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await PersistenceFixtures.AddHypothesisAsync(Database, other.Id);

    await AssertDeleteBlockedAsync(CatalogKind.Role, SeedData.UnityDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_RoleUsedByVacancy_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddVacancyAsync(SeedData.OwnerId, roleId: SeedData.UnrealDeveloperRoleId);

    await AssertDeleteBlockedAsync(CatalogKind.Role, SeedData.UnrealDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_RoleUsedByVacancyOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddVacancyAsync(other.Id, roleId: SeedData.UnrealDeveloperRoleId);

    await AssertDeleteBlockedAsync(CatalogKind.Role, SeedData.UnrealDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedByHypothesis_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddHypothesisWithLocationAsync(SeedData.OwnerId, SeedData.EuLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.EuLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedByHypothesisOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddHypothesisWithLocationAsync(other.Id, SeedData.EuLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.EuLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedByVacancy_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddVacancyAsync(SeedData.OwnerId, locationId: SeedData.SerbiaLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.SerbiaLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByHypothesis_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await PersistenceFixtures.AddHypothesisAsync(Database, SeedData.OwnerId);

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.EnglishLanguageId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByVacancy_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddVacancyAsync(SeedData.OwnerId, languageId: SeedData.RussianLanguageId);

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.RussianLanguageId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByCvDocument_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await PersistenceFixtures.AddCvAsync(Database, SeedData.OwnerId);

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.EnglishLanguageId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByCvDocumentOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await using (var context = Database.CreateContext(other.Id))
    {
      context.CvDocuments.Add(new CvDocument(other.Id, "Other CV", SeedData.RussianLanguageId, Moment));
      await context.SaveChangesAsync();
    }

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.RussianLanguageId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedByVacancyOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddVacancyAsync(other.Id, locationId: SeedData.SerbiaLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.SerbiaLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByHypothesisOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await PersistenceFixtures.AddHypothesisAsync(Database, other.Id);

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.EnglishLanguageId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByVacancyOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddVacancyAsync(other.Id, languageId: SeedData.RussianLanguageId);

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.RussianLanguageId);
  }

  private async Task AssertDeleteBlockedAsync(CatalogKind kind, Guid id)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(kind, id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Contains(await Service.ListAsync(kind), i => i.Id == id);
  }

  private async Task AddHypothesisWithLocationAsync(Guid userId, Guid locationId)
  {
    var hypothesis = new Hypothesis(userId, "Hypo", SeedData.UnrealDeveloperRoleId, SeedData.RussianLanguageId, Moment);
    hypothesis.SetLocations([locationId]);
    await using var context = Database.CreateContext(userId);
    context.Hypotheses.Add(hypothesis);
    await context.SaveChangesAsync();
  }

  private async Task AddVacancyAsync(Guid userId, Guid? roleId = null, Guid? locationId = null, Guid? languageId = null)
  {
    var vacancy = new Vacancy(userId, "https://example.com/jobs/catalog", VacancySource.Manual, Moment)
    {
      RoleId = roleId,
      LocationId = locationId,
      LanguageId = languageId,
    };
    await using var context = Database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
  }
}
