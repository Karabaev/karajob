using Karajob.Domain.Companies;
using Karajob.Domain.Goals;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class EnginePersistenceTests
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Fact]
  public async Task Track_WithEngines_ReloadsEngineLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var track = PersistenceFixtures.NewTrack(SeedData.OwnerId);
    track.SetEngines([SeedData.UnityEngineId, SeedData.UnrealEngineId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Tracks.Add(track));

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Tracks.SingleAsync(h => h.Id == track.Id);

    Assert.Equal(
      new[] { SeedData.UnityEngineId, SeedData.UnrealEngineId }.Order(),
      loaded.Engines.Select(p => p.EngineId).Order());
    Assert.All(loaded.Engines, p => Assert.Equal(SeedData.OwnerId, p.UserId));
  }

  [Fact]
  public async Task Vacancy_WithEngines_ReloadsEngineLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var vacancy = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/engines", VacancySource.Manual, Moment);
    vacancy.SetEngines([SeedData.GodotEngineId, SeedData.DefoldEngineId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Vacancies.Add(vacancy));

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Equal(
      new[] { SeedData.GodotEngineId, SeedData.DefoldEngineId }.Order(),
      loaded.Engines.Select(p => p.EngineId).Order());
  }

  [Fact]
  public async Task SearchGoal_WithEngines_ReloadsEngineLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var goal = await context.SearchGoals.SingleAsync();
      goal.SetEngines([SeedData.CocosEngineId, SeedData.UnityEngineId]);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.SearchGoals.SingleAsync();

    Assert.Equal(
      new[] { SeedData.CocosEngineId, SeedData.UnityEngineId }.Order(),
      loaded.Engines.Select(p => p.EngineId).Order());
  }

  [Fact]
  public async Task Track_WithoutEngines_ReloadsEmptyEngines()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var track = await PersistenceFixtures.AddTrackAsync(database, SeedData.OwnerId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Tracks.SingleAsync(h => h.Id == track.Id);

    Assert.Empty(loaded.Engines);
  }

  [Fact]
  public async Task Track_RemovingEngineFromSet_DeletesLinkRow()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var track = PersistenceFixtures.NewTrack(SeedData.OwnerId);
    track.SetEngines([SeedData.UnityEngineId, SeedData.UnrealEngineId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Tracks.Add(track));

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Tracks.SingleAsync(h => h.Id == track.Id);
      loaded.SetEngines([SeedData.UnityEngineId]);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    Assert.Equal(SeedData.UnityEngineId, Assert.Single(await fresh.TrackEngines.ToListAsync()).EngineId);
  }

  [Fact]
  public async Task SaveChanges_DeletingEngineReferencedByTrack_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var track = PersistenceFixtures.NewTrack(SeedData.OwnerId);
    track.SetEngines([SeedData.UnityEngineId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Tracks.Add(track));

    await AssertDeletingEngineFailsAsync(database, SeedData.UnityEngineId);
  }

  [Fact]
  public async Task SaveChanges_DeletingEngineReferencedByVacancy_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var vacancy = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/restrict", VacancySource.Manual, Moment);
    vacancy.SetEngines([SeedData.GodotEngineId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Vacancies.Add(vacancy));

    await AssertDeletingEngineFailsAsync(database, SeedData.GodotEngineId);
  }

  [Fact]
  public async Task SaveChanges_DeletingEngineReferencedBySearchGoal_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var goal = await context.SearchGoals.SingleAsync();
      goal.SetEngines([SeedData.DefoldEngineId]);
      await context.SaveChangesAsync();
    }

    await AssertDeletingEngineFailsAsync(database, SeedData.DefoldEngineId);
  }

  [Fact]
  public async Task SaveChanges_DeletingUnreferencedEngine_Succeeds()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Engines.Remove(await context.Engines.SingleAsync(p => p.Id == SeedData.UnrealEngineId));

    await context.SaveChangesAsync();

    Assert.False(await context.Engines.AnyAsync(p => p.Id == SeedData.UnrealEngineId));
  }

  [Fact]
  public async Task SaveChanges_DeletingTrackWithEngines_CascadesToLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var track = PersistenceFixtures.NewTrack(SeedData.OwnerId);
    track.SetEngines([SeedData.UnityEngineId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Tracks.Add(track));

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Tracks.Remove(await context.Tracks.SingleAsync(h => h.Id == track.Id));
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await fresh.TrackEngines.ToListAsync());
  }

  [Theory]
  [InlineData(typeof(TrackEngine), nameof(TrackEngine.TrackId))]
  [InlineData(typeof(VacancyEngine), nameof(VacancyEngine.VacancyId))]
  [InlineData(typeof(SearchGoalEngine), nameof(SearchGoalEngine.SearchGoalId))]
  public async Task Model_EngineLink_HasUniqueIndexOnOwnerAndEngine(Type linkType, string ownerProperty)
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var entityType = context.Model.FindEntityType(linkType)!;

    Assert.Contains(
      entityType.GetIndexes(),
      i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([ownerProperty, "EngineId"]));
  }

  [Fact]
  public async Task Engines_LinkedByOtherProfile_AreInvisibleToOwnerButCatalogStaysShared()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    var track = PersistenceFixtures.NewTrack(other.Id);
    track.SetEngines([SeedData.UnityEngineId]);
    await SaveAsync(database, other.Id, c => c.Tracks.Add(track));
    var vacancy = new Vacancy(other.Id, "https://example.com/jobs/isolation", VacancySource.Manual, Moment);
    vacancy.SetEngines([SeedData.DefoldEngineId]);
    await SaveAsync(database, other.Id, c => c.Vacancies.Add(vacancy));
    var goal = new SearchGoal(other.Id);
    goal.SetEngines([SeedData.GodotEngineId]);
    await SaveAsync(database, other.Id, c => c.SearchGoals.Add(goal));

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.TrackEngines.ToListAsync());
    Assert.Empty(await owner.VacancyEngines.ToListAsync());
    Assert.Empty(await owner.SearchGoalEngines.ToListAsync());
    Assert.Single(await otherContext.TrackEngines.ToListAsync());
    Assert.Single(await otherContext.VacancyEngines.ToListAsync());
    Assert.Single(await otherContext.SearchGoalEngines.ToListAsync());
    Assert.Equal(6, await owner.Engines.CountAsync());
  }

  [Fact]
  public async Task Engines_AfterMigration_ContainSeededCatalog()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var engines = await context.Engines.ToListAsync();

    Assert.Equal(
      ["Cocos", "Defold", "Godot", "Proprietary", "Unity", "Unreal Engine"],
      engines.Select(e => e.Name).Order(StringComparer.Ordinal));
    Assert.Equal(SeedData.UnityEngineId, engines.Single(e => e.Name == "Unity").Id);
    Assert.Equal(SeedData.ProprietaryEngineId, engines.Single(e => e.Name == "Proprietary").Id);
  }

  [Fact]
  public async Task Project_WithEngine_ReloadsEngineId()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var project = await AddProjectAsync(database, SeedData.GodotEngineId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);

    Assert.Equal(SeedData.GodotEngineId, (await fresh.Projects.SingleAsync(p => p.Id == project.Id)).EngineId);
  }

  [Fact]
  public async Task SaveChanges_DeletingEngineReferencedByProject_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await AddProjectAsync(database, SeedData.UnityEngineId);

    await AssertDeletingEngineFailsAsync(database, SeedData.UnityEngineId);
  }

  [Fact]
  public async Task SaveChanges_ProjectWithUnknownEngine_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();

    await Assert.ThrowsAsync<DbUpdateException>(() => AddProjectAsync(database, Guid.NewGuid()));
  }

  [Fact]
  public async Task SearchGoalEngines_LinkedByOtherProfile_AreInvisibleToOwner()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    var goal = new SearchGoal(other.Id);
    goal.SetEngines([SeedData.CocosEngineId]);
    await SaveAsync(database, other.Id, c => c.SearchGoals.Add(goal));

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.SearchGoalEngines.ToListAsync());
    Assert.Single(await otherContext.SearchGoalEngines.ToListAsync());
  }

  private static async Task<Project> AddProjectAsync(SqliteTestDatabase database, Guid engineId)
  {
    var company = await PersistenceFixtures.AddCompanyAsync(database, "engine-co");
    var project = new Project(company.Id, "Engine Game", ProjectStatus.InDevelopment) { EngineId = engineId };
    await SaveAsync(database, SeedData.OwnerId, c => c.Projects.Add(project));
    return project;
  }

  private static async Task SaveAsync(SqliteTestDatabase database, Guid userId, Action<KarajobDbContext> add)
  {
    await using var context = database.CreateContext(userId);
    add(context);
    await context.SaveChangesAsync();
  }

  private static async Task AssertDeletingEngineFailsAsync(SqliteTestDatabase database, Guid engineId)
  {
    await using var context = database.CreateContext(SeedData.OwnerId);
    var engine = await context.Engines.SingleAsync(p => p.Id == engineId);

    context.Engines.Remove(engine);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }
}
