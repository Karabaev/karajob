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
}
