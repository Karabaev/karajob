using Karajob.Domain.Goals;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class PlatformPersistenceTests
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Fact]
  public async Task Hypothesis_WithPlatforms_ReloadsPlatformLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = PersistenceFixtures.NewHypothesis(SeedData.OwnerId);
    hypothesis.SetPlatforms([SeedData.PcPlatformId, SeedData.VrPlatformId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Hypotheses.Add(hypothesis));

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Hypotheses.SingleAsync(h => h.Id == hypothesis.Id);

    Assert.Equal(
      new[] { SeedData.PcPlatformId, SeedData.VrPlatformId }.Order(),
      loaded.Platforms.Select(p => p.PlatformId).Order());
    Assert.All(loaded.Platforms, p => Assert.Equal(SeedData.OwnerId, p.UserId));
  }

  [Fact]
  public async Task Vacancy_WithPlatforms_ReloadsPlatformLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var vacancy = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/platforms", VacancySource.Manual, Moment);
    vacancy.SetPlatforms([SeedData.MobilePlatformId, SeedData.WebPlatformId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Vacancies.Add(vacancy));

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Vacancies.SingleAsync(v => v.Id == vacancy.Id);

    Assert.Equal(
      new[] { SeedData.MobilePlatformId, SeedData.WebPlatformId }.Order(),
      loaded.Platforms.Select(p => p.PlatformId).Order());
  }

  [Fact]
  public async Task SearchGoal_WithPlatforms_ReloadsPlatformLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var goal = await context.SearchGoals.SingleAsync();
      goal.SetPlatforms([SeedData.ConsolesPlatformId, SeedData.PcPlatformId]);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.SearchGoals.SingleAsync();

    Assert.Equal(
      new[] { SeedData.ConsolesPlatformId, SeedData.PcPlatformId }.Order(),
      loaded.Platforms.Select(p => p.PlatformId).Order());
  }

  [Fact]
  public async Task Hypothesis_WithoutPlatforms_ReloadsEmptyPlatforms()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    var loaded = await fresh.Hypotheses.SingleAsync(h => h.Id == hypothesis.Id);

    Assert.Empty(loaded.Platforms);
  }

  [Fact]
  public async Task Hypothesis_RemovingPlatformFromSet_DeletesLinkRow()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = PersistenceFixtures.NewHypothesis(SeedData.OwnerId);
    hypothesis.SetPlatforms([SeedData.PcPlatformId, SeedData.VrPlatformId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Hypotheses.Add(hypothesis));

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var loaded = await context.Hypotheses.SingleAsync(h => h.Id == hypothesis.Id);
      loaded.SetPlatforms([SeedData.PcPlatformId]);
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    Assert.Equal(SeedData.PcPlatformId, Assert.Single(await fresh.HypothesisPlatforms.ToListAsync()).PlatformId);
  }

  [Fact]
  public async Task SaveChanges_DeletingPlatformReferencedByHypothesis_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = PersistenceFixtures.NewHypothesis(SeedData.OwnerId);
    hypothesis.SetPlatforms([SeedData.PcPlatformId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Hypotheses.Add(hypothesis));

    await AssertDeletingPlatformFailsAsync(database, SeedData.PcPlatformId);
  }

  [Fact]
  public async Task SaveChanges_DeletingPlatformReferencedByVacancy_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var vacancy = new Vacancy(SeedData.OwnerId, "https://example.com/jobs/restrict", VacancySource.Manual, Moment);
    vacancy.SetPlatforms([SeedData.MobilePlatformId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Vacancies.Add(vacancy));

    await AssertDeletingPlatformFailsAsync(database, SeedData.MobilePlatformId);
  }

  [Fact]
  public async Task SaveChanges_DeletingPlatformReferencedBySearchGoal_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      var goal = await context.SearchGoals.SingleAsync();
      goal.SetPlatforms([SeedData.WebPlatformId]);
      await context.SaveChangesAsync();
    }

    await AssertDeletingPlatformFailsAsync(database, SeedData.WebPlatformId);
  }

  [Fact]
  public async Task SaveChanges_DeletingUnreferencedPlatform_Succeeds()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);
    context.Platforms.Remove(await context.Platforms.SingleAsync(p => p.Id == SeedData.VrPlatformId));

    await context.SaveChangesAsync();

    Assert.False(await context.Platforms.AnyAsync(p => p.Id == SeedData.VrPlatformId));
  }

  [Fact]
  public async Task SaveChanges_DeletingHypothesisWithPlatforms_CascadesToLinks()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = PersistenceFixtures.NewHypothesis(SeedData.OwnerId);
    hypothesis.SetPlatforms([SeedData.PcPlatformId]);
    await SaveAsync(database, SeedData.OwnerId, c => c.Hypotheses.Add(hypothesis));

    await using (var context = database.CreateContext(SeedData.OwnerId))
    {
      context.Hypotheses.Remove(await context.Hypotheses.SingleAsync(h => h.Id == hypothesis.Id));
      await context.SaveChangesAsync();
    }

    await using var fresh = database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await fresh.HypothesisPlatforms.ToListAsync());
  }

  [Theory]
  [InlineData(typeof(HypothesisPlatform), nameof(HypothesisPlatform.HypothesisId))]
  [InlineData(typeof(VacancyPlatform), nameof(VacancyPlatform.VacancyId))]
  [InlineData(typeof(SearchGoalPlatform), nameof(SearchGoalPlatform.SearchGoalId))]
  public async Task Model_PlatformLink_HasUniqueIndexOnOwnerAndPlatform(Type linkType, string ownerProperty)
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var entityType = context.Model.FindEntityType(linkType)!;

    Assert.Contains(
      entityType.GetIndexes(),
      i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([ownerProperty, "PlatformId"]));
  }

  [Fact]
  public async Task Platforms_LinkedByOtherProfile_AreInvisibleToOwnerButCatalogStaysShared()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var other = await PersistenceFixtures.AddUserAsync(database);
    var hypothesis = PersistenceFixtures.NewHypothesis(other.Id);
    hypothesis.SetPlatforms([SeedData.PcPlatformId]);
    await SaveAsync(database, other.Id, c => c.Hypotheses.Add(hypothesis));
    var vacancy = new Vacancy(other.Id, "https://example.com/jobs/isolation", VacancySource.Manual, Moment);
    vacancy.SetPlatforms([SeedData.WebPlatformId]);
    await SaveAsync(database, other.Id, c => c.Vacancies.Add(vacancy));
    var goal = new SearchGoal(other.Id);
    goal.SetPlatforms([SeedData.MobilePlatformId]);
    await SaveAsync(database, other.Id, c => c.SearchGoals.Add(goal));

    await using var owner = database.CreateContext(SeedData.OwnerId);
    await using var otherContext = database.CreateContext(other.Id);

    Assert.Empty(await owner.HypothesisPlatforms.ToListAsync());
    Assert.Empty(await owner.VacancyPlatforms.ToListAsync());
    Assert.Empty(await owner.SearchGoalPlatforms.ToListAsync());
    Assert.Single(await otherContext.HypothesisPlatforms.ToListAsync());
    Assert.Single(await otherContext.VacancyPlatforms.ToListAsync());
    Assert.Single(await otherContext.SearchGoalPlatforms.ToListAsync());
    Assert.Equal(5, await owner.Platforms.CountAsync());
  }

  private static async Task SaveAsync(SqliteTestDatabase database, Guid userId, Action<KarajobDbContext> add)
  {
    await using var context = database.CreateContext(userId);
    add(context);
    await context.SaveChangesAsync();
  }

  private static async Task AssertDeletingPlatformFailsAsync(SqliteTestDatabase database, Guid platformId)
  {
    await using var context = database.CreateContext(SeedData.OwnerId);
    var platform = await context.Platforms.SingleAsync(p => p.Id == platformId);

    context.Platforms.Remove(platform);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }
}
