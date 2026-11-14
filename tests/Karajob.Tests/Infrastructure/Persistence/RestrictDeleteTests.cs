using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class RestrictDeleteTests
{
  [Fact]
  public async Task SaveChanges_DeletingHypothesisReferencedByMatch_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, hypothesis.Id);
    await using var context = database.CreateContext(SeedData.OwnerId);
    var loaded = await context.Hypotheses.SingleAsync(h => h.Id == hypothesis.Id);

    context.Hypotheses.Remove(loaded);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DeletingRoleReferencedByHypothesis_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await PersistenceFixtures.AddHypothesisAsync(database, SeedData.OwnerId);
    await using var context = database.CreateContext(SeedData.OwnerId);
    var role = await context.Roles.SingleAsync(r => r.Id == SeedData.UnityDeveloperRoleId);

    context.Roles.Remove(role);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DeletingLocationReferencedByHypothesisLocation_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var hypothesis = PersistenceFixtures.NewHypothesis(SeedData.OwnerId);
    hypothesis.SetLocations([SeedData.EuLocationId]);
    await using (var seed = database.CreateContext(SeedData.OwnerId))
    {
      seed.Hypotheses.Add(hypothesis);
      await seed.SaveChangesAsync();
    }

    await using var context = database.CreateContext(SeedData.OwnerId);
    var location = await context.Locations.SingleAsync(l => l.Id == SeedData.EuLocationId);

    context.Locations.Remove(location);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DeletingLanguageReferencedByCvDocument_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await PersistenceFixtures.AddCvAsync(database, SeedData.OwnerId);
    await using var context = database.CreateContext(SeedData.OwnerId);
    var language = await context.Languages.SingleAsync(l => l.Id == SeedData.EnglishLanguageId);

    context.Languages.Remove(language);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }
}
