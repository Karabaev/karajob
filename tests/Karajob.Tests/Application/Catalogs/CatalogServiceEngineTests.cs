using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Domain.Companies;
using Karajob.Domain.Goals;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Catalogs;

public class CatalogServiceEngineTests : CatalogServiceFixture
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Fact]
  public async Task ListAsync_Engines_ReturnsSeededEnginesSortedByNameWithoutCode()
  {
    var items = await Service.ListAsync(CatalogKind.Engine);

    Assert.Equal(["Cocos", "Defold", "Godot", "Proprietary", "Unity", "Unreal Engine"], items.Select(i => i.Name));
    Assert.Equal(SeedData.UnityEngineId, items.Single(i => i.Name == "Unity").Id);
    Assert.All(items, i => Assert.Null(i.Code));
  }

  [Fact]
  public async Task CreateAsync_Engine_ReturnsAndPersistsItem()
  {
    var created = await Service.CreateAsync(CatalogKind.Engine, new CatalogInput("  Phaser "));

    Assert.NotEqual(Guid.Empty, created.Id);
    Assert.Equal("Phaser", created.Name);
    Assert.Null(created.Code);
    Assert.Contains(await Service.ListAsync(CatalogKind.Engine), i => i.Id == created.Id && i.Name == "Phaser");
  }

  [Fact]
  public async Task CreateAsync_EngineWithCode_IgnoresCode()
  {
    var created = await Service.CreateAsync(CatalogKind.Engine, new CatalogInput("Phaser", "xx"));

    Assert.Null(created.Code);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task CreateAsync_EngineWithBlankName_ThrowsValidationWithNameKey(string name)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Engine, new CatalogInput(name)));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Theory]
  [InlineData("unity")]
  [InlineData(" UNREAL  engine ")]
  public async Task CreateAsync_EngineWithDuplicateName_ThrowsValidationWithNameKey(string name)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Engine, new CatalogInput(name)));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
    Assert.Equal(6, (await Service.ListAsync(CatalogKind.Engine)).Count);
  }

  [Fact]
  public async Task RenameAsync_Engine_ReturnsAndPersistsNewName()
  {
    var renamed = await Service.RenameAsync(CatalogKind.Engine, SeedData.CocosEngineId, "  Cocos Creator ");

    Assert.Equal(SeedData.CocosEngineId, renamed.Id);
    Assert.Equal("Cocos Creator", renamed.Name);
    Assert.Contains(await Service.ListAsync(CatalogKind.Engine), i => i.Id == renamed.Id && i.Name == "Cocos Creator");
  }

  [Fact]
  public async Task RenameAsync_EngineToAnotherEnginesName_ThrowsValidationWithNameKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.RenameAsync(CatalogKind.Engine, SeedData.CocosEngineId, "godot"));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task RenameAsync_UnknownEngine_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RenameAsync(CatalogKind.Engine, Guid.NewGuid(), "Новый"));
  }

  [Fact]
  public async Task DeleteAsync_UnusedEngine_RemovesIt()
  {
    var created = await Service.CreateAsync(CatalogKind.Engine, new CatalogInput("Phaser"));

    await Service.DeleteAsync(CatalogKind.Engine, created.Id);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Engine), i => i.Id == created.Id);
  }

  [Fact]
  public async Task DeleteAsync_SeededEngineNotReferenced_RemovesIt()
  {
    await Service.DeleteAsync(CatalogKind.Engine, SeedData.CocosEngineId);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Engine), i => i.Id == SeedData.CocosEngineId);
  }

  [Fact]
  public async Task DeleteAsync_UnknownEngine_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(CatalogKind.Engine, Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteAsync_EngineUsedByProject_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    var company = await PersistenceFixtures.AddCompanyAsync(Database, "engine-delete-co");
    await using (var context = Database.CreateContext(SeedData.OwnerId))
    {
      context.Projects.Add(new Project(company.Id, "Game", ProjectStatus.InDevelopment) { EngineId = SeedData.UnityEngineId });
      await context.SaveChangesAsync();
    }

    await AssertDeleteBlockedAsync(SeedData.UnityEngineId);
  }

  [Fact]
  public async Task DeleteAsync_EngineUsedByTrack_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddTrackWithEngineAsync(SeedData.OwnerId, SeedData.UnrealEngineId);

    await AssertDeleteBlockedAsync(SeedData.UnrealEngineId);
  }

  [Fact]
  public async Task DeleteAsync_EngineUsedByTrackOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddTrackWithEngineAsync(other.Id, SeedData.UnrealEngineId);

    await AssertDeleteBlockedAsync(SeedData.UnrealEngineId);
  }

  [Fact]
  public async Task DeleteAsync_EngineUsedByVacancy_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddVacancyWithEngineAsync(SeedData.OwnerId, SeedData.GodotEngineId);

    await AssertDeleteBlockedAsync(SeedData.GodotEngineId);
  }

  [Fact]
  public async Task DeleteAsync_EngineUsedByVacancyOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddVacancyWithEngineAsync(other.Id, SeedData.GodotEngineId);

    await AssertDeleteBlockedAsync(SeedData.GodotEngineId);
  }

  [Fact]
  public async Task DeleteAsync_EngineUsedBySearchGoal_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddSearchGoalWithEngineAsync(SeedData.OwnerId, SeedData.DefoldEngineId);

    await AssertDeleteBlockedAsync(SeedData.DefoldEngineId);
  }

  [Fact]
  public async Task DeleteAsync_EngineUsedBySearchGoalOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddSearchGoalWithEngineAsync(other.Id, SeedData.DefoldEngineId);

    await AssertDeleteBlockedAsync(SeedData.DefoldEngineId);
  }

  private async Task AssertDeleteBlockedAsync(Guid id)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(CatalogKind.Engine, id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Contains(await Service.ListAsync(CatalogKind.Engine), i => i.Id == id);
  }

  private async Task AddTrackWithEngineAsync(Guid userId, Guid engineId)
  {
    var track = new Track(userId, "Hypo", SeedData.UnrealDeveloperRoleId, SeedData.RussianLanguageId, Moment);
    track.SetEngines([engineId]);
    await using var context = Database.CreateContext(userId);
    context.Tracks.Add(track);
    await context.SaveChangesAsync();
  }

  private async Task AddVacancyWithEngineAsync(Guid userId, Guid engineId)
  {
    var vacancy = new Vacancy(userId, "https://example.com/jobs/engine", VacancySource.Manual, Moment);
    vacancy.SetEngines([engineId]);
    await using var context = Database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
  }

  private async Task AddSearchGoalWithEngineAsync(Guid userId, Guid engineId)
  {
    await using var context = Database.CreateContext(userId);
    var goal = await context.SearchGoals.SingleOrDefaultAsync();
    if (goal is null)
    {
      goal = new SearchGoal(userId);
      context.SearchGoals.Add(goal);
    }

    goal.SetEngines([engineId]);
    await context.SaveChangesAsync();
  }
}
