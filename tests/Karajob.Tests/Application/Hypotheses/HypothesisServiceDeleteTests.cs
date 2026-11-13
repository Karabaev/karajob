using Karajob.Application.Common;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Hypotheses;

public class HypothesisServiceDeleteTests : HypothesisServiceFixture
{
  [Fact]
  public async Task DeleteAsync_Unreferenced_RemovesHypothesis()
  {
    var created = await Service.CreateAsync(Input());

    await Service.DeleteAsync(created.Id);

    Assert.Null(await Service.FindAsync(created.Id));
  }

  [Fact]
  public async Task DeleteAsync_Missing_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteAsync_HypothesisOfAnotherUser_ThrowsKeyNotFoundAndKeepsIt()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    var otherService = ServiceFor(other.Id);
    var foreign = await otherService.CreateAsync(Input());

    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(foreign.Id));

    Assert.NotNull(await otherService.FindAsync(foreign.Id));
  }

  [Fact]
  public async Task DeleteAsync_ReferencedByVacancyMatch_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    var created = await Service.CreateAsync(Input());
    await PersistenceFixtures.AddMatchedVacancyAsync(Database, SeedData.OwnerId, created.Id);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(created.Id));
  }

  [Fact]
  public async Task DeleteAsync_ReferencedByApplication_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    var (_, hypothesis, _) = await PersistenceFixtures.AddAppliedVacancyAsync(Database, SeedData.OwnerId);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(hypothesis.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindAsync(hypothesis.Id));
  }
}
