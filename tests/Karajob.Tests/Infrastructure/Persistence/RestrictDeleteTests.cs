using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class RestrictDeleteTests
{
  [Fact]
  public async Task SaveChanges_DeletingTrackReferencedByMatch_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var track = await PersistenceFixtures.AddTrackAsync(database, SeedData.OwnerId);
    await PersistenceFixtures.AddMatchedVacancyAsync(database, SeedData.OwnerId, track.Id);
    await using var context = database.CreateContext(SeedData.OwnerId);
    var loaded = await context.Tracks.SingleAsync(h => h.Id == track.Id);

    context.Tracks.Remove(loaded);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DeletingRoleReferencedByTrack_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await PersistenceFixtures.AddTrackAsync(database, SeedData.OwnerId);
    await using var context = database.CreateContext(SeedData.OwnerId);
    var role = await context.Roles.SingleAsync(r => r.Id == SeedData.UnityDeveloperRoleId);

    context.Roles.Remove(role);

    await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
  }

  [Fact]
  public async Task SaveChanges_DeletingLocationReferencedByTrackLocation_ThrowsDbUpdateException()
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    var track = PersistenceFixtures.NewTrack(SeedData.OwnerId);
    track.SetLocations([SeedData.EuLocationId]);
    await using (var seed = database.CreateContext(SeedData.OwnerId))
    {
      seed.Tracks.Add(track);
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
