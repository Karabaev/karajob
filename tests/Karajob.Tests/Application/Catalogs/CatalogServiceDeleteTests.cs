using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Domain.Cvs;
using Karajob.Domain.Goals;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Catalogs;

public class CatalogServiceDeleteTests : CatalogServiceFixture
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Theory]
  [InlineData(CatalogKind.Role, "Godot Developer", null)]
  [InlineData(CatalogKind.Location, "Germany", null)]
  [InlineData(CatalogKind.Platform, "Smart TV", null)]
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
  [InlineData(CatalogKind.Platform)]
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
  public async Task DeleteAsync_RoleUsedByTrack_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await PersistenceFixtures.AddTrackAsync(Database, SeedData.OwnerId);

    await AssertDeleteBlockedAsync(CatalogKind.Role, SeedData.UnityDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_RoleUsedByTrackOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await PersistenceFixtures.AddTrackAsync(Database, other.Id);

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
  public async Task DeleteAsync_LocationUsedByTrack_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddTrackWithLocationAsync(SeedData.OwnerId, SeedData.EuLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.EuLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedByTrackOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddTrackWithLocationAsync(other.Id, SeedData.EuLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.EuLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedByVacancy_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddVacancyAsync(SeedData.OwnerId, locationId: SeedData.SerbiaLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.SerbiaLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByTrack_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await PersistenceFixtures.AddTrackAsync(Database, SeedData.OwnerId);

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
  public async Task DeleteAsync_LanguageUsedByTrackOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await PersistenceFixtures.AddTrackAsync(Database, other.Id);

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.EnglishLanguageId);
  }

  [Fact]
  public async Task DeleteAsync_LanguageUsedByVacancyOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddVacancyAsync(other.Id, languageId: SeedData.RussianLanguageId);

    await AssertDeleteBlockedAsync(CatalogKind.Language, SeedData.RussianLanguageId);
  }

  [Fact]
  public async Task DeleteAsync_RoleUsedBySearchGoal_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await SetSearchGoalRoleAsync(SeedData.OwnerId, SeedData.UnrealDeveloperRoleId);

    await AssertDeleteBlockedAsync(CatalogKind.Role, SeedData.UnrealDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_RoleUsedBySearchGoalOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddSearchGoalAsync(other.Id, roleId: SeedData.UnrealDeveloperRoleId);

    await AssertDeleteBlockedAsync(CatalogKind.Role, SeedData.UnrealDeveloperRoleId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedBySearchGoal_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await SetSearchGoalLocationAsync(SeedData.OwnerId, SeedData.EuLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.EuLocationId);
  }

  [Fact]
  public async Task DeleteAsync_LocationUsedBySearchGoalOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddSearchGoalAsync(other.Id, locationId: SeedData.EuLocationId);

    await AssertDeleteBlockedAsync(CatalogKind.Location, SeedData.EuLocationId);
  }

  [Fact]
  public async Task DeleteAsync_SeededPlatformNotReferenced_RemovesIt()
  {
    await Service.DeleteAsync(CatalogKind.Platform, SeedData.VrPlatformId);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Platform), i => i.Id == SeedData.VrPlatformId);
  }

  [Fact]
  public async Task DeleteAsync_PlatformUsedByTrack_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddTrackWithPlatformAsync(SeedData.OwnerId, SeedData.PcPlatformId);

    await AssertDeleteBlockedAsync(CatalogKind.Platform, SeedData.PcPlatformId);
  }

  [Fact]
  public async Task DeleteAsync_PlatformUsedByTrackOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddTrackWithPlatformAsync(other.Id, SeedData.PcPlatformId);

    await AssertDeleteBlockedAsync(CatalogKind.Platform, SeedData.PcPlatformId);
  }

  [Fact]
  public async Task DeleteAsync_PlatformUsedByVacancy_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddVacancyWithPlatformAsync(SeedData.OwnerId, SeedData.MobilePlatformId);

    await AssertDeleteBlockedAsync(CatalogKind.Platform, SeedData.MobilePlatformId);
  }

  [Fact]
  public async Task DeleteAsync_PlatformUsedByVacancyOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddVacancyWithPlatformAsync(other.Id, SeedData.MobilePlatformId);

    await AssertDeleteBlockedAsync(CatalogKind.Platform, SeedData.MobilePlatformId);
  }

  [Fact]
  public async Task DeleteAsync_PlatformUsedBySearchGoal_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddSearchGoalWithPlatformAsync(SeedData.OwnerId, SeedData.WebPlatformId);

    await AssertDeleteBlockedAsync(CatalogKind.Platform, SeedData.WebPlatformId);
  }

  [Fact]
  public async Task DeleteAsync_PlatformUsedBySearchGoalOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddSearchGoalWithPlatformAsync(other.Id, SeedData.WebPlatformId);

    await AssertDeleteBlockedAsync(CatalogKind.Platform, SeedData.WebPlatformId);
  }

  private async Task AssertDeleteBlockedAsync(CatalogKind kind, Guid id)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(kind, id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Contains(await Service.ListAsync(kind), i => i.Id == id);
  }

  private async Task SetSearchGoalRoleAsync(Guid userId, Guid roleId)
  {
    await using var context = Database.CreateContext(userId);
    var goal = await context.SearchGoals.SingleAsync();
    goal.RoleId = roleId;
    await context.SaveChangesAsync();
  }

  private async Task SetSearchGoalLocationAsync(Guid userId, Guid locationId)
  {
    await using var context = Database.CreateContext(userId);
    var goal = await context.SearchGoals.SingleAsync();
    goal.SetLocations([locationId]);
    await context.SaveChangesAsync();
  }

  private async Task AddSearchGoalAsync(Guid userId, Guid? roleId = null, Guid? locationId = null)
  {
    var goal = new SearchGoal(userId) { RoleId = roleId };
    if (locationId is { } id)
    {
      goal.SetLocations([id]);
    }

    await using var context = Database.CreateContext(userId);
    context.SearchGoals.Add(goal);
    await context.SaveChangesAsync();
  }

  private async Task AddTrackWithLocationAsync(Guid userId, Guid locationId)
  {
    var track = new Track(userId, "Hypo", SeedData.UnrealDeveloperRoleId, SeedData.RussianLanguageId, Moment);
    track.SetLocations([locationId]);
    await using var context = Database.CreateContext(userId);
    context.Tracks.Add(track);
    await context.SaveChangesAsync();
  }

  private async Task AddTrackWithPlatformAsync(Guid userId, Guid platformId)
  {
    var track = new Track(userId, "Hypo", SeedData.UnrealDeveloperRoleId, SeedData.RussianLanguageId, Moment);
    track.SetPlatforms([platformId]);
    await using var context = Database.CreateContext(userId);
    context.Tracks.Add(track);
    await context.SaveChangesAsync();
  }

  private async Task AddVacancyWithPlatformAsync(Guid userId, Guid platformId)
  {
    var vacancy = new Vacancy(userId, "https://example.com/jobs/catalog", VacancySource.Manual, Moment);
    vacancy.SetPlatforms([platformId]);
    await using var context = Database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
  }

  private async Task AddSearchGoalWithPlatformAsync(Guid userId, Guid platformId)
  {
    await using var context = Database.CreateContext(userId);
    var goal = await context.SearchGoals.SingleOrDefaultAsync();
    if (goal is null)
    {
      goal = new SearchGoal(userId);
      context.SearchGoals.Add(goal);
    }

    goal.SetPlatforms([platformId]);
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
